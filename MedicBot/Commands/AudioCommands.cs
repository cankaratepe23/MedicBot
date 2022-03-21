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

    // TODO: Overloads for the add comment: Attachment, youtube link maybe?, id of message with attachment 
    [Command("add")]
    public async Task AddCommand(CommandContext ctx, [RemainingText] string audioName)
    {
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

        try
        {
            await AudioManager.AddAsync(audioName, ctx.Member.Id, ctx.Message.Attachments[0].Url);
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
            await AudioManager.PlayAsync(audioName, ctx.Guild);
        }
        catch (AudioTrackNotFoundException e)
        {
            await ctx.RespondAsync(e.Message);
        }
    }
}