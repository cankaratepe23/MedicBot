// See https://aka.ms/new-console-template for more information

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using MedicBot.Commands;
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
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        var logFactory = new LoggerFactory().AddSerilog();

        var lavalinkEndpoint = new ConnectionEndpoint()
        {
            Hostname = "127.0.0.1",
            Port = 2333
        };

        var lavalinkConfiguration = new LavalinkConfiguration()
        {
            Password = "5aJCTF!Z2&*853#79r7!xind*u^2LWy",
            RestEndpoint = lavalinkEndpoint,
            SocketEndpoint = lavalinkEndpoint
        };
        
        var discord = new DiscordClient(new DiscordConfiguration()
        {
            Token = Environment.GetEnvironmentVariable("Bot_Token_Dev"),
            LoggerFactory = logFactory
        });
        var lavalink = discord.UseLavalink();
        
        // Commands
        var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
        {
            StringPrefixes = new[] {"*"}
        });
        commands.RegisterCommands<BaseCommands>();
        commands.RegisterCommands<AudioCommands>();

        // Events
        discord.MessageCreated += async (s, e) =>
        {
            if (e.Message.Content.ToLower() == "ping")
            {
                await e.Message.RespondAsync("Pong!");
            }
        };

        // Startup
        await discord.ConnectAsync();
        await lavalink.ConnectAsync(lavalinkConfiguration);
        await Task.Delay(-1);
    }
}