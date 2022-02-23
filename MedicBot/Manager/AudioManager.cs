using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using MedicBot.Utils;
using Serilog;

namespace MedicBot.Manager;

public static class AudioManager
{
    private static DiscordClient Client { get; set; }

    public static void Init(DiscordClient client)
    {
        Client = client;
    }

    // Normally called by CommandsNext
    public static async Task JoinAsync(CommandContext ctx, DiscordChannel channel)
    {
        var lava = ctx.Client.GetLavalink();
        if (!lava.ConnectedNodes.Any())
        {
            Log.Warning(Constants.JoinAsyncLavalinkNotConnectedLog);
            await ctx.RespondAsync(Constants.JoinAsyncLavalinkNotConnectedMessage);
            return;
        }

        if (channel.Type != ChannelType.Voice)
        {
            // Handle cases where one text and one voice channel may exist with the same name.
            var alternateChannel = ctx.Guild.Channels.Values.FirstOrDefault(c =>
                string.Equals(c.Name, channel.Name, StringComparison.CurrentCultureIgnoreCase) &&
                c.Type == ChannelType.Voice);
            if (alternateChannel == null)
            {
                Log.Warning("Not a voice channel: {Channel}", channel);
                await ctx.RespondAsync(Constants.NotVoiceChannel);
                return;
            }

            channel = alternateChannel;
        }

        var node = lava.GetIdealNodeConnection();
        await node.ConnectAsync(channel);
        Log.Information("Voice connected to {Channel}", channel);
        await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsup"));
    }

    // Join the voice channel with the largest number of connected non-bot users.
    // May be called from external sources, such as a REST API.
    public static async Task JoinAsync(ulong guildId)
    {
        var lava = Client.GetLavalink();
        if (!lava.ConnectedNodes.Any())
        {
            Log.Warning(Constants.JoinAsyncLavalinkNotConnectedLog);
            return;
        }

        var guildExists = Client.Guilds.TryGetValue(guildId, out var guild);
        if (!guildExists)
        {
            Log.Warning("Guild with ID: {Id} not found", guildId);
            return;
        }

        var mostCrowdedVoiceChannel = guild!.Channels.Values
            .Where(c => c.Type == ChannelType.Voice)
            .OrderByDescending(c => c.Users.Count(u => !u.IsBot))
            .FirstOrDefault();
        if (mostCrowdedVoiceChannel == null)
        {
            Log.Warning("JoinAsync() couldn't find the most crowded channel in {Guild}", guild);
            return;
        }

        var node = lava.GetIdealNodeConnection();
        await node.ConnectAsync(mostCrowdedVoiceChannel);
        Log.Information("Voice connected to {Channel}", mostCrowdedVoiceChannel);
    }

    // Called by CommandsNext
    public static async Task LeaveAsync(CommandContext ctx)
    {
        var lava = ctx.Client.GetLavalink();
        if (!lava.ConnectedNodes.Any())
        {
            Log.Warning(Constants.LeaveAsyncLavalinkNotConnectedLog);
            await ctx.RespondAsync(Constants.LeaveAsyncLavalinkNotConnectedMessage);
            return;
        }

        var connection = lava.GetGuildConnection(ctx.Guild);
        if (connection == null)
        {
            Log.Warning(Constants.NotConnectedToVoiceLog);
            await ctx.RespondAsync(Constants.NotConnectedToVoiceMessage);
            return;
        }

        await connection.DisconnectAsync();
        Log.Information("Voice disconnected from {Channel}", connection.Channel);
        await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsup"));
    }

    public static async Task LeaveAsync(ulong guildId)
    {
        var lava = Client.GetLavalink();
        if (!lava.ConnectedNodes.Any())
        {
            Log.Warning(Constants.LeaveAsyncLavalinkNotConnectedLog);
            return;
        }

        var guildExists = Client.Guilds.TryGetValue(guildId, out var guild);
        if (!guildExists)
        {
            Log.Warning("Guild with ID: {Id} not found", guildId);
            return;
        }

        var connection = lava.GetGuildConnection(guild);
        if (connection == null)
        {
            Log.Warning(Constants.NotConnectedToVoiceLog);
            return;
        }

        await connection.DisconnectAsync();
        Log.Information("Voice disconnected from {Channel}", connection.Channel);
    }
}