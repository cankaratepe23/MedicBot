using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using MedicBot.Manager;
using MedicBot.Repository;
using Serilog;

namespace MedicBot.Commands;

[Hidden]
[RequireOwner]
public class BaseCommands : BaseCommandModule
{
    [Command("test")]
    public async Task TestCommand(CommandContext ctx, [RemainingText] string remainingText)
    {
        Log.Information("Test command called by {User}", ctx.User);
        await ctx.RespondAsync($"Test! Current time is: {DateTime.Now}");

        var botSetting = SettingsRepository.GetBotSetting(remainingText);
        var message = botSetting == null
            ? $"Could not find setting with key: \"{remainingText}\""
            : $"Found setting value: {botSetting}";

        await ctx.RespondAsync(message);


        await AudioManager.JoinAsync(463052720509812736);
    }

    [Command("shutdown")]
    public async Task ShutdownBotCommand(CommandContext ctx)
    {
        // TODO Move this to Constants file or better yet, configuration file.
        await ctx.Client.GetLavalink().GetNodeConnection(new ConnectionEndpoint("127.0.0.1", 2333)).StopAsync();
        await ctx.Client.DisconnectAsync();
        Environment.Exit(0);
    }
}