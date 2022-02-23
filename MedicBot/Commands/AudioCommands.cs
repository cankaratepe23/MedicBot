using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MedicBot.Manager;

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
}