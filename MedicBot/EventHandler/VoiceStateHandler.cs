using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MedicBot.Manager;
using MedicBot.Repository;
using MedicBot.Utils;
using Serilog;

namespace MedicBot.EventHandler;

public static class VoiceStateHandler
{
    private static readonly Dictionary<ulong, Dictionary<ulong, UserVoiceStateInfo>> VoiceStateTrackers = new();
    private static bool _isTracking;

    static VoiceStateHandler()
    {
        UpdateThreshold();
        // TODO Check if this is necessary
    }

    private static DiscordClient Client { get; set; } = null!;
    private static int MinNumberOfUsersNeededToEarnPoints { get; set; }

    private static void UpdateThreshold()
    {
        MinNumberOfUsersNeededToEarnPoints =
            SettingsRepository.GetValue<int>(Constants.MinNumberOfUsersNeededToEarnPoints);
    }

    public static void Init(DiscordClient client)
    {
        Client = client;
        _isTracking = true;
    }

    public static Task DiscordOnVoiceStateUpdated(DiscordClient sender, VoiceStateUpdateEventArgs e)
    {
        // TODO Check if a semaphore is needed
        // TODO If more functionality is to be added into this event handler, moving each separate function to different methods would increase readability.
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

            // TODO else case: A user may change voice channels. This is neither a connect nor disconnect case,
            // but they but should be removed from the tracker and re-added to the new channel's tracker.
        }

        return Task.CompletedTask;
    }

    private static void TrackerUserJoin(DiscordUser eventUser, int nonBotUsersCount, DiscordChannel channel)
    {
        Log.Debug("Join event of {User} to {Channel}", eventUser, channel);
        if (nonBotUsersCount < MinNumberOfUsersNeededToEarnPoints)
        {
            Log.Debug(
                "{Channel} does not have enough users to earn points (Current minimum is: {Minimum}, channel has {Current} users",
                channel, MinNumberOfUsersNeededToEarnPoints, nonBotUsersCount);
        }
        else
        {
            TrackAllInChannel(channel);
        }
    }

    private static void TrackAllInChannel(DiscordChannel channel)
    {
        foreach (var member in channel.GetNonBotUsers())
        {
            if (!VoiceStateTrackers.ContainsKey(channel.Id))
            {
                VoiceStateTrackers.Add(channel.Id, new Dictionary<ulong, UserVoiceStateInfo>());
            }

            if (VoiceStateTrackers[channel.Id].ContainsKey(member.Id))
            {
                continue;
            }

            VoiceStateTrackers[channel.Id].Add(member.Id, new UserVoiceStateInfo(member) {StartTime = DateTime.UtcNow});
            Log.Information("Now tracking user {User}", member);
        }
    }

    private static void TrackerUserDisconnect(DiscordUser eventUser, int nonBotUsersCount, DiscordChannel channel)
    {
        Log.Debug("Disconnect event of {User} from {Channel}", eventUser, channel);
        if (nonBotUsersCount < MinNumberOfUsersNeededToEarnPoints)
        {
            TrackerRemoveChannel(channel.Id);
        }
        else
        {
            TrackerRemoveUser(eventUser, channel.Id);
        }
    }

    private static void TrackerRemoveUser(DiscordUser eventUser, ulong channelId)
    {
        Log.Debug("Removing user {User} from the tracker list", eventUser);
        var voiceStateInfo = VoiceStateTrackers[channelId][eventUser.Id];
        if (voiceStateInfo == null)
        {
            throw new InvalidOperationException(
                $"User {eventUser} left a voice channel that had more than {Constants.MinNumberOfUsersNeededToEarnPoints} users connected, but was not found in the trackers list.");
        }

        voiceStateInfo.FinishTime = DateTime.UtcNow;
        UserManager.AddPoints(voiceStateInfo.User, voiceStateInfo.GetTimeSpentInVoice());
        VoiceStateTrackers[channelId].Remove(eventUser.Id);
    }

    private static void TrackerRemoveChannel(ulong channelId)
    {
        Log.Debug("Removing everyone in channel with ID: {ChannelId} from the tracker list", channelId);
        if (!VoiceStateTrackers.ContainsKey(channelId))
        {
            Log.Debug("Channel with ID: {ChannelId} was not being tracked", channelId);
            return;
        }

        foreach (var (_, voiceStateInfo) in VoiceStateTrackers[channelId])
        {
            voiceStateInfo.FinishTime = DateTime.UtcNow;
            UserManager.AddPoints(voiceStateInfo.User, voiceStateInfo.GetTimeSpentInVoice());
        }

        VoiceStateTrackers[channelId].Clear();
    }

    private static void TrackerUserChangeChannel(VoiceStateUpdateEventArgs e)
    {
        TrackerUserDisconnect(e.User, e.Before.Channel.CountNonBotUsers(), e.Before.Channel);
        TrackerUserJoin(e.User, e.After.Channel.CountNonBotUsers(), e.After.Channel);
    }

    public static void StartTracking()
    {
        UpdateThreshold();
        Log.Information("Initialize tracking");
        var allPopulatedVoiceChannels = Client.Guilds.Values.SelectMany(guild =>
            guild.Channels.Values.Where(channel =>
                channel.Type == ChannelType.Voice &&
                channel.Users.Count(member => !member.IsBot) >= MinNumberOfUsersNeededToEarnPoints));
        var channelCounter = 0;
        foreach (var channel in allPopulatedVoiceChannels)
        {
            TrackAllInChannel(channel);
            channelCounter++;
        }

        Log.Information("Started tracking {Count} populated channel(s)", channelCounter);
    }

    public static void ReloadTracking()
    {
        UpdateThreshold();
        if (!_isTracking)
        {
            return;
        }

        foreach (var (channelId, _) in VoiceStateTrackers)
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