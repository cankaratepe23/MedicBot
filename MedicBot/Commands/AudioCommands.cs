using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MedicBot.Manager;
using MedicBot.Utils;

namespace MedicBot.Commands;

public class AudioCommands : BaseCommandModule
{
    [Command("join")]
    public async Task JoinCommand(CommandContext ctx, DiscordChannel channel)
    {
        await AudioManager.JoinAsync(ctx, channel);
    }

    [Command("leave")]
    public async Task LeaveCommand(CommandContext ctx)
    {
        await AudioManager.LeaveAsync(ctx);
    }

    // TODO: Overloads for the add comment: Attachment, youtube link maybe?, id of message with attachment 
    [Command("add")]
    public async Task AddCommand(CommandContext ctx, [RemainingText] string audioName)
    {
        if (!audioName.IsValidFileName())
            // TODO Improve message add logging.
            throw new ArgumentException("File name has invalid characters.");
        if (ctx.Message.Attachments.Count == 0)
            // TODO Improve message add logging.
            throw new ArgumentException("This overload requires an attachment.");

        var attachmentUrl = ctx.Message.Attachments[0].Url;
        await AudioManager.AddAsync(audioName, ctx.Member.Id, attachmentUrl);
    }
}