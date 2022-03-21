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
            return;
        }

        await ctx.Message.RespondThumbsUpAsync();
    }

    // TODO: Overloads for the add comment: Attachment, youtube link maybe?, id of message with attachment 
    [Command("add")]
    public async Task AddCommand(CommandContext ctx, [RemainingText] string audioName)
    {
        // TODO Maybe move all error checking into AudioManager, like it is done with the Play commands.
        if (!audioName.IsValidFileName())
        {
            Log.Warning("{Filename} has invalid characters", audioName);
            await ctx.RespondAsync($"Filename: {audioName} has invalid characters.");
            return;
        }

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

        var attachmentUrl = ctx.Message.Attachments[0].Url;
        if (attachmentUrl.LastIndexOf('.') == -1 || string.IsNullOrWhiteSpace(attachmentUrl[attachmentUrl.LastIndexOf('.')..]))
        {
            Log.Warning("Discord attachment doesn't have a file extension");
            await ctx.RespondAsync(
                "The file you sent has no extension. Please add a valid extension to the file before sending it.");
        }
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