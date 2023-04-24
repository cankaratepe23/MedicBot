using DSharpPlus;
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

    public static async Task AddAsync(string audioName, ulong userId, string url)
    {
        if (!audioName.IsValidFileName())
        {
            Log.Warning("{Filename} has invalid characters", audioName);
            throw new ArgumentException($"Filename: {audioName} has invalid characters.");
        }

        if (url.LastIndexOf('.') == -1 || string.IsNullOrWhiteSpace(url[url.LastIndexOf('.')..]))
        {
            Log.Warning("Discord attachment doesn't have a file extension");
            throw new ArgumentException(
                "The file you sent has no extension. Please add a valid extension to the file before sending it.");
        }

        if (AudioRepository.NameExists(audioName))
        {
            Log.Warning("An AudioTrack with the name {AudioName} already exists", audioName);
            throw new AudioTrackExistsException($"An AudioTrack with the name {audioName} already exists.");
        }

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

    public static async Task DeleteAsync(string audioName, ulong userId)
    {
        var audioTrack = AudioRepository.FindByNameExact(audioName);
        if (audioTrack == null)
        {
            Log.Warning("No track was found with name: {Name}", audioName);
            throw new AudioTrackNotFoundException($"No track was found with name: {audioName}");
        }

        if (audioTrack.OwnerId != userId && Client.CurrentUser.Id != userId)
        {
            Log.Warning("A non-owner or non-admin user {UserId} attempted deleting the following track: {@AudioTrack}",
                userId, audioTrack);
            var user = await Client.GetUserAsync(userId);
            if (user != null)
            {
                Log.Warning("Offending user of the unauthorized delete operation: {User}", user);
            }

            throw new UnauthorizedException("You need to be the owner of this audio track to delete it.");
        }

        AudioRepository.Delete(audioTrack.Id);
        File.Delete(audioTrack.Path);
    }

    public static IEnumerable<AudioTrack> FindAsync(string searchQuery)
    {
        // TODO Allow searching by ID with a special prefix or something
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            return AudioRepository.All(); // TODO Pagination and DM for large messages
        }
        return AudioRepository.FindAllByName(searchQuery);
    }

    // TODO Add summary docs for everything

    #region Join

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

    public static async Task JoinGuildIdAsync(ulong guildId)
    {
        await JoinGuildAsync(Client.FindGuild(guildId));
    }

    public static async Task JoinGuildAsync(DiscordGuild guild)
    {
        // TODO Add option to choose a default channel for a guild and join that if no user is in any voice channel.
        var mostCrowdedVoiceChannel = guild.Channels.VoiceChannelWithMostNonBotUsers();
        if (mostCrowdedVoiceChannel == null)
        {
            Log.Warning("JoinGuildIdAsync() couldn't find the most crowded channel in {Guild}", guild);
            throw new ChannelNotFoundException($"Couldn't find the most crowded channel in {guild}");
        }

        await JoinAsync(mostCrowdedVoiceChannel);
    }

    #endregion

    #region Leave

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
        await LeaveAsync(Client.FindGuild(guildId));
    }

    #endregion

    #region Play

    public static async Task PlayAsync(AudioTrack audioTrack, DiscordGuild guild, DiscordMember member)
    {
        if (!UserManager.CanPlayAudio(member, audioTrack))
        {
            Log.Warning("{Member} can not play the audio {AudioTrack}", member, audioTrack);
            throw new UnauthorizedException($"{member} can not play the audio {audioTrack}");
        }

        var lava = Client.GetLavalink();

        var connection = lava.GetGuildConnection(guild);
        if (connection == null)
        {
            Log.Information("Play was called when bot was not in a voice channel");
            Log.Information("Trying to join a voice channel");
            var channelToJoin = guild.Channels.VoiceChannelWithMostNonBotUsers();
            if (channelToJoin == null)
            {
                Log.Warning("JoinGuildIdAsync() couldn't find the most crowded channel in {Guild}", guild);
                throw new ChannelNotFoundException($"Couldn't find the most crowded channel in {guild}");
            }

            await JoinAsync(channelToJoin);
            connection = lava.GetGuildConnection(guild);
            if (connection == null)
            {
                Log.Error("Bot couldn't join the most crowded channel, but no exception was thrown");
                throw new Exception("Fatal: Bot was not in a voice channel when play was called, " +
                                    "and it couldn't join the most crowded channel either," +
                                    "but it threw no exceptions when it was supposed to.");
            }
        }

        var audioFile = new FileInfo(audioTrack.Path);
        if (!audioFile.Exists)
        {
            Log.Warning("File {FilePath} does not exist, cannot play", audioFile.FullName);
            throw new FileNotFoundException($"File does not exist: {audioFile.FullName}");
        }
        var result = await connection.GetTracksAsync(audioFile);
        if (result.LoadResultType != LavalinkLoadResultType.TrackLoaded)
        {
            Log.Warning("Lavalink failed to load the track {@Track} with failure type: {Type}", audioTrack,
                result.LoadResultType);
            throw new LavalinkLoadFailedException(result.LoadResultType.ToString());
        }

        await connection.PlayAsync(result.Tracks.FirstOrDefault());
        // TODO Add green circle (voice state update) during playback.
    }

    public static async Task PlayAsync(string audioName, DiscordGuild guild, DiscordMember member,
        bool searchById = false)
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

        await PlayAsync(audioTrack, guild, member);
    }

    public static async Task PlayAsync(string audioNameOrId, ulong guildId, DiscordMember member,
        bool searchById = false)
    {
        var guild = Client.FindGuild(guildId);
        await PlayAsync(audioNameOrId, guild, member, searchById);
    }

    #endregion
}