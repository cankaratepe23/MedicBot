using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Lavalink4NET;
using Lavalink4NET.Players;
using Lavalink4NET.Rest;
using Lavalink4NET.Rest.Entities.Tracks;
using MedicBot.Exceptions;
using MedicBot.Hub;
using MedicBot.Model;
using MedicBot.Repository;
using MedicBot.Utils;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Serilog;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Readers;

namespace MedicBot.Manager;

public class AudioManager : IAudioManager
{
    private readonly DiscordClient _client;
    private readonly IHubContext<PlaybackHub, IPlaybackClient> _hubContext;
    private readonly IAudioService _audioService;
    private readonly IAudioRepository _audioRepository;
    private readonly IAudioPlaybackLogRepository _audioPlaybackLogRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUserManager _userManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _audioTracksPath;
    private readonly string _tempFilesPath;

    private readonly Dictionary<ulong, Queue<AudioTrack>> _lastPlayedTracks = new();

    public AudioManager(
        DiscordClient client,
        IAudioService audioService,
        IHubContext<PlaybackHub, IPlaybackClient> hubContext,
        IAudioRepository audioRepository,
        IAudioPlaybackLogRepository audioPlaybackLogRepository,
        ISettingsRepository settingsRepository,
        IUserManager userManager,
        IHttpClientFactory httpClientFactory)
    {
        _client = client;
        _audioService = audioService;
        _hubContext = hubContext;
        _audioRepository = audioRepository;
        _audioPlaybackLogRepository = audioPlaybackLogRepository;
        _settingsRepository = settingsRepository;
        _userManager = userManager;
        _httpClientFactory = httpClientFactory;
        _audioTracksPath = Constants.AudioTracksPath;
        _tempFilesPath = Constants.TempFilesPath;
    }

    public async Task AddAsync(string audioName, ulong userId, string url)
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

        if (_audioRepository.NameExists(audioName))
        {
            Log.Warning("An AudioTrack with the name {AudioName} already exists", audioName);
            throw new AudioTrackExistsException($"An AudioTrack with the name {audioName} already exists.");
        }

        Log.Information("Detected file extension: {FileExtension}", fileExtension);
        var fileName = audioName + fileExtension;
        var filePath = fileExtension.ToLowerInvariant() == ".7z"
            ? string.Join('/', _tempFilesPath, fileName)
            : string.Join('/', _audioTracksPath, fileName);

        {
            var httpClient = _httpClientFactory.CreateClient();
            Log.Information("Downloading: {DownloadUrl}", url);
            Log.Information("Downloading file to {FilePath}", filePath);
            await using var stream = await httpClient.GetStreamAsync(url);
            await using var fileStream = File.OpenWrite(filePath);
            await stream.CopyToAsync(fileStream);
            await fileStream.FlushAsync();
        }

        if (fileExtension.ToLowerInvariant() == ".7z")
        {
            Log.Information("Extracting 7z...");
            var filesDirectory = string.Join('/', _tempFilesPath, audioName);
            Directory.CreateDirectory(filesDirectory);

            using (var archive = SevenZipArchive.OpenArchive(filePath))
            using (var reader = archive.ExtractAllEntries())
            {
                reader.WriteAllToDirectory(filesDirectory);
            }

            foreach (var file in Directory.GetFiles(filesDirectory))
            {
                var newAudioFileName = Path.GetFileName(file);
                var newAudioName = Path.GetFileNameWithoutExtension(newAudioFileName);
                Log.Information("Adding {AudioTrackName}", newAudioName);
                var newFilePath = string.Join('/', _audioTracksPath, newAudioFileName);
                File.Move(file, newFilePath);
                if (_audioRepository.NameExists(audioName))
                {
                    Log.Warning("An AudioTrack with the name {AudioName} already exists", newAudioName);
                    Log.Warning("Skipping adding track with name {AudioName}", newAudioName);
                    continue;
                }

                _audioRepository.Add(new AudioTrack(newAudioName, newFilePath, userId));
            }
        }
        else
        {
            _audioRepository.Add(new AudioTrack(audioName, filePath, userId));
        }
    }

    public void AddTag(AudioTrack audioTrack, string tagName)
    {
        if (audioTrack.Tags.Contains(tagName))
        {
            return;
        }

        audioTrack.Tags.Add(tagName);
        _audioRepository.Update(audioTrack);
    }

    public async Task DeleteAsync(string audioName, ulong userId)
    {
        var audioTrack = _audioRepository.FindByNameExact(audioName);
        if (audioTrack == null)
        {
            Log.Warning("No track was found with name: {Name}", audioName);
            throw new AudioTrackNotFoundException($"No track was found with name: {audioName}");
        }

        if (audioTrack.OwnerId != userId && _client.CurrentUser.Id != userId)
        {
            Log.Warning("A non-owner or non-admin user {UserId} attempted deleting the following track: {@AudioTrack}",
                userId, audioTrack);
            var user = await _client.GetUserAsync(userId);
            if (user != null)
            {
                Log.Warning("Offending user of the unauthorized delete operation: {User}", user);
            }

            throw new UnauthorizedException("You need to be the owner of this audio track to delete it.");
        }

        _audioRepository.Delete(audioTrack.Id);
        File.Delete(audioTrack.Path);
    }

    public async Task<IEnumerable<AudioTrack>> FindAsync(string searchQuery, long limit = 10, DiscordGuild? guild = null, ulong? userId = null)
    {
        var canGetNonGlobals = CanGetNonGlobals(userId, guild);

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
                var randomTrack = await _audioRepository.Random(tag, canGetNonGlobals);
                return new List<AudioTrack> { randomTrack };
            }

            return _audioRepository.All(tag).Where(t => canGetNonGlobals || t.IsGlobal );
        }

        if (searchQuery == "!!" && guild != null)
        {
            var previousTrack = GetLastPlayedTracks(guild)?.FirstOrDefault();
            return previousTrack == null ? new List<AudioTrack>() : new List<AudioTrack> { previousTrack };
        }

        if (searchQuery.StartsWith('\"') && searchQuery.EndsWith('"'))
        {
            return _audioRepository.FindAllByName(searchQuery.Trim('"'), tag).Where(t => canGetNonGlobals || t.IsGlobal );
        }

        return (await _audioRepository.FindMany(searchQuery, limit, tag)).Where(t => canGetNonGlobals || t.IsGlobal );
    }

    public AudioTrack? FindById(string id)
    {
        return _audioRepository.FindById(id);
    }

    public IEnumerable<AudioTrack> GetNewTracksAsync(long limit = 10)
    {
        return _audioRepository.GetOrderedByDate(limit);
    }

    public DateTimeOffset GetLatestUpdateTime()
    {
        var latestModifiedTrack = _audioRepository.GetOrderedByModified(1).FirstOrDefault();
        if (latestModifiedTrack == null || latestModifiedTrack.LastModifiedAt == null)
        {
            return DateTimeOffset.UtcNow;
        }

        return (DateTimeOffset) latestModifiedTrack.LastModifiedAt;
    }

    public IEnumerable<AudioTrack>? GetLastPlayedTracks(DiscordGuild guild)
    {
        return _lastPlayedTracks.TryGetValue(guild.Id, out var tracks) ? tracks.Reverse() : null;
    }

    public IEnumerable<RecentAudioTrack> GetFrequentlyUsedTracks(DiscordUser user)
    {
        return GetFrequentlyUsedTracks(user.Id);
    }

    public IEnumerable<RecentAudioTrack> GetFrequentlyUsedTracks(ulong userId)
    {
        var recents = _audioPlaybackLogRepository.GetGlobalLog()
                                                .GroupBy(l => l.AudioTrack.Id)
                                                .OrderBy(g => g.Count())
                                                .Take(50)
                                                .Select(g => new RecentAudioTrack()
                                                    {
                                                        AudioTrack = g.First().AudioTrack,
                                                        Count = g.Count()
                                                    });
        return recents;
    }

    public IEnumerable<RecentAudioTrack> GetRecentAudioTracks(DiscordUser user)
    {
        return GetRecentAudioTracks(user.Id);
    }

    public IEnumerable<RecentAudioTrack> GetRecentAudioTracks(ulong userId)
    {
        var canGetNonGlobals = CanGetNonGlobals(userId);
        var recents = _audioPlaybackLogRepository.GetGlobalLog()
                                                .Where(l => canGetNonGlobals || l.AudioTrack.IsGlobal)
                                                .OrderByDescending(l => l.Timestamp)
                                                .DistinctBy(l => l.AudioTrack.Id)
                                                .Take(50)
                                                .Select(l => new RecentAudioTrack() { AudioTrack = l.AudioTrack, Count = l.Timestamp.Ticks });
        return recents;
    }

    private async Task<LavalinkPlayer> GetLavalinkConnection(DiscordGuild guild)
    {
        var result = await _audioService.Players.RetrieveAsync(guild.Id, null, PlayerFactory.Default, Microsoft.Extensions.Options.Options.Create(new LavalinkPlayerOptions()));
        
        var connection = result.Player;

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

            connection = await JoinAsync(channelToJoin);
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

    private bool CanGetNonGlobals(ulong? userId, DiscordGuild? guild = null)
    {
        bool canUserGetNonGlobals = false;
        bool canGuildGetNonGlobals = false;
        if (userId.HasValue)
        {
            var globalTesters = _settingsRepository.GetValue<string>(Constants.GlobalTesters);

            if (!string.IsNullOrWhiteSpace(globalTesters))
            {
                var testerIds = globalTesters.Split(',').Select(s => Convert.ToUInt64(s));
                if (testerIds.Contains(userId.Value))
                {
                    return false;
                }
            }

            var botGuilds = _client.Guilds;
            var whitelistedMembers = botGuilds.Where(g => Constants.WhitelistedGuilds.Contains(g.Key))
                                        .Select(g => g.Value.Members);
            canUserGetNonGlobals = whitelistedMembers.Any(g => g.ContainsKey(userId.Value));
        }
        
        if (guild != null)
        {
            canGuildGetNonGlobals = Constants.WhitelistedGuilds.Contains(guild.Id);
        }

        return canUserGetNonGlobals || canGuildGetNonGlobals;
    }

    #region Join

    public async Task<LavalinkPlayer> JoinAsync(DiscordChannel channel)
    {
        if (channel.Type != ChannelType.Voice)
        {
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

        var result = await _audioService.Players.RetrieveAsync(channel.Guild.Id, channel.Id, PlayerFactory.Default, Microsoft.Extensions.Options.Options.Create(new LavalinkPlayerOptions()), new PlayerRetrieveOptions { ChannelBehavior = PlayerChannelBehavior.Join });
        var connection = result.Player;
        if (connection == null)
        {
            Log.Warning("JoinAsync() failed to establish a Lavalink connection to {Channel}", channel);
            throw new Exception("JoinAsync() failed to establish a Lavalink connection.");
        }
        Log.Information("Voice connected to {Channel}", channel);
        return connection;
    }

    public async Task JoinChannelIdAsync(ulong channelId)
    {
        var guild = _client.Guilds.Values.FirstOrDefault(g => g.Channels.ContainsKey(channelId));
        if (guild == null)
        {
            Log.Warning("Guild containing a channel with ID: {Id} not found", channelId);
            throw new GuildNotFoundException($"Guild containing a channel with ID: {channelId} not found.");
        }

        var channel = guild.Channels[channelId];
        await JoinAsync(channel);
    }

    public async Task JoinGuildIdAsync(ulong guildId)
    {
        await JoinGuildAsync(_client.FindGuild(guildId));
    }

    public async Task JoinGuildAsync(DiscordGuild guild)
    {
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

    public async Task LeaveAsync(DiscordGuild guild)
    {
        var connection = await GetLavalinkConnection(guild);
        if (connection == null)
        {
            Log.Warning(Constants.NotConnectedToVoiceLog);
            throw new Exception(Constants.NotConnectedToVoiceMessage);
        }
        var currentChannel = guild.Channels[connection.VoiceChannelId];
        await connection.DisconnectAsync();
        Log.Information("Voice disconnected from {Channel}", currentChannel);
    }

    public async Task LeaveAsync(ulong guildId)
    {
        await LeaveAsync(_client.FindGuild(guildId));
    }

    #endregion

    #region Play

    public async Task<int> PlayAsync(AudioTrack audioTrack, DiscordGuild guild, DiscordMember member, CommandContext? ctx = null)
    {        
        if (!_userManager.CanPlayAudio(member, audioTrack, out var reason))
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

        if (!_lastPlayedTracks.TryGetValue(guild.Id, out Queue<AudioTrack>? value))
        {
            value = new Queue<AudioTrack>(10);
            _lastPlayedTracks.Add(guild.Id, value);
        }

        if (value.Count >= 10)
        {
            _ = value.Dequeue();
        }

        value.Enqueue(audioTrack);

        await connection.PlayFileAsync(new FileInfo(audioTrack.Path));
        var audioPrice = audioTrack.CalculateAndSetPrice(_settingsRepository);
        _audioRepository.Update(audioTrack);
        _userManager.DeductPoints(member, audioPrice);
        Log.Information("User {User} has played {AudioTrack} for {AudioPrice}", member, audioTrack, audioPrice);
        var playbackLog = new AudioPlaybackLog()
        {
            Timestamp = DateTime.UtcNow,
            AudioTrack = audioTrack,
            UserId = member.Id
        };
        await _audioPlaybackLogRepository.AddAsync(playbackLog);
        await _hubContext.Clients.All.ReceiveRecentPlay(audioTrack.Id.ToString());
        return audioPrice;
    }

    public async Task PlayAsync(Uri audioUrl, DiscordGuild guild, DiscordMember member)
    {
        if (_userManager.IsMuted(member))
        {
            Log.Warning("{Member} can not play the audio {AudioUrl}", member, audioUrl);
            throw new UnauthorizedException($"{member} can not play the audio {audioUrl}");
        }

        var connection = await GetLavalinkConnection(guild);

        var result = await _audioService.Tracks.LoadTrackAsync(audioUrl.ToString(), TrackSearchMode.YouTube);

        if (result == null)
        {
            Log.Warning("Lavalink failed to load the track from URL {Url}", audioUrl);
            throw new LavalinkLoadFailedException(audioUrl.ToString());
        }

        await connection.PlayAsync(result);
    }

    public async Task<int> PlayAsync(string audioName, DiscordGuild guild, DiscordMember member, CommandContext? ctx = null,
        bool searchById = false)
    {
        if (Uri.TryCreate(audioName, UriKind.Absolute, out var uriResult))
        {
            if (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)
            {
                await PlayAsync(uriResult, guild, member);
                return -1;
            }
        }

        var audioTrack = searchById
            ? _audioRepository.FindById(audioName)
            : (await FindAsync(audioName, 1, guild)).FirstOrDefault();
        if (audioTrack == null)
        {
            Log.Warning("No track was found with {IdOrName}: {Id}", searchById ? "ID" : "name", audioName);
            throw new AudioTrackNotFoundException(
                $"No track was found with {(searchById ? "ID" : "name")}: {audioName}");
        }

        return await PlayAsync(audioTrack, guild, member, ctx);
    }

    public async Task<int> PlayAsync(string audioNameOrId, ulong guildId, ulong memberId,
        bool searchById = false)
    {
        var guild = _client.FindGuild(guildId);
        var member = guild.Members[memberId];
        return await PlayAsync(audioNameOrId, guild, member, searchById: searchById);
    }

    #endregion
}