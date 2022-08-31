using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
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
            await AudioManager.AddAsync(audioName, ctx.Member.Id, ctx.Message.GetFirstAttachment().Url);
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
            await AudioManager.AddAsync(audioName, ctx.Member.Id, message.GetFirstAttachment().Url);
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
    public async Task PlayCommand(CommandContext ctx, [RemainingText] string audioName)
    {
        try
        {
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
    public async Task ListCommand(CommandContext ctx, [RemainingText] string audioName)
    {
        try
        {
            var matchingTracks = AudioManager.FindAsync(audioName).Select(t => t.Name);
            await ctx.RespondAsync(string.Join(";", matchingTracks));
        }
        catch (Exception e)
        {
            await ctx.RespondAsync(e.Message);
            throw;
        }
    }
}