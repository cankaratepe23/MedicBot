using System.Net;
using AspNet.Security.OAuth.Discord;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using MedicBot.Commands;
using MedicBot.EventHandler;
using MedicBot.Manager;
using MedicBot.Repository;
using MedicBot.Utils;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Serilog;

namespace MedicBot;

internal static class Program
{
    public static readonly HttpClient Client = new();
    private static readonly CancellationTokenSource CancellationTokenSource = new();
    private static DiscordClient? _client;

    private static void Main()
    {
        ConfigureAsync().GetAwaiter().GetResult();
    }

    private static async Task ConfigureAsync()
    {
        // Configuration

        #region WebApp Config

        var builder = WebApplication.CreateBuilder();
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("MedicBotPolicy", policyBuilder =>
            {
                policyBuilder.WithOrigins("https://medicbot.comaristan.com").AllowCredentials();
                if (builder.Environment.IsDevelopment())
                {
                    policyBuilder.WithOrigins("https://127.0.0.1:3000").AllowCredentials();
                }
            });
        });
        builder.Configuration.AddEnvironmentVariables("MedicBot_");
        var clientId = Environment.GetEnvironmentVariable(Constants.OAuthClientIdEnvironmentVariableName);
        var clientSecret = Environment.GetEnvironmentVariable(Constants.OAuthClientSecretEnvironmentVariableName);
        if (clientId == null || clientSecret == null)
        {
            throw new InvalidOperationException("Discord OAuth environment variables are not set.");
        }

        var jwtSecret = Environment.GetEnvironmentVariable(Constants.JwtTokenSecretEnvironmentVariableName);
        if (string.IsNullOrEmpty(jwtSecret))
        {
            throw new InvalidOperationException("JWT secret environment variable is not set.");
        }

        TokenManager.Init(jwtSecret);

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });

        _ = DiscordAuthenticationDefaults.CallbackPath;
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            }).AddCookie(options =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    options.LoginPath = "/Auth/LocalLogin";
                }
                else
                {
                    options.LoginPath = "/Auth/Login";
                }
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = (int) HttpStatusCode.Unauthorized;
                    return Task.CompletedTask;
                };
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.MaxAge = TimeSpan.FromDays(7);
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtSecret)),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents {
                    OnMessageReceived = (context) => {
                        if (!context.Request.Query.TryGetValue("token", out StringValues values))
                        {
                            return Task.CompletedTask;
                        }

                        if (values.Count > 1) {
                            context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                            context.Fail(
                                "Only one 'token' query string parameter can be defined. " +
                                $"However, {values.Count:N0} were included in the request."
                            );

                            return Task.CompletedTask;
                        }

                        var token = values.Single();

                        if (string.IsNullOrWhiteSpace(token)) {
                            context.Response.StatusCode = (int) HttpStatusCode.Unauthorized;
                            context.Fail(
                                "The 'token' query string parameter was defined, " +
                                "but a value to represent the token was not included."
                            );

                            return Task.CompletedTask;
                        }

                        context.Token = token;

                        return Task.CompletedTask;
                    }
                };
            })
            .AddDiscord(options =>
            {
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.CorrelationCookie.SameSite = SameSiteMode.None;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
            });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("CombinedPolicy", policy =>
            {
                policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
                policy.AuthenticationSchemes.Add(CookieAuthenticationDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
            });
        });

        var mongoDbSettings = builder.Configuration.GetSection("MongoDb").Get<MongoDbSettings>();
        if (mongoDbSettings?.ConnectionString is null)
        {
            Log.Error("Failed to read MongoDB configuration from appsettings.json");
            return;
        }

        var mongoClientSettings = MongoClientSettings.FromConnectionString(mongoDbSettings.ConnectionString);
        var mongoClient = new MongoClient(mongoClientSettings);
        var mongoDb = mongoClient.GetDatabase(mongoDbSettings.Database);
        if (mongoDb is null)
        {
            Log.Error("Failed to connect to MongoDB database");
            return;
        }

        MongoDbManager.Database = mongoDb;

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseForwardedHeaders();

        app.UseHttpsRedirection();

        app.UseCors("MedicBotPolicy");

        app.UseCookiePolicy(new CookiePolicyOptions
        {
            MinimumSameSitePolicy = SameSiteMode.None,
            Secure = CookieSecurePolicy.Always
        });

        app.UseAuthentication();

        app.UseAuthorization();

        app.MapControllers();

        #endregion

        #region Logger Config

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(
                outputTemplate: Constants.SerilogOutputTemplate);

        if (app.Environment.IsDevelopment())
        {
            loggerConfiguration.MinimumLevel.Debug();
        }

        Log.Logger = loggerConfiguration.CreateLogger();

        var logFactory = new LoggerFactory().AddSerilog();

        #endregion

        #region Lavalink Config

        var lavalinkConfiguration = new LavalinkConfiguration
        {
            // Lavalink only listens on 127.0.0.1, plaintext password in git is not a security concern.
            Password = Constants.LavalinkPassword,
            RestEndpoint = Constants.LavalinkEndpoint,
            SocketEndpoint = Constants.LavalinkEndpoint
        };

        #endregion

        #region Discord Client Config

        var discord = new DiscordClient(new DiscordConfiguration
        {
            Token = Environment.GetEnvironmentVariable(Constants.BotTokenEnvironmentVariableName),
            LoggerFactory = logFactory,
            Intents = DiscordIntents.All
        });
        _client = discord;
        var lavalink = discord.UseLavalink();

        #endregion

        #region Initializations

        // Ensure tracks folder exists
        Log.Information("Checking initial directories");
        var audioTracksPath = Constants.AudioTracksPath;
        var imagesPath = Constants.ImagesPath;
        var tempFilesPath = Constants.TempFilesPath;
        if (!Directory.Exists(audioTracksPath))
        {
            Log.Information("Creating {DirectoryPath}", audioTracksPath);
            Directory.CreateDirectory(audioTracksPath);
        }

        if (!Directory.Exists(imagesPath))
        {
            Log.Information("Creating {DirectoryPath}", imagesPath);
            Directory.CreateDirectory(imagesPath);
        }

        if (!Directory.Exists(tempFilesPath))
        {
            Log.Information("Creating {DirectoryPath}", tempFilesPath);
            Directory.CreateDirectory(tempFilesPath);
        }

        AudioManager.Init(discord, audioTracksPath, tempFilesPath);
        ImageManager.Init(discord, imagesPath, tempFilesPath);
        VoiceStateHandler.Init(discord);

        // Commands Init
        var commands = discord.UseCommandsNext(new CommandsNextConfiguration
        {
            StringPrefixes = Constants.BotPrefixes,
            Services = app.Services
        });
        commands.RegisterCommands<BaseCommands>();
        commands.RegisterCommands<AudioCommands>();
        commands.RegisterCommands<ImageCommands>();
        commands.RegisterCommands<SettingsCommands>();
        commands.RegisterCommands<ImportExportCommands>();
        commands.RegisterCommands<MiscCommands>();
        commands.RegisterConverter(new StringLowercaseConverter());

        // Interactivity Init
        discord.UseInteractivity(new InteractivityConfiguration
        {
            PollBehaviour = PollBehaviour.KeepEmojis,
            Timeout = TimeSpan.FromSeconds(15)
        });

        #endregion

        // Register events
        async void CleanupCallback()
        {
            await Cleanup();
            Environment.Exit(0);
        }

        app.Lifetime.ApplicationStopping.Register(CleanupCallback);
        discord.VoiceStateUpdated += VoiceStateHandler.DiscordOnVoiceStateUpdated;
        discord.GuildDownloadCompleted += (_, _) =>
        {
            VoiceStateHandler.StartTracking();
            return Task.CompletedTask;
        };


        // Startup
        await app.StartAsync();
        await discord.ConnectAsync();
        await lavalink.ConnectAsync(lavalinkConfiguration);
        try
        {
            await Task.Delay(-1, CancellationTokenSource.Token);
        }
        catch (TaskCanceledException)
        {
        }
    }

    public static async Task Cleanup()
    {
        if (_client == null)
        {
            return;
        }

        VoiceStateHandler.ReloadTracking();
        await _client.GetLavalink().GetNodeConnection(Constants.LavalinkEndpoint).StopAsync();
        await _client.DisconnectAsync();
        _client.Dispose();
        _client = null;
        CancellationTokenSource.Cancel();
    }
}