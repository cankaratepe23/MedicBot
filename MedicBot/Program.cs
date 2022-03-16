using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Lavalink;
using LiteDB;
using MedicBot.Commands;
using MedicBot.Manager;
using MedicBot.Model;
using MedicBot.Utils;
using Serilog;

namespace MedicBot;

internal static class Program
{
    private static readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private static DiscordClient? _client;
    
    private static void Main(string[] args)
    {
        ConfigureAsync().GetAwaiter().GetResult();
    }

    private static async Task ConfigureAsync()
    {
        // Configuration

        #region Logger Config

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(
                outputTemplate: Constants.SerilogOutputTemplate)
            .CreateLogger();

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

        #region WebApp Config

        var builder = WebApplication.CreateBuilder();
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        #endregion

        // Initializations
        // Ensure tracks folder exists
        var audioTracksPath = Path.Combine(Environment.CurrentDirectory, Constants.AudioTracksPath);
        if (!Directory.Exists(audioTracksPath))
            Directory.CreateDirectory(audioTracksPath);
        AudioManager.Init(discord, audioTracksPath);


        // Commands Init
        var commands = discord.UseCommandsNext(new CommandsNextConfiguration
        {
            StringPrefixes = Constants.BotPrefixes
        });
        commands.RegisterCommands<BaseCommands>();
        commands.RegisterCommands<AudioCommands>();
        commands.RegisterCommands<SettingsCommands>();

        // Register events
        app.Lifetime.ApplicationStopping.Register(async () =>
        {
            await Cleanup();
            Environment.Exit(0);
        });
        
        // Startup
        await app.StartAsync();
        await discord.ConnectAsync();
        await lavalink.ConnectAsync(lavalinkConfiguration);
        try
        {
            await Task.Delay(-1, _cancellationTokenSource.Token);
        }
        catch (TaskCanceledException tce)
        { }
    }

    public static async Task Cleanup()
    {
        if (_client == null)
        {
            return;
        }

        await _client.GetLavalink().GetNodeConnection(Constants.LavalinkEndpoint).StopAsync();
        await _client.DisconnectAsync();
        _client.Dispose();
        _client = null;
        _cancellationTokenSource.Cancel();
    }
}