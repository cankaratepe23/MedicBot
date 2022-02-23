using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Serilog;

namespace MedicBot.Commands;

public class BaseCommands : BaseCommandModule
{
    [Command("test")]
    public async Task TestCommand(CommandContext ctx)
    {
        Log.Information($"Test command called by {ctx.User}");
        await ctx.RespondAsync($"Test! Current time is: {DateTime.Now}");
    }
}