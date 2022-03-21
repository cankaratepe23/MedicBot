using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
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
        await ctx.RespondAsync(remainingText);
    }

    [Command("shutdown")]
    public async Task ShutdownBotCommand(CommandContext ctx)
    {
        await Program.Cleanup();
        Environment.Exit(0);
    }
}