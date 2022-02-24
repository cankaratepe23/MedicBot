using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using LiteDB;
using MedicBot.Commands;
using MedicBot.Manager;
using MedicBot.Model;
using MedicBot.Utils;
using Microsoft.Extensions.Logging;
using Serilog;

namespace MedicBot;

internal static class Program
{
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

        var lavalinkEndpoint = new ConnectionEndpoint
        {
            Hostname = "127.0.0.1",
            Port = 2333
        };

        var lavalinkConfiguration = new LavalinkConfiguration
        {
            Password =
                Constants.LavalinkPassword, // Lavalink only listens on 127.0.0.1, this is not a security concern.
            RestEndpoint = lavalinkEndpoint,
            SocketEndpoint = lavalinkEndpoint
        };

        #endregion

        #region Discord Client Config

        var discord = new DiscordClient(new DiscordConfiguration
        {
            Token = Environment.GetEnvironmentVariable(Constants.BotTokenEnvironmentVariableName),
            LoggerFactory = logFactory
        });
        var lavalink = discord.UseLavalink();

        #endregion

        #region WebAppConfig
        
        // TODO

        #endregion
        
        // Initializations
        AudioManager.Init(discord);

        // Commands Init
        var commands = discord.UseCommandsNext(new CommandsNextConfiguration
        {
            StringPrefixes = Constants.BotPrefixes
        });
        commands.RegisterCommands<BaseCommands>();
        commands.RegisterCommands<AudioCommands>();
        commands.RegisterCommands<SettingsCommands>();

        // Startup
        await discord.ConnectAsync();
        await lavalink.ConnectAsync(lavalinkConfiguration);
        await Task.Delay(-1);
    }
}