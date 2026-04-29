using System.Collections.Concurrent;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MedicBot.Repository;
using MedicBot.Utils;
using Serilog;

namespace MedicBot.EventHandler;

public class VoiceStateHandler : IVoiceStateHandler
{
    private readonly ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, UserVoiceStateInfo>> _voiceStateTrackers = new();
    private readonly DiscordClient _client;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUserPointsRepository _userPointsRepository;
    private bool _isTracking;
    private int _minNumberOfUsersNeededToEarnPoints;

    public VoiceStateHandler(DiscordClient client, ISettingsRepository settingsRepository, IUserPointsRepository userPointsRepository)
    {
        _client = client;
        _settingsRepository = settingsRepository;
        _userPointsRepository = userPointsRepository;
        UpdateThreshold();
        _isTracking = true;
    }

    private void UpdateThreshold()
    {
        _minNumberOfUsersNeededToEarnPoints =
            _settingsRepository.GetValue<int>(Constants.MinNumberOfUsersNeededToEarnPoints);
    }

    public Task DiscordOnVoiceStateUpdated(DiscordClient sender, VoiceStateUpdateEventArgs e)
    {
        if (!e.User.IsBot)
        {
            UpdateThreshold();
            var channel = e.Channel ?? e.Before.Channel;
            if (e.IsJoinEvent())
            {
                TrackerUserJoin(e.User, channel.CountNonBotUsers(), channel);
            }
            else if (e.IsDisconnectEvent())
            {
                TrackerUserDisconnect(e.User, channel.CountNonBotUsers(), channel);
            }
            else if (e.Before.Channel != e.After.Channel)
            {
                TrackerUserChangeChannel(e);
            }
        }

        return Task.CompletedTask;
    }

    private void TrackerUserJoin(DiscordUser eventUser, int nonBotUsersCount, DiscordChannel channel)
    {
        Log.Debug("Join event of {User} to {Channel}", eventUser, channel);
        if (nonBotUsersCount < _minNumberOfUsersNeededToEarnPoints)
        {
            Log.Debug(
                "{Channel} does not have enough users to earn points (Current minimum is: {Minimum}, channel has {Current} users",
                channel, _minNumberOfUsersNeededToEarnPoints, nonBotUsersCount);
        }
        else
        {
            TrackAllInChannel(channel);
        }
    }

    private void TrackAllInChannel(DiscordChannel channel)
    {
        foreach (var member in channel.GetNonBotUsers())
        {
            var usersDict = _voiceStateTrackers.GetOrAdd(channel.Id, _ => new ConcurrentDictionary<ulong, UserVoiceStateInfo>());

            if (usersDict.ContainsKey(member.Id))
            {
                continue;
            }

            usersDict.TryAdd(member.Id, new UserVoiceStateInfo(member) {StartTime = DateTime.UtcNow});
            Log.Information("Now tracking user {User}", member);
        }
    }

    private void TrackerUserDisconnect(DiscordUser eventUser, int nonBotUsersCount, DiscordChannel channel)
    {
        Log.Debug("Disconnect event of {User} from {Channel}", eventUser, channel);
        if (nonBotUsersCount < _minNumberOfUsersNeededToEarnPoints)
        {
            TrackerRemoveChannel(channel.Id);
        }
        else
        {
            TrackerRemoveUser(eventUser, channel.Id);
        }
    }

    public async Task TrackerUserAddPointsAsync(ulong userId)
    {
        var user = await _client.GetUserAsync(userId);
        TrackerUserAddPoints(user);
    }

    public void TrackerUserAddPoints(DiscordUser user)
    {
        UserVoiceStateInfo? voiceStateInfo = null;
        ConcurrentDictionary<ulong, UserVoiceStateInfo>? usersDict = null;
        foreach (var channelIdUsersDictPair in _voiceStateTrackers)
        {
            var currentUsersDict = channelIdUsersDictPair.Value;
            foreach (var userIdStateInfoPair in currentUsersDict)
            {
                var currentUserId = userIdStateInfoPair.Key;
                var currentUserState = userIdStateInfoPair.Value;
                if (currentUserId == user.Id)
                {
                    voiceStateInfo = currentUserState;
                    usersDict = currentUsersDict;
                    break;
                }
            }
        }
        if (voiceStateInfo == null || usersDict == null)
        {
            Log.Debug("{User} is not being tracked, nothing to add", user);
            return;
        }

        Log.Debug("Found tracked user info {UserVoiceStateInfo}", voiceStateInfo);
        voiceStateInfo.FinishTime = DateTime.UtcNow;
        var pointsToAdd = voiceStateInfo.GetTimeSpentInVoice();
        _userPointsRepository.AddPoints(user.Id, (int)Math.Floor(pointsToAdd.TotalSeconds));
        Log.Information("Added {Points} points to {User}", pointsToAdd, voiceStateInfo.User);

        usersDict[user.Id] = new UserVoiceStateInfo(user) { StartTime = DateTime.UtcNow };
    }

    private void TrackerRemoveUser(DiscordUser eventUser, ulong channelId)
    {
        Log.Debug("Removing user {User} from the tracker list", eventUser);
        if (!_voiceStateTrackers.TryGetValue(channelId, out var usersDict) ||
            !usersDict.TryRemove(eventUser.Id, out var voiceStateInfo))
        {
            return;
        }

        voiceStateInfo.FinishTime = DateTime.UtcNow;
        var pointsToAdd = voiceStateInfo.GetTimeSpentInVoice();
        _userPointsRepository.AddPoints(eventUser.Id, (int)Math.Floor(pointsToAdd.TotalSeconds));
        Log.Information("Added {Points} points to {User}", pointsToAdd, voiceStateInfo.User);
    }

    private void TrackerRemoveChannel(ulong channelId)
    {
        Log.Debug("Removing everyone in channel with ID: {ChannelId} from the tracker list", channelId);
        if (!_voiceStateTrackers.TryGetValue(channelId, out var usersDict))
        {
            Log.Debug("Channel with ID: {ChannelId} was not being tracked", channelId);
            return;
        }

        foreach (var (_, voiceStateInfo) in usersDict)
        {
            voiceStateInfo.FinishTime = DateTime.UtcNow;
            _userPointsRepository.AddPoints(voiceStateInfo.User.Id, (int)Math.Floor(voiceStateInfo.GetTimeSpentInVoice().TotalSeconds));
        }

        usersDict.Clear();
    }

    private void TrackerUserChangeChannel(VoiceStateUpdateEventArgs e)
    {
        TrackerUserDisconnect(e.User, e.Before.Channel.CountNonBotUsers(), e.Before.Channel);
        TrackerUserJoin(e.User, e.After.Channel.CountNonBotUsers(), e.After.Channel);
    }

    public void StartTracking()
    {
        UpdateThreshold();
        Log.Information("Initialize tracking");
        var allPopulatedVoiceChannels = _client.Guilds.Values.SelectMany(guild =>
            guild.Channels.Values.Where(channel =>
                channel.Type == ChannelType.Voice &&
                channel.Users.Count(member => !member.IsBot) >= _minNumberOfUsersNeededToEarnPoints));
        var channelCounter = 0;
        foreach (var channel in allPopulatedVoiceChannels)
        {
            TrackAllInChannel(channel);
            channelCounter++;
        }

        Log.Information("Started tracking {Count} populated channel(s)", channelCounter);
    }

    public void ReloadTracking()
    {
        UpdateThreshold();
        if (!_isTracking)
        {
            return;
        }

        foreach (var (channelId, _) in _voiceStateTrackers)
        {
            TrackerRemoveChannel(channelId);
        }

        StartTracking();
    }
}

internal class UserVoiceStateInfo
{
    public UserVoiceStateInfo(DiscordUser user)
    {
        User = user;
    }

    public DiscordUser User { get; }
    public DateTime StartTime { get; init; }
    public DateTime FinishTime { get; set; }

    public TimeSpan GetTimeSpentInVoice()
    {
        return FinishTime - StartTime;
    }
}