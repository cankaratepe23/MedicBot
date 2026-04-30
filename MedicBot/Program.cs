using System.Net;
using AspNet.Security.OAuth.Discord;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Lavalink4NET.Extensions;
using MedicBot.Commands;
using MedicBot.EventHandler;
using MedicBot.Hub;
using MedicBot.Manager;
using MedicBot.Options;
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
    private static readonly CancellationTokenSource CancellationTokenSource = new();
    private static DiscordClient? _client;

    private static void Main()
    {
        ConfigureAsync().GetAwaiter().GetResult();
    }

    private static async Task ConfigureAsync()
    {
        #region WebApp Config

        var builder = WebApplication.CreateBuilder();
        WriteStartupDebug("Host environment: {EnvironmentName}", builder.Environment.EnvironmentName);
        WriteStartupDebug("Raw ASPNETCORE_ENVIRONMENT: {Value}", GetNonEmptyEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
        WriteStartupDebug("Raw DOTNET_ENVIRONMENT: {Value}", GetNonEmptyEnvironmentVariable("DOTNET_ENVIRONMENT"));
        WriteStartupDebug("Raw DOTNET_LAUNCH_PROFILE: {Value}", GetNonEmptyEnvironmentVariable("DOTNET_LAUNCH_PROFILE"));
        WriteStartupDebug("Configuration sources: {Sources}", string.Join(", ", builder.Configuration.Sources.Select(source => source.GetType().Name)));
        builder.Services.AddControllers();
        builder.Services.AddSignalR().AddJsonProtocol();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHttpClient();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("MedicBotPolicy", policyBuilder =>
            {
                policyBuilder.WithOrigins("https://medicbot.comaristan.com").WithMethods("GET", "POST", "DELETE").WithHeaders("x-requested-with","x-signalr-user-agent").AllowCredentials();
                
                if (builder.Environment.IsDevelopment())
                {
                    policyBuilder.WithOrigins("https://127.0.0.1:3000");
                }
            });
        });
        var webSocketOptions = new WebSocketOptions();
        webSocketOptions.AllowedOrigins.Add("https://medicbot.comaristan.com");
        if (builder.Environment.IsDevelopment())
        {
            webSocketOptions.AllowedOrigins.Add("https://127.0.0.1:3000");
        }

        builder.Configuration.AddEnvironmentVariables("MedicBot_");
        WriteStartupDebug("Added prefixed environment variable source: MedicBot_");
        WriteStartupDebug("Config key presence: Discord:Token={DiscordToken}, Discord:OAuthClientId={DiscordClientId}, Discord:OAuthClientSecret={DiscordClientSecret}, Auth:JwtSecret={JwtSecret}, MongoDb:ConnectionString={MongoConnectionString}",
            HasConfiguredValue(builder.Configuration, "Discord:Token"),
            HasConfiguredValue(builder.Configuration, "Discord:OAuthClientId"),
            HasConfiguredValue(builder.Configuration, "Discord:OAuthClientSecret"),
            HasConfiguredValue(builder.Configuration, "Auth:JwtSecret"),
            HasConfiguredValue(builder.Configuration, "MongoDb:ConnectionString"));

        // Bind options from configuration (user secrets, environment variables, appsettings)
        builder.Services.Configure<DiscordOptions>(builder.Configuration.GetSection(DiscordOptions.SectionName));
        builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));

        var discordOptions = builder.Configuration.GetSection(DiscordOptions.SectionName).Get<DiscordOptions>();
        if (string.IsNullOrEmpty(discordOptions?.Token))
        {
            throw new InvalidOperationException("Discord bot token is not configured. Set 'Discord:Token' via user secrets or environment variables.");
        }
        if (string.IsNullOrEmpty(discordOptions.OAuthClientId) || string.IsNullOrEmpty(discordOptions.OAuthClientSecret))
        {
            throw new InvalidOperationException("Discord OAuth is not configured. Set 'Discord:OAuthClientId' and 'Discord:OAuthClientSecret' via user secrets or environment variables.");
        }

        var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>();
        if (string.IsNullOrEmpty(authOptions?.JwtSecret))
        {
            throw new InvalidOperationException("JWT secret is not configured. Set 'Auth:JwtSecret' via user secrets or environment variables.");
        }

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
                    IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(authOptions.JwtSecret)),
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
                options.ClientId = discordOptions.OAuthClientId;
                options.ClientSecret = discordOptions.OAuthClientSecret;
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

        // MongoDB
        var mongoDbSettings = builder.Configuration.GetSection("MongoDb").Get<MongoDbSettings>();
        if (mongoDbSettings?.ConnectionString is null)
        {
            throw new InvalidOperationException("MongoDB connection string is not configured. Set 'MongoDb:ConnectionString' via user secrets or environment variables.");
        }

        var mongoClientSettings = MongoClientSettings.FromConnectionString(mongoDbSettings.ConnectionString);
        var mongoClient = new MongoClient(mongoClientSettings);
        var mongoDb = mongoClient.GetDatabase(mongoDbSettings.Database)
            ?? throw new InvalidOperationException("Failed to connect to MongoDB database '" + mongoDbSettings.Database + "'.");

        builder.Services.AddSingleton<IMongoDatabase>(mongoDb);

        // Repositories
        builder.Services.AddSingleton<ISettingsRepository, SettingsRepository>();
        builder.Services.AddSingleton<IAudioRepository, AudioRepository>();
        builder.Services.AddSingleton<IAudioPlaybackLogRepository, AudioPlaybackLogRepository>();
        builder.Services.AddSingleton<IImageRepository, ImageRepository>();
        builder.Services.AddSingleton<IRefreshTokenRepository, RefreshTokenRepository>();
        builder.Services.AddSingleton<IUserFavoritesRepository, UserFavoritesRepository>();
        builder.Services.AddSingleton<IUserMuteRepository, UserMuteRepository>();
        builder.Services.AddSingleton<IUserPointsRepository, UserPointsRepository>();

        // Event Handlers
        builder.Services.AddSingleton<IVoiceStateHandler, VoiceStateHandler>();
        builder.Services.AddSingleton<BotSettingHandler>();

        // Managers
        builder.Services.AddSingleton<ITokenManager, TokenManager>();
        builder.Services.AddSingleton<IUserManager, UserManager>();
        builder.Services.AddSingleton<IAudioManager, AudioManager>();
        builder.Services.AddSingleton<IImageManager, ImageManager>();
        builder.Services.AddSingleton<IImportExportManager, ImportExportManager>();
        builder.Services.AddSingleton<IMiscManager, MiscManager>();

        #region Discord Client Config

        var logFactory = new LoggerFactory().AddSerilog();

        var discord = new DiscordClient(new DiscordConfiguration
        {
            Token = discordOptions.Token,
            LoggerFactory = logFactory,
            Intents = DiscordIntents.All
        });
        _client = discord;

        #endregion
        
        #region Lavalink Config

        builder.Services.AddSingleton(discord);
        builder.Services.AddLavalink();
        builder.Services.ConfigureLavalink(config =>
        {
            config.Passphrase = Constants.LavalinkPassword;
            config.BaseAddress = new Uri(Constants.LavalinkHttp);
            config.WebSocketUri = new Uri(Constants.LavalinkWebSocket);
        });

        #endregion

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

        app.UseWebSockets(webSocketOptions);

        app.UseCookiePolicy(new CookiePolicyOptions
        {
            MinimumSameSitePolicy = SameSiteMode.None,
            Secure = CookieSecurePolicy.Always
        });

        app.UseAuthentication();

        app.UseAuthorization();

        app.MapControllers();

        app.MapHub<PlaybackHub>("/PlaybackHub");

        #endregion

        #region Logger Config

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(
                outputTemplate: Constants.SerilogOutputTemplate);

        var mongoDbConnectionStringWithDatabaseName = mongoDbSettings.GetConnectionStringWithDatabaseName();
        if (mongoDbConnectionStringWithDatabaseName != null)
        {
            loggerConfiguration.WriteTo.MongoDBBson(mongoDbConnectionStringWithDatabaseName,
                cappedMaxDocuments: 10000,
                rollingInterval: Serilog.Sinks.MongoDB.RollingInterval.Month);
        }

        if (app.Environment.IsDevelopment())
        {
            loggerConfiguration.MinimumLevel.Debug();
        }

        Log.Logger = loggerConfiguration.CreateLogger();

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

        // Start Lavalink
        using (var scope = app.Services.CreateScope())
        {
            var audioService = scope.ServiceProvider.GetRequiredService<Lavalink4NET.IAudioService>();
            await audioService.StartAsync();
        }

        // Resolve voice state handler for event wiring
        var voiceStateHandler = app.Services.GetRequiredService<IVoiceStateHandler>();

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
        discord.VoiceStateUpdated += voiceStateHandler.DiscordOnVoiceStateUpdated;
        discord.GuildDownloadCompleted += (_, _) =>
        {
            voiceStateHandler.StartTracking();
            return Task.CompletedTask;
        };

        // Startup
        await app.StartAsync();
        await discord.ConnectAsync();
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

        await _client.DisconnectAsync();
        _client.Dispose();
        _client = null;
        CancellationTokenSource.Cancel();
    }

    private static bool HasConfiguredValue(IConfiguration configuration, string key)
    {
        return !string.IsNullOrWhiteSpace(configuration[key]);
    }

    private static string GetNonEmptyEnvironmentVariable(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        return string.IsNullOrWhiteSpace(value) ? "<not set>" : value;
    }

    private static void WriteStartupDebug(string messageTemplate, params object?[] propertyValues)
    {
        var renderedMessage = messageTemplate;
        foreach (var propertyValue in propertyValues)
        {
            var placeholderStart = renderedMessage.IndexOf('{');
            var placeholderEnd = renderedMessage.IndexOf('}', placeholderStart + 1);
            if (placeholderStart < 0 || placeholderEnd < 0)
            {
                break;
            }

            renderedMessage = renderedMessage.Remove(placeholderStart, placeholderEnd - placeholderStart + 1)
                .Insert(placeholderStart, propertyValue?.ToString() ?? "<null>");
        }

        Console.WriteLine($"[startup-debug] {renderedMessage}");
    }
}