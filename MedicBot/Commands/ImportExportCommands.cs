using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Serilog;

namespace MedicBot.Commands;

[Hidden]
[RequireOwner]
public class ImportExportCommands : BaseCommandModule
{
    [Command("import")]
    public async Task ImportCommand(CommandContext ctx)
    {
        Log.Information("Import command called by {User}", ctx.User);
        await ctx.RespondAsync("Starting import...");
        await ctx.RespondAsync("Import done.");
    }
}