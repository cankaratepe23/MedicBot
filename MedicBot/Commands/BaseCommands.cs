using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Serilog;

namespace MedicBot.Commands;

[Hidden]
[RequireOwner]
public class BaseCommands : BaseCommandModule
{
    private readonly IMongoDatabase _database;

    public BaseCommands(IMongoDatabase database)
    {
        _database = database;
    }

    [Command("test")]
    public async Task TestCommand(CommandContext ctx, [RemainingText] string remainingText)
    {
        Log.Information("Test command called by {User}", ctx.User);
        var firstTrack = await _database.GetCollection<BsonDocument>("medicbot-data")
                                .AsQueryable()
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