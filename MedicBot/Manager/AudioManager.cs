using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using MedicBot.Model;
using MedicBot.Repository;
using MedicBot.Utils;
using Serilog;

namespace MedicBot.Manager;

public static class AudioManager
{
    private static DiscordClient Client { get; set; }
    private static string AudioTracksFullPath { get; set; }

    public static void Init(DiscordClient client, string fullPath)
    {
        Client = client;
        AudioTracksFullPath = fullPath;
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
        await ctx.Message.RespondThumbsUpAsync();
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
    // TODO Maybe we can avoid passing ctx here as a parameter and pass guild id instead
    // TODO that way we might be able to reduce the number of methods to just one
    // TODO Responding to the user would be done at Commands level
    public static async Task LeaveAsync(CommandContext ctx)
    {
        var lava = ctx.Client.GetLavalink();
        if (!lava.ConnectedNodes.Any())
        {
            Log.Warning(Constants.LeaveAsyncLavalinkNotConnectedLog);
            await ctx.RespondAsync(Constants.LeaveAsyncLavalinkNotConnectedMessage);
            return;
        }

        // TODO Somehow centralize the method and error logging to reusable methods here.
        var connection = lava.GetGuildConnection(ctx.Guild);
        if (connection == null)
        {
            Log.Warning(Constants.NotConnectedToVoiceLog);
            await ctx.RespondAsync(Constants.NotConnectedToVoiceMessage);
            return;
        }

        await connection.DisconnectAsync();
        Log.Information("Voice disconnected from {Channel}", connection.Channel);
        await ctx.Message.RespondThumbsUpAsync();
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

    public static async Task AddAsync(string audioName, ulong userId, string url)
    {
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
        AudioRepository.Add(new AudioTrack
        {
            Name = audioName,
            Path = filePath,
            OwnerId = userId
        });
    }

    public static async Task PlayAsync(AudioTrack audioTrack, ulong guildId)
    {
        var lava = Client.GetLavalink();
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

        var audioFile = new FileInfo(audioTrack.Path);
        var result = await connection.GetTracksAsync(audioFile);
        if (result.LoadResultType == LavalinkLoadResultType.TrackLoaded)
        {
            await connection.PlayAsync(result.Tracks.FirstOrDefault());
        }
    }
}