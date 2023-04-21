using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using MedicBot.Repository;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace MedicBot.Commands;

[Hidden]
[RequireOwner]
public class BaseCommands : BaseCommandModule
{
    private readonly IMongoDatabase _db;

    public BaseCommands(IMongoDatabase db)
    {
        _db = db;
    }

    [Command("test")]
    public async Task TestCommand(CommandContext ctx, [RemainingText] string remainingText)
    {
        Log.Information("Test command called by {User}", ctx.User);
        var firstTrack = await _db.GetCollection<BsonDocument>("search-test").AsQueryable().FirstOrDefaultAsync();
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