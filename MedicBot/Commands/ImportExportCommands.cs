using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using MedicBot.Manager;
using MedicBot.Utils;
using Serilog;

namespace MedicBot.Commands;

[Hidden]
[RequireOwner]
public class ImportExportCommands : BaseCommandModule
{
    private readonly IImportExportManager _importExportManager;

    public ImportExportCommands(IImportExportManager importExportManager)
    {
        _importExportManager = importExportManager;
    }

    [Command("import")]
    public async Task ImportCommand(CommandContext ctx)
    {
        Log.Information("Import command called by {User}", ctx.User);
        await ctx.RespondAsync("Starting import...");
        try
        {
            await _importExportManager.Import(ctx.Message.GetFirstAttachment().Url);
        }
        catch (Exception e)
        {
            await ctx.RespondAsync(e.Message);
            return;
        }

        await ctx.RespondAsync("Import done.");
    }
}