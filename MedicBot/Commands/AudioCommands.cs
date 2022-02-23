using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MedicBot.Manager;

namespace MedicBot.Commands;

public class AudioCommands : BaseCommandModule
{
    // TODO Support cases where voice channel name is the same as the name of a text channel.
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
}