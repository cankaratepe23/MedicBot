using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MedicBot.Exceptions;
using MedicBot.Manager;
using MedicBot.Utils;
using Serilog;

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
            return;
        }

        await ctx.Message.RespondThumbsUpAsync();
    }

    // TODO: Overloads for the add comment: Attachment, youtube link maybe?, id of message with attachment 
    [Command("add")]
    public async Task AddCommand(CommandContext ctx, [RemainingText] string audioName)
    {
        if (!audioName.IsValidFileName())
        {
            Log.Warning("{Filename} has invalid characters", audioName);
            await ctx.RespondAsync($"Filename: {audioName} has invalid characters.");
            return;
        }

        if (ctx.Message.Attachments.Count == 0)
        {
            Log.Warning("No attachments found in {Message}", ctx.Message);
            await ctx.RespondAsync("This command requires an attachment.");
            return;
        }

        var attachmentUrl = ctx.Message.Attachments[0].Url;
        await AudioManager.AddAsync(audioName, ctx.Member.Id, attachmentUrl);
    }

    [Command("play")]
    public async Task PlayCommand(CommandContext ctx, [RemainingText] string audioName)
    {
        try
        {
            await AudioManager.PlayAsync(audioName, ctx.Guild);
        }
        catch (AudioTrackNotFoundException e)
        {
            await ctx.RespondAsync(e.Message);
        }
    }
}