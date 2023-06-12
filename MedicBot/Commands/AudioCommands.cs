using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using MedicBot.Exceptions;
using MedicBot.Manager;
using MedicBot.Utils;

namespace MedicBot.Commands;

public class AudioCommands : BaseCommandModule
{
    [Command("join")]
    public async Task JoinCommand(CommandContext ctx, DiscordChannel channel)
    {
        try
        {
            await AudioManager.JoinAsync(channel);
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
            await AudioManager.JoinGuildAsync(ctx.Guild);
        }
        catch (Exception e)
        {
            await ctx.RespondAsync(e.Message);
        }
    }

    [Command("leave")]
    public async Task LeaveCommand(CommandContext ctx)
    {
        try
        {
            await AudioManager.LeaveAsync(ctx.Guild);
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
            await AudioManager.AddAsync(audioName, ctx.Message.Author.Id, ctx.Message.GetFirstAttachment().Url);
        }
        catch (Exception e)
        {
            await ctx.RespondAsync(e.Message);
            return;
        }

        await ctx.Message.RespondThumbsUpAsync();
    }

    // This overload needs to have higher priority than the AddCommand(CommandContext, string) overload,
    // because if not, the message id is parsed as part of the string.
    [Command("add")]
    [Priority(1)]
    public async Task AddCommand(CommandContext ctx, DiscordMessage message, [RemainingText] string audioName)
    {
        try
        {
            await AudioManager.AddAsync(audioName, ctx.Message.Author.Id, message.GetFirstAttachment().Url);
        }
        catch (Exception e)
        {
            await ctx.RespondAsync(e.Message);
            return;
        }

        await ctx.Message.RespondThumbsUpAsync();
    }

    // TODO Download from YouTube link

    [Command("delete")]
    public async Task DeleteCommand(CommandContext ctx, [RemainingText] string audioName)
    {
        try
        {
            await AudioManager.DeleteAsync(audioName, ctx.User.Id);
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
                // TODO Support DMs for commands
                await ctx.RespondAsync("Direct messages are not supported yet");
                return;
            }

            await AudioManager.PlayAsync(audioName, ctx.Guild, ctx.Member);
        }
        catch (AudioTrackNotFoundException e)
        {
            await ctx.RespondAsync(e.Message);
        }
        catch (LavalinkLoadFailedException e)
        {
            await ctx.RespondAsync($"Lavalink failed to load the track with error: {e.Message}");
        }
    }

    [Command("list")]
    [Aliases("search")]
    public async Task ListCommand(CommandContext ctx, string searchTerm = "", long limit = 10)
    {
        try
        {
            var matchingTracks = (await AudioManager.FindAsync(searchTerm, limit)).ToList();
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
            var newTracks = AudioManager.GetNewTracksAsync(limit);
            await ctx.Channel.SendPaginatedMessageAsync(ctx.User,
                ctx.Client.GetInteractivity().GeneratePagesInEmbed(string.Join("\n", newTracks)));
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
            var lastPlayedTracks = AudioManager.GetLastPlayedTracks(ctx.Guild);
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
}