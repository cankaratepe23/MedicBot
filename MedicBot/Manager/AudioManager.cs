﻿using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using MedicBot.Exceptions;
using MedicBot.Model;
using MedicBot.Repository;
using MedicBot.Utils;
using Serilog;

namespace MedicBot.Manager;

public static class AudioManager
{
    private static DiscordClient Client { get; set; } = null!;
    private static string AudioTracksFullPath { get; set; } = null!;

    public static void Init(DiscordClient client, string fullPath)
    {
        Client = client;
        AudioTracksFullPath = fullPath;
    }

    // TODO Add summary docs for everything
    public static async Task JoinAsync(DiscordChannel channel)
    {
        var lava = Client.GetLavalink();
        if (!lava.ConnectedNodes.Any())
        {
            Log.Warning(Constants.JoinAsyncLavalinkNotConnectedLog);
            throw new LavalinkNotConnectedException(Constants.JoinAsyncLavalinkNotConnectedLog);
        }

        if (channel.Type != ChannelType.Voice)
        {
            // Handle cases where one text and one voice channel may exist with the same name.
            var alternateChannel = channel.Guild.Channels.Values.FirstOrDefault(c =>
                string.Equals(c.Name, channel.Name, StringComparison.CurrentCultureIgnoreCase) &&
                c.Type == ChannelType.Voice);
            if (alternateChannel == null)
            {
                Log.Warning("Not a voice channel: {Channel}", channel);
                throw new InvalidOperationException($"Not a voice channel: {channel}");
            }

            channel = alternateChannel;
        }

        var node = lava.GetIdealNodeConnection();
        await node.ConnectAsync(channel);
        Log.Information("Voice connected to {Channel}", channel);
    }

    public static async Task JoinChannelIdAsync(ulong channelId)
    {
        var guild = Client.Guilds.Values.FirstOrDefault(g => g.Channels.ContainsKey(channelId));
        if (guild == null)
        {
            Log.Warning("Guild containing a channel with ID: {Id} not found", channelId);
            throw new GuildNotFoundException($"Guild containing a channel with ID: {channelId} not found.");
        }

        var channel = guild.Channels[channelId];
        await JoinAsync(channel);
    }

    // Join the voice channel with the largest number of connected non-bot users.
    // TODO Maybe separate this into two methods, one for finding the guild and one for joining the most crowded channel.
    public static async Task JoinGuildIdAsync(ulong guildId)
    {
        var guild = FindGuild(guildId);

        // TODO Add option to choose a default channel for a guild and join that if no user is in any voice channel.
        var mostCrowdedVoiceChannel = guild.Channels.Values
            .Where(c => c.Type == ChannelType.Voice)
            .OrderByDescending(c => c.Users.Count(u => !u.IsBot))
            .FirstOrDefault();
        if (mostCrowdedVoiceChannel == null)
        {
            Log.Warning("JoinGuildIdAsync() couldn't find the most crowded channel in {Guild}", guild);
            throw new ChannelNotFoundException($"Couldn't find the most crowded channel in {guild}");
        }

        await JoinAsync(mostCrowdedVoiceChannel);
    }

    public static async Task LeaveAsync(DiscordGuild guild)
    {
        var lava = Client.GetLavalink();
        if (!lava.ConnectedNodes.Any())
        {
            Log.Warning(Constants.LeaveAsyncLavalinkNotConnectedLog);
            throw new Exception(Constants.LeaveAsyncLavalinkNotConnectedMessage);
        }

        var connection = lava.GetGuildConnection(guild);
        if (connection == null)
        {
            Log.Warning(Constants.NotConnectedToVoiceLog);
            throw new Exception(Constants.NotConnectedToVoiceMessage);
        }

        await connection.DisconnectAsync();
        Log.Information("Voice disconnected from {Channel}", connection.Channel);
    }

    public static async Task LeaveAsync(ulong guildId)
    {
        await LeaveAsync(FindGuild(guildId));
    }

    public static async Task AddAsync(string audioName, ulong userId, string url)
    {
        // TODO Add error checking for files with no extension
        // TODO Add duplicate audio name checking
        var fileExtension = url[url.LastIndexOf('.')..];
        var fileName = audioName + fileExtension;
        var filePath = Path.Combine(AudioTracksFullPath, fileName);
        {
            using var client = new HttpClient();
            await using var stream = await client.GetStreamAsync(url);
            await using var fileStream = File.OpenWrite(filePath);
            await stream.CopyToAsync(fileStream);
            await fileStream.FlushAsync();
        }
        AudioRepository.Add(new AudioTrack(audioName, filePath, userId));
    }

    public static async Task PlayAsync(AudioTrack audioTrack, DiscordGuild guild)
    {
        var lava = Client.GetLavalink();

        var connection = lava.GetGuildConnection(guild);
        if (connection == null)
        {
            Log.Warning(Constants.NotConnectedToVoiceLog);
            // TODO throw new exception;;
            return;
        }

        var audioFile = new FileInfo(audioTrack.Path);
        var result = await connection.GetTracksAsync(audioFile);
        if (result.LoadResultType == LavalinkLoadResultType.TrackLoaded)
            await connection.PlayAsync(result.Tracks.FirstOrDefault());
    }


    public static async Task PlayAsync(string audioName, DiscordGuild guild, bool searchById = false)
    {
        var audioTrack = searchById
            ? AudioRepository.FindById(audioName)
            : AudioRepository.FindByName(audioName);
        if (audioTrack == null)
        {
            Log.Warning("No track was found with {IdOrName}: {Id}", searchById ? "ID" : "name", audioName);
            throw new AudioTrackNotFoundException(
                $"No track was found with {(searchById ? "ID" : "name")}: {audioName}");
        }

        await PlayAsync(audioTrack, guild);
    }

    public static async Task PlayAsync(AudioTrack audioTrack, ulong guildId)
    {
        var guild = FindGuild(guildId);
        await PlayAsync(audioTrack, guild);
    }

    public static async Task PlayAsync(string audioNameOrId, ulong guildId, bool searchById = false)
    {
        var guild = FindGuild(guildId);
        await PlayAsync(audioNameOrId, guild, searchById);
    }

    /// <summary>
    ///     Finds a guild with the given guildId. Wraps Client.Guilds.TryGetValue() with logging and exception
    ///     that will be thrown if the guild cannot be found.
    /// </summary>
    /// <param name="guildId">The guild ID to search for.</param>
    /// <returns>The Guild object with the given ID.</returns>
    /// <exception cref="Exception">Exception with user-friendly message stating the guild cannot be found.</exception>
    private static DiscordGuild FindGuild(ulong guildId)
    {
        var guildExists = Client.Guilds.TryGetValue(guildId, out var guild);
        if (guildExists && guild != null) return guild;
        Log.Warning("Guild with ID: {Id} not found", guildId);
        throw new Exception($"Guild with ID: {guildId} not found");
    }
}