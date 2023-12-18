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

    [Command("mute")]
    public async Task MuteCommand(CommandContext ctx, DiscordMember memberToMute, int minutes = 15)
    {
        if (memberToMute.VoiceState == null)
        {
            await ctx.RespondAsync("User must be in a voice channel to be muted.");
            return;
        }
        var targetVoiceChannel = memberToMute.VoiceState.Channel;
        if (!targetVoiceChannel.Users.Contains(ctx.Member))
        {
            await ctx.RespondAsync("You need to be in the same voice channel with the user you're trying to mute.");
            return;
        }

        var totalCount = targetVoiceChannel.GetNonBotUsers().Count();
        var votesNeeded = (int) Math.Ceiling(totalCount / 2.0);

        var pollResponse = await ctx.RespondAsync($"Mute {memberToMute.Mention} for {minutes} minutes? React to this message with any emoji to vote YES (Need {votesNeeded}/{totalCount} votes) (20 seconds)");
        var reactions = await pollResponse.CollectReactionsAsync(timeoutOverride: TimeSpan.FromSeconds(20));
        var voteCount = reactions.SelectMany(r => r.Users).Distinct().Count();
        if (voteCount >= votesNeeded)
        {
            UserManager.Mute(memberToMute, minutes);
            await ctx.RespondAsync($"{memberToMute.Mention} you have been muted with {voteCount}/{totalCount} votes!");
        }
        else
        {
            await ctx.RespondAsync("Vote has failed, not muting.");
        }
    }
}