using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Lavalink;
using MedicBot.Manager;
using MedicBot.Repository;
using MedicBot.Utils;
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

        var botSetting = SettingsRepository.Get(remainingText);
        var message = botSetting == null
            ? $"Could not find setting with key: \"{remainingText}\""
            : $"Found setting value: {botSetting}";

        await ctx.RespondAsync(message);


        await AudioManager.JoinAsync(463052720509812736);
    }

    [Command("shutdown")]
    public async Task ShutdownBotCommand(CommandContext ctx)
    {
        await ctx.Client.GetLavalink().GetNodeConnection(Constants.LavalinkEndpoint).StopAsync();
        await ctx.Client.DisconnectAsync();
        Environment.Exit(0);
    }
}