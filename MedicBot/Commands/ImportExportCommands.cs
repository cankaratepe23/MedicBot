using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using MedicBot.Manager;
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
        if (ctx.Message.Attachments.Count == 0 || ctx.Message.Attachments[0] == null)
        {
            Log.Warning("No attachments found in {Message}", ctx.Message);
            await ctx.RespondAsync("This command requires an attachment.");
            return;
        }

        if (ctx.Message.Attachments.Count > 1)
        {
            Log.Information("Ignoring multiple attachments sent to add command");
            await ctx.RespondAsync("You sent multiple attachments. Only the first attachment will be considered.");
        }

        await ctx.RespondAsync("Starting import...");
        await ImportExportManager.Import(ctx.Message.Attachments[0].Url);
        await ctx.RespondAsync("Import done.");
    }
}