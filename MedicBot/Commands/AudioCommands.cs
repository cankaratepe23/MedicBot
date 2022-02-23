using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MedicBot.Manager;

namespace MedicBot.Commands;

public class AudioCommands : BaseCommandModule
{
    // TODO Support joining user's connected channel when no command argument is given.
    // TODO Support cases where voice channel name is the same as the name of a text channel.
    [Command("join")]
    public async Task JoinCommand(CommandContext ctx, DiscordChannel channel)
    {
        await AudioManager.JoinAsync(ctx, channel);
    }
    // TODO Test leave functionality.
}