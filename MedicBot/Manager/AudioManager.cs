﻿using System.Diagnostics;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using Serilog;

namespace MedicBot.Manager;

public static class AudioManager
{
    private static DiscordClient Client { get; set; }

    public static void Init(DiscordClient client)
    {
        Client ??= client;
    }

    // Normally called by CommandsNext
    public static async Task JoinAsync(CommandContext ctx, DiscordChannel channel)
    {
        var lava = ctx.Client.GetLavalink();
        if (!lava.ConnectedNodes.Any())
        {
            Log.Warning("Join() called before a Lavalink connection established");
            await ctx.RespondAsync("Lavalink connection not established.");
            return;
        }

        if (channel.Type != ChannelType.Voice)
        {
            Log.Warning("Not a voice channel: {Channel}", channel);
            await ctx.RespondAsync("Not a voice channel.");
            return;
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
            Log.Warning("Join() called before a Lavalink connection established");
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
            Log.Warning("Join() couldn't find the most crowded channel in {Guild}", guild);
            return;
        }

        var node = lava.GetIdealNodeConnection();
        await node.ConnectAsync(mostCrowdedVoiceChannel);
        Log.Information("Voice connected to {Channel}", mostCrowdedVoiceChannel);
    }
}