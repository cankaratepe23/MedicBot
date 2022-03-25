using System.ComponentModel;
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
    private static bool IsTracking = false;
    public static DiscordClient Client { get; set; } = null!;
    private static int _minNumberOfUsersNeededToEarnPoints;

    private static int MinNumberOfUsersNeededToEarnPoints { get; set; }

    static VoiceStateHandler()
    {
        MinNumberOfUsersNeededToEarnPoints = SettingsRepository.GetValue<int>(Constants.MinNumberOfUsersNeededToEarnPoints);
    }

    public static void Init(DiscordClient client)
    {
        Client = client;
        IsTracking = true;
    }

    public static Task DiscordOnVoiceStateUpdated(DiscordClient sender, VoiceStateUpdateEventArgs e)
    {
        // TODO Check if a semaphore is needed
        // TODO Get rid of magic numbers, make non-bot user count needed to earn scores configurable.
        // TODO If more functionality is to be added into this event handler, moving each separate function to different methods would increase readability.
        if (!e.User.IsBot)
        {
            MinNumberOfUsersNeededToEarnPoints =
                SettingsRepository.GetValue<int>(Constants.MinNumberOfUsersNeededToEarnPoints);
            var channel = e.Channel ?? e.Before.Channel;
            var nonBotUsersCount = channel.Users.Count(member => !member.IsBot);
            if (e.IsJoinEvent())
            {
                TrackerUserJoin(e.User, nonBotUsersCount, channel);
            }
            else if (e.IsDisconnectEvent())
            {
                TrackerUserDisconnect(e.User, nonBotUsersCount, channel.Id);
            }
            else
            {
                Log.Warning("Voice state updated event handler has encountered an unexpected condition");
                Log.Warning("Details of the exceptional voice state update event:");
                Log.Warning("{@VoiceStateUpdateEventArgs}", e);
                Log.Warning("{@DiscordClient}", sender);
            }
        }

        return Task.CompletedTask;
    }

    private static void TrackerUserJoin(DiscordUser eventUser, int nonBotUsersCount, DiscordChannel channel)
    {
        Log.Debug("Join event of {User}", eventUser);
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

            if (VoiceStateTrackers[channel.Id].ContainsKey(member.Id)) continue;
            
            VoiceStateTrackers[channel.Id].Add(member.Id, new UserVoiceStateInfo(member) {StartTime = DateTime.UtcNow});
            Log.Information("Now tracking user {User}", member);
        }
    }
    
    private static void TrackerUserDisconnect(DiscordUser eventUser, int nonBotUsersCount, ulong channelId)
    {
        Log.Debug("Disconnect event of {User}", eventUser);
        if (nonBotUsersCount < MinNumberOfUsersNeededToEarnPoints)
        {
            TrackerRemoveChannel(channelId);
        }
        else
        {
            Log.Debug("Removing user {User} from the tracker list", eventUser);
            var voiceStateInfo = VoiceStateTrackers[channelId][eventUser.Id];
            if (voiceStateInfo == null)
                throw new InvalidOperationException(
                    $"User {eventUser} left a voice channel that had more than {Constants.MinNumberOfUsersNeededToEarnPoints} users connected, but was not found in the trackers list.");

            voiceStateInfo.FinishTime = DateTime.UtcNow;
            VoiceStateTrackers[channelId].Remove(eventUser.Id);
        }
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
            UserManager.AddScore(voiceStateInfo.User, voiceStateInfo.GetTimeSpentInVoice());
        }

        VoiceStateTrackers[channelId].Clear();
    }
    
    public static void StartTracking()
    {
        var allPopulatedVoiceChannels = Client.Guilds.Values.SelectMany(guild =>
            guild.Channels.Values.Where(channel => channel.Type == ChannelType.Voice && channel.Users.Count(member => !member.IsBot) >= MinNumberOfUsersNeededToEarnPoints));
        foreach (var channel in allPopulatedVoiceChannels)
        {
            TrackAllInChannel(channel);
        }
    }
    
    private static void ReloadTracking()
    {
        if (!IsTracking)
        {
            return;
        }
        foreach (var (channelId,_) in VoiceStateTrackers)
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