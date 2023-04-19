using AspNet.Security.OAuth.Discord;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Lavalink;
using LiteDB;
using MedicBot.Commands;
using MedicBot.EventHandler;
using MedicBot.Manager;
using MedicBot.Model;
using MedicBot.Utils;
using Microsoft.AspNetCore.Authentication.Cookies;
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
                if (builder.Environment.IsDevelopment())
                {
                    policyBuilder.WithOrigins("http://localhost:3000");
                }
            });
        });
        var clientId = Environment.GetEnvironmentVariable(Constants.OAuthClientIdEnvironmentVariableName);
        var clientSecret = Environment.GetEnvironmentVariable(Constants.OAuthClientSecretEnvironmentVariableName);
        if (clientId == null || clientSecret == null)
        {
            throw new InvalidOperationException("Discord OAuth environment variables are not set.");
        }

        _ = DiscordAuthenticationDefaults.CallbackPath;
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            }).AddCookie()
            .AddDiscord(options =>
            {
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
            });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseCors("MedicBotPolicy");

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

        #region LiteDB Config

        var mapper = BsonMapper.Global;
        mapper.Entity<BotSetting>()
            .Id(s => s.Key);

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
            LoggerFactory = logFactory
        });
        _client = discord;
        var lavalink = discord.UseLavalink();

        #endregion

        #region Initializations

        // Ensure tracks folder exists
        var audioTracksPath = Path.Combine(Environment.CurrentDirectory, Constants.AudioTracksPath);
        if (!Directory.Exists(audioTracksPath))
        {
            Directory.CreateDirectory(audioTracksPath);
        }

        AudioManager.Init(discord, audioTracksPath);
        VoiceStateHandler.Init(discord);

        // Commands Init
        var commands = discord.UseCommandsNext(new CommandsNextConfiguration
        {
            StringPrefixes = Constants.BotPrefixes
        });
        commands.RegisterCommands<BaseCommands>();
        commands.RegisterCommands<AudioCommands>();
        commands.RegisterCommands<SettingsCommands>();
        commands.RegisterCommands<ImportExportCommands>();
        commands.RegisterConverter(new StringLowercaseConverter());

        #endregion

        // Register events
        app.Lifetime.ApplicationStopping.Register(async () =>
        {
            await Cleanup();
            Environment.Exit(0);
        });
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