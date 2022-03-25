using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
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
        await ctx.RespondAsync(SettingsRepository.GetValue<int>(remainingText).ToString());
    }

    [Command("shutdown")]
    public async Task ShutdownBotCommand(CommandContext ctx)
    {
        await Program.Cleanup();
        Environment.Exit(0);
    }
}