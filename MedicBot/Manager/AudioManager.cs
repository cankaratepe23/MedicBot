using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using MedicBot.Exceptions;
using MedicBot.Model;
using MedicBot.Repository;
using MedicBot.Utils;
using Serilog;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Readers;

namespace MedicBot.Manager;

public static class AudioManager
{
    private static DiscordClient Client { get; set; } = null!;
    private static string AudioTracksPath { get; set; } = null!;
    private static string TempFilesPath { get; set; } = null!;

    private static Dictionary<ulong, Queue<AudioTrack>> LastPlayedTracks { get; } = new();

    public static void Init(DiscordClient client, string tracksPath, string tempFilesPath)
    {
        Client = client;
        AudioTracksPath = tracksPath;
        TempFilesPath = tempFilesPath;
    }

    public static async Task AddAsync(string audioName, ulong userId, string url)
    {
        if (!audioName.IsValidFileName())
        {
            Log.Warning("{Filename} has invalid characters", audioName);
            throw new ArgumentException($"Filename: {audioName} has invalid characters.");
        }

        var parsedUrl = new Uri(url);

        var fileExtension = Path.GetExtension(parsedUrl.AbsolutePath);

        if (string.IsNullOrEmpty(fileExtension))
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

        Log.Information("Detected file extension: {FileExtension}", fileExtension);
        var fileName = audioName + fileExtension;
        var filePath = fileExtension.ToLowerInvariant() == ".7z"
            ? string.Join('/', TempFilesPath, fileName)
            : string.Join('/', AudioTracksPath, fileName);

        {
            Log.Information("Downloading: {DownloadUrl}", url);
            Log.Information("Downloading file to {FilePath}", filePath);
            await using var stream = await Program.Client.GetStreamAsync(url);
            await using var fileStream = File.OpenWrite(filePath);
            await stream.CopyToAsync(fileStream);
            await fileStream.FlushAsync();
        }

        if (fileExtension.ToLowerInvariant() == ".7z")
        {
            Log.Information("Extracting 7z...");
            var filesDirectory = string.Join('/', TempFilesPath, audioName);
            Directory.CreateDirectory(filesDirectory);

            using (var archive = SevenZipArchive.Open(filePath))
            using (var reader = archive.ExtractAllEntries())
            {
                reader.WriteAllToDirectory(filesDirectory);
            }

            foreach (var file in Directory.GetFiles(filesDirectory))
            {
                var newAudioFileName = Path.GetFileName(file);
                var newAudioName = Path.GetFileNameWithoutExtension(newAudioFileName);
                Log.Information("Adding {AudioTrackName}", newAudioName);
                var newFilePath = string.Join('/', AudioTracksPath, newAudioFileName);
                File.Move(file, newFilePath);
                if (AudioRepository.NameExists(audioName))
                {
                    Log.Warning("An AudioTrack with the name {AudioName} already exists", newAudioName);
                    Log.Warning("Skipping adding track with name {AudioName}", newAudioName);
                    continue;
                }

                AudioRepository.Add(new AudioTrack(newAudioName, newFilePath, userId));
            }
            // TODO? Delete .7z file after done
        }
        else
        {
            AudioRepository.Add(new AudioTrack(audioName, filePath, userId));
        }
    }

    public static void AddTag(AudioTrack audioTrack, string tagName)
    {
        if (audioTrack.Tags.Contains(tagName))
        {
            return;
        }

        audioTrack.Tags.Add(tagName);
        AudioRepository.Update(audioTrack);
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

    public static async Task<IEnumerable<AudioTrack>> FindAsync(string searchQuery, long limit = 10, DiscordGuild? guild = null)
    {
        // TODO Sort results (check if needed first)
        string? tag = null;
        searchQuery = searchQuery.Trim();
        if (searchQuery.Contains(':'))
        {
            var splitQuery = searchQuery.Split(':');
            tag = splitQuery[0];
            searchQuery = splitQuery[1].Trim();
        }

        if (string.IsNullOrWhiteSpace(searchQuery) || searchQuery == "?")
        {
            if (limit == 1)
            {
                var randomTrack = await AudioRepository.Random(tag);
                return new List<AudioTrack> { randomTrack };
            }

            return AudioRepository.All(tag);
        }

        if (searchQuery == "!!" && guild != null)
        {
            var previousTrack = GetLastPlayedTracks(guild)?.FirstOrDefault();
            return previousTrack == null ? new List<AudioTrack>() : new List<AudioTrack> { previousTrack };
        }

        if (searchQuery.StartsWith('\"') && searchQuery.EndsWith('"'))
        {
            return AudioRepository.FindAllByName(searchQuery.Trim('"'), tag);
        }

        return await AudioRepository.FindMany(searchQuery, limit, tag);
    }

    public static AudioTrack? FindById(string id)
    {
        return AudioRepository.FindById(id);
    }

    public static IEnumerable<AudioTrack> GetNewTracksAsync(long limit = 10)
    {
        return AudioRepository.GetOrderedByDate(limit);
    }

    public static DateTimeOffset GetLatestUpdateTime()
    {
        var latestModifiedTrack = AudioRepository.GetOrderedByModified(1).FirstOrDefault();
        if (latestModifiedTrack == null || latestModifiedTrack.LastModifiedAt == null)
        {
            return DateTimeOffset.UtcNow;
        }

        return (DateTimeOffset) latestModifiedTrack.LastModifiedAt;
    }

    public static IEnumerable<AudioTrack>? GetLastPlayedTracks(DiscordGuild guild)
    {
        return LastPlayedTracks.TryGetValue(guild.Id, out var tracks) ? tracks.Reverse() : null;
    }

    public static IEnumerable<RecentAudioTrack> GetRecentTracks(DiscordUser user)
    {
        return GetRecentTracks(user.Id);
    }

    public static IEnumerable<RecentAudioTrack> GetRecentTracks(ulong userId)
    {
        var recents = AudioPlaybackLogRepository.GetUserRecents(userId)
                                                .GroupBy(l => l.AudioTrack.Id)
                                                .Select(g => new RecentAudioTrack()
                                                    {
                                                        AudioTrack = g.First().AudioTrack,
                                                        Count = g.Count()
                                                    });
        return recents;
    }

    private static async Task<LavalinkGuildConnection> GetLavalinkConnection(DiscordGuild guild)
    {
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

        return connection;
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

    public static async Task PlayAsync(AudioTrack audioTrack, DiscordGuild guild, DiscordMember member, CommandContext? ctx = null)
    {
        if (!UserManager.CanPlayAudio(member, audioTrack, out var reason))
        {
            Log.Warning("{Member} can not play the audio {AudioTrack}", member, audioTrack);
            Log.Warning("Reason: {Reason}", reason);
            throw new UnauthorizedException($"{member} can not play the audio {audioTrack}", new UnauthorizedException(reason));
        }

        var connection = await GetLavalinkConnection(guild);

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

        if (!LastPlayedTracks.TryGetValue(guild.Id, out Queue<AudioTrack>? value))
        {
            value = new Queue<AudioTrack>(10);
            LastPlayedTracks.Add(guild.Id, value);
        }

        if (value.Count >= 10)
        {
            _ = value.Dequeue();
        }

        value.Enqueue(audioTrack);

        await connection.PlayAsync(result.Tracks.FirstOrDefault());
        // TODO If PlayAsync does not return immediately and waits for playback to end (not likely), user could end up with a negative balance.
        var audioPrice = audioTrack.CalculateAndSetPrice();
        UserManager.DeductPoints(member, audioPrice);
        Log.Information("User {User} has played {AudioTrack} for {AudioPrice}", member, audioTrack, audioPrice);
        var playbackLog = new AudioPlaybackLog()
        {
            Timestamp = DateTime.UtcNow,
            AudioTrack = audioTrack,
            UserId = member.Id
        };
        AudioPlaybackLogRepository.Add(playbackLog);
    }

    public static async Task PlayAsync(Uri audioUrl, DiscordGuild guild, DiscordMember member)
    {
        if (UserManager.IsMuted(member))
        {
            Log.Warning("{Member} can not play the audio {AudioUrl}", member, audioUrl);
            throw new UnauthorizedException($"{member} can not play the audio {audioUrl}");
        }

        var connection = await GetLavalinkConnection(guild);

        var result = await connection.GetTracksAsync(audioUrl);

        if (result.LoadResultType != LavalinkLoadResultType.TrackLoaded)
        {
            Log.Warning("Lavalink failed to load the track from URL {Url} with failure type: {Type}", audioUrl,
                result.LoadResultType);
            if (result.Exception.Message == null)
            {
                throw new LavalinkLoadFailedException(result.LoadResultType.ToString());
            }

            Log.Warning("Exception: {ExceptionMessage}", result.Exception.Message);
            throw new LavalinkLoadFailedException(result.Exception.Message);
        }

        await connection.PlayAsync(result.Tracks.FirstOrDefault());
    }

    public static async Task PlayAsync(string audioName, DiscordGuild guild, DiscordMember member, CommandContext? ctx = null,
        bool searchById = false)
    {
        if (Uri.TryCreate(audioName, UriKind.Absolute, out var uriResult))
        {
            if (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)
            {
                await PlayAsync(uriResult, guild, member);
                return;
            }
        }

        var audioTrack = searchById
            ? AudioRepository.FindById(audioName)
            : (await FindAsync(audioName, 1, guild)).FirstOrDefault();
        if (audioTrack == null)
        {
            Log.Warning("No track was found with {IdOrName}: {Id}", searchById ? "ID" : "name", audioName);
            throw new AudioTrackNotFoundException(
                $"No track was found with {(searchById ? "ID" : "name")}: {audioName}");
        }

        await PlayAsync(audioTrack, guild, member, ctx);
    }

    public static async Task PlayAsync(string audioNameOrId, ulong guildId, ulong memberId,
        bool searchById = false)
    {
        var guild = Client.FindGuild(guildId);
        var member = guild.Members[memberId];
        await PlayAsync(audioNameOrId, guild, member, searchById: searchById);
    }

    #endregion

    #region Points
    /// <summary>
    /// Calculates the price decrease on an audio track based on previous price and last price update date,
    /// and updates the price information of the track.
    /// <para />
    /// Should be called if checking the current price, or there is another need to update the price property of the audio track.
    /// </summary>
    /// <returns>The price that would have been used if it were played.</returns>
    public static int CalculateAndDecreasePrice(this AudioTrack audioTrack)
    {
        var basePrice = SettingsRepository.GetValue<int>(Constants.DefaultScore);
        if (audioTrack.LastPriceUpdateAt != null)
        {
            var roundedMinutesSinceLastPriceUpdate = (int)(DateTime.UtcNow - audioTrack.LastPriceUpdateAt).Value.TotalMinutes;
            if (roundedMinutesSinceLastPriceUpdate > 0)
            {
                var priceDecreasePerMinute = SettingsRepository.GetValue<int>(Constants.PriceDecreasePerMinute);
                var priceDecrease = roundedMinutesSinceLastPriceUpdate * priceDecreasePerMinute;

                if (audioTrack.Price - priceDecrease < 0)
                {
                    audioTrack.Price = 0;
                }
                else
                {
                    audioTrack.Price -= priceDecrease;
                }

                audioTrack.LastPriceUpdateAt = DateTime.UtcNow;
            }
        }
        var effectivePrice = audioTrack.Price + basePrice;
        AudioRepository.Update(audioTrack);
        return effectivePrice;
    }

    private static void CalculateAndIncreasePrice(this AudioTrack audioTrack)
    {
        var priceIncreasePerUse = SettingsRepository.GetValue<int>(Constants.PriceIncreasePerUse);
        var priceMaximum = SettingsRepository.GetValue<int>(Constants.PriceMaximum);
        if (audioTrack.Price + priceIncreasePerUse > priceMaximum)
        {
            audioTrack.Price = priceMaximum;
        }
        else
        {
            audioTrack.Price += priceIncreasePerUse;   
        }
        audioTrack.LastPriceUpdateAt = DateTime.UtcNow;
        AudioRepository.Update(audioTrack);
    }

    /// <summary>
    /// Decreases the price of the audio track according to last update date, if needed, and returns the up-to-date usage cost for the track.
    /// Also increases the price of the track for future uses.
    /// </summary>
    /// <returns></returns>
    public static int CalculateAndSetPrice(this AudioTrack audioTrack)
    {
        var effectivePrice = audioTrack.CalculateAndDecreasePrice();
        audioTrack.CalculateAndIncreasePrice();
        return effectivePrice;
    }

    #endregion
}