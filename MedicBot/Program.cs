// See https://aka.ms/new-console-template for more information

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using MedicBot.Commands;
using MedicBot.Manager;
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
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(
                outputTemplate: Constants.SerilogOutputTemplate)
            .CreateLogger();

        var logFactory = new LoggerFactory().AddSerilog();

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

        var discord = new DiscordClient(new DiscordConfiguration
        {
            Token = Environment.GetEnvironmentVariable(Constants.BotTokenEnvironmentVariableName),
            LoggerFactory = logFactory
        });
        var lavalink = discord.UseLavalink();

        AudioManager.Init(discord);

        // Commands
        var commands = discord.UseCommandsNext(new CommandsNextConfiguration
        {
            StringPrefixes = Constants.BotPrefixes
        });
        commands.RegisterCommands<BaseCommands>();
        commands.RegisterCommands<AudioCommands>();

        // Startup
        await discord.ConnectAsync();
        await lavalink.ConnectAsync(lavalinkConfiguration);
        await Task.Delay(-1);
    }
}