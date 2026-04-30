using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using MedicBot.Exceptions;
using MedicBot.Manager;
using MedicBot.Repository;
using MedicBot.Utils;

namespace MedicBot.Commands;

public class AudioCommands : BaseCommandModule
{
    private readonly IAudioManager _audioManager;
    private readonly IUserManager _userManager;
    private readonly ISettingsRepository _settingsRepository;

    public AudioCommands(IAudioManager audioManager, IUserManager userManager, ISettingsRepository settingsRepository)
    {
        _audioManager = audioManager;
        _userManager = userManager;
        _settingsRepository = settingsRepository;
    }

    [Command("join")]
    public async Task JoinCommand(CommandContext ctx, DiscordChannel channel)
    {
        try
        {
            await _audioManager.JoinAsync(channel);
        }
        catch (Exception e)
        {
            await ctx.RespondAsync(e.Message);
            return;
        }

        await ctx.Message.RespondThumbsUpAsync();
    }

    [Command("join")]
    public async Task JoinCommand(CommandContext ctx)
    {
        try
        {
            await _audioManager.JoinGuildAsync(ctx.Guild);
        }
        catch (Exception e)
        {
            await ctx.RespondAsync(e.Message);
        }
    }

    [Command("leave")]
    [Aliases("dc")]
    public async Task LeaveCommand(CommandContext ctx)
    {
        try
        {
            await _audioManager.LeaveAsync(ctx.Guild);
        }
        catch (Exception e)
        {
            await ctx.RespondAsync(e.Message);
        }
    }

    [Command("add")]
    [Priority(0)]
    public async Task AddCommand(CommandContext ctx, [RemainingText] string audioName)
    {
        try
        {
            await _audioManager.AddAsync(audioName, ctx.Message.Author.Id, ctx.Message.GetFirstAttachment().Url);
        }
        catch (Exception e)
        {
            await ctx.RespondAsync(e.Message);
            return;
        }

        await ctx.Message.RespondThumbsUpAsync();
    }

    [Command("add")]
    [Priority(1)]
    public async Task AddCommand(CommandContext ctx, DiscordMessage message, [RemainingText] string audioName)
    {
        try
        {
            await _audioManager.AddAsync(audioName, ctx.Message.Author.Id, message.GetFirstAttachment().Url);
        }
        catch (Exception e)
        {
            await ctx.RespondAsync(e.Message);
            return;
        }

        await ctx.Message.RespondThumbsUpAsync();
    }

    [Command("delete")]
    public async Task DeleteCommand(CommandContext ctx, [RemainingText] string audioName)
    {
        try
        {
            await _audioManager.DeleteAsync(audioName, ctx.User.Id);
        }
        catch (Exception e)
        {
            await ctx.RespondAsync(e.Message);
            return;
        }

        await ctx.Message.RespondThumbsUpAsync();
    }

    [Command("play")]
    public async Task PlayCommand(CommandContext ctx)
    {
        await PlayCommand(ctx, "");
    }

    [Command("play")]
    public async Task PlayCommand(CommandContext ctx, [RemainingText] string audioName)
    {
        try
        {
            if (ctx.Member is null)
            {
                await ctx.RespondAsync("Direct messages are not supported yet");
                return;
            }

            await _audioManager.PlayAsync(audioName, ctx.Guild, ctx.Member, ctx);
        }
        catch (AudioTrackNotFoundException e)
        {
            await ctx.RespondAsync(e.Message);
        }
        catch (LavalinkLoadFailedException e)
        {
            await ctx.RespondAsync($"Lavalink failed to load the track with error: {e.Message}");
        }
        catch (UnauthorizedException e)
        {
            await ctx.RespondAsync($"You cannot play this audio right now. {e.InnerException?.Message}".Trim());
        }
    }

    [Command("list")]
    [Aliases("search")]
    public async Task ListCommand(CommandContext ctx, string searchTerm = "", long limit = 10)
    {
        try
        {
            var matchingTracks = (await _audioManager.FindAsync(searchTerm, limit, ctx.Guild, ctx.User.Id)).ToList();
            if (matchingTracks.Count == 0)
            {
                await ctx.RespondAsync("No matching tracks found");
                return;
            }

            await ctx.Channel.SendPaginatedMessageAsync(ctx.User,
                ctx.Client.GetInteractivity().GeneratePagesInEmbed(string.Join("\n", matchingTracks)));
        }
        catch (Exception e)
        {
            await ctx.RespondAsync(e.Message);
            throw;
        }
    }

    [Command("news")]
    public async Task NewsCommand(CommandContext ctx, long limit = 10)
    {
        try
        {
            var newTracks = _audioManager.GetNewTracksAsync(limit);
            await ctx.Channel.SendPaginatedMessageAsync(ctx.User,
                ctx.Client.GetInteractivity().GeneratePagesInEmbed(string.Join("\n", newTracks)));
        }
        catch (Exception e)
        {
            await ctx.RespondAsync(e.Message);
            throw;
        }
    }

    [Command("recents")]
    public async Task RecentsCommand(CommandContext ctx)
    {
        try
        {
            var recentTracks = _audioManager.GetRecentAudioTracks(ctx.User);
            await ctx.Channel.SendPaginatedMessageAsync(ctx.User, ctx.Client.GetInteractivity().GeneratePagesInEmbed(string.Join("\n", recentTracks)));
        }
        catch (Exception e)
        {
            await ctx.RespondAsync(e.Message);
            throw;
        }
    }

    [Command("history")]
    public async Task HistoryCommand(CommandContext ctx)
    {
        try
        {
            var lastPlayedTracks = _audioManager.GetLastPlayedTracks(ctx.Guild);
            if (lastPlayedTracks is null)
            {
                await ctx.RespondAsync("No tracks have been played yet");
                return;
            }

            await ctx.Channel.SendPaginatedMessageAsync(ctx.User,
                ctx.Client.GetInteractivity().GeneratePagesInEmbed(string.Join("\n", lastPlayedTracks)));
        }
        catch (Exception e)
        {
            await ctx.RespondAsync(e.Message);
            throw;
        }
    }

    [Command("tag")]
    [Aliases("collection", "koleksiyon")]
    public async Task TagCommand(CommandContext ctx, [RemainingText] string audioName)
    {
        try
        {
            var foundTrack = (await _audioManager.FindAsync(audioName, 1, ctx.Guild, ctx.User.Id)).FirstOrDefault();
            if (foundTrack == null)
            {
                throw new AudioTrackNotFoundException(audioName, false);
            }
            await ctx.RespondAsync($"Which tag should I add to audio `{foundTrack.Name}`?"
                                    + $"\nThis track currently has {(foundTrack.Tags.Count == 0 ? "no tags." : $"the following tags: `{string.Join(", ", foundTrack.Tags)}`")}"
                                    + $"\nReply 'X' to cancel.`");
            var reply = await ctx.Message.GetNextMessageAsync(m =>
            {
                return m.Content.ToLower() != "x";
            });

            if (!reply.TimedOut)
            {
                var replyContent = reply.Result.Content.Trim().ToLower();
                _audioManager.AddTag(foundTrack, replyContent);
                await reply.Result.RespondAsync("Added tag " + replyContent + " to " + foundTrack.Name);
            }
            else
            {
                await ctx.RespondAsync("Timed out.");
            }
        }
        catch (Exception e)
        {
            await ctx.RespondAsync(e.Message);
            throw;
        }
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
            _userManager.Mute(memberToMute, minutes);
            await ctx.RespondAsync($"{memberToMute.Mention} you have been muted with {voteCount}/{totalCount} votes!");
        }
        else
        {
            await ctx.RespondAsync("Vote has failed, not muting.");
        }
    }

    [Command("balance")]
    [Aliases("puan", "points")]
    public async Task BalanceCommand(CommandContext ctx, DiscordUser? member = null)
    {
        if (member == null)
        {
            member = ctx.User;
        }

        var memberPoints = Convert.ToDouble(_userManager.GetPoints(member));
        var defaultPrice = _settingsRepository.GetValue<int>(Constants.DefaultScore);

        await ctx.RespondAsync($"You have {memberPoints} points, which means you can play around {Math.Floor(memberPoints / defaultPrice)} tracks.");
    }

    [Command("price")]
    [Aliases("fiyat")]
    public async Task PriceCommand(CommandContext ctx, [RemainingText] string searchTerm)
    {
        try
        {
            var matchingTracks = (await _audioManager.FindAsync(searchTerm, 1, ctx.Guild, ctx.User.Id)).ToList();
            if (matchingTracks.Count == 0)
            {
                await ctx.RespondAsync("No matching tracks found");
                return;
            }
            
            var matchingTrack = matchingTracks[0];
            var effectivePrice = matchingTrack.CalculateAndDecreasePrice(_settingsRepository);
            await ctx.RespondAsync($"Current price for `{matchingTrack.Name}`: {effectivePrice}");
        }
        catch (Exception e)
        {
            await ctx.RespondAsync(e.Message);
            throw;
        }
    }
}