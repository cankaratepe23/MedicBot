using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;

namespace MedicBot.Commands;

public class AudioCommands : BaseCommandModule
{
    // TODO Support joining user's connected channel when no command argument is given.
    // TODO Support cases where voice channel name is the same as the name of a text channel.
    [Command("join")]
    public async Task JoinCommand(CommandContext ctx, DiscordChannel channel)
    {
        var lava = ctx.Client.GetLavalink();
        if (!lava.ConnectedNodes.Any())
        {
            // TODO Add logging
            await ctx.RespondAsync("Lavalink connection not established.");
            return;
        }

        if (channel.Type != ChannelType.Voice)
        {
            // TODO Add logging
            await ctx.RespondAsync("Not a voice channel.");
            return;
        }

        var node = lava.GetIdealNodeConnection();

        await node.ConnectAsync(channel);
        await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsup"));
    }
    // TODO Test leave functionality.
}