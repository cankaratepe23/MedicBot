using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using MedicBot.Manager;
using MedicBot.Utils;
using MongoDB.Bson;
using MongoDB.Driver;
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
        var firstTrack = await MongoDbManager.Database.GetCollection<BsonDocument>("search-test").AsQueryable()
            .FirstOrDefaultAsync();
        var trackName = firstTrack is null ? "**null**" : firstTrack["Name"];
        await ctx.RespondAsync(trackName.ToString() ?? "**null**");
    }

    [Command("shutdown")]
    public async Task ShutdownBotCommand(CommandContext ctx)
    {
        await Program.Cleanup();
        Environment.Exit(0);
    }
}