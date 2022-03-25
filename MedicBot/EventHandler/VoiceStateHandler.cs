using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MedicBot.Manager;
using MedicBot.Utils;
using Serilog;

namespace MedicBot.EventHandler;

public static class VoiceStateHandler
{
    private static Dictionary<ulong, Dictionary<ulong, UserVoiceStateInfo>> voiceStateTrackers = new();

    public static Task DiscordOnVoiceStateUpdated(DiscordClient sender, VoiceStateUpdateEventArgs e)
    {
        // TODO Check if a semaphore is needed
        // TODO Get rid of magic numbers, make non-bot user count needed to earn scores configurable.
        // TODO If more functionality is to be added here, moving each separate function to different methods would increase readability.
        if (!e.User.IsBot)
        {
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
        if (nonBotUsersCount < Constants.MinNumberOfUsersNeededToEarnPoints)
        {
            Log.Debug("{User} is alone in the channel right now", eventUser);
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
            
            if (!voiceStateTrackers.ContainsKey(channel.Id))
            {
                voiceStateTrackers.Add(channel.Id, new Dictionary<ulong, UserVoiceStateInfo>());
            }

            if (voiceStateTrackers[channel.Id].ContainsKey(member.Id)) continue;
            
            voiceStateTrackers[channel.Id].Add(member.Id, new UserVoiceStateInfo(member) {StartTime = DateTime.UtcNow});
            Log.Information("Now tracking user {User}", member);
        }
    }
    
    private static void TrackerUserDisconnect(DiscordUser eventUser, int nonBotUsersCount, ulong channelId)
    {
        Log.Debug("Disconnect event of {User}", eventUser);
        if (nonBotUsersCount < Constants.MinNumberOfUsersNeededToEarnPoints)
        {
            TrackerRemoveChannel(channelId);
        }
        else
        {
            Log.Debug("Removing user {User} from the tracker list", eventUser);
            var voiceStateInfo = voiceStateTrackers[channelId][eventUser.Id];
            if (voiceStateInfo == null)
                throw new InvalidOperationException(
                    $"User {eventUser} left a voice channel that had more than {Constants.MinNumberOfUsersNeededToEarnPoints} users connected, but was not found in the trackers list.");

            voiceStateInfo.FinishTime = DateTime.UtcNow;
            voiceStateTrackers[channelId].Remove(eventUser.Id);
        }
    }

    private static void TrackerRemoveChannel(ulong channelId)
    {
        Log.Debug("Removing everyone in channel with ID: {ChannelId} from the tracker list", channelId);
        if (!voiceStateTrackers.ContainsKey(channelId))
        {
            Log.Debug("Channel with ID: {ChannelId} was not being tracked", channelId);
            return;
        }
        foreach (var (_, voiceStateInfo) in voiceStateTrackers[channelId])
        {
            voiceStateInfo.FinishTime = DateTime.UtcNow;
            UserManager.AddScore(voiceStateInfo.User, voiceStateInfo.GetTimeSpentInVoice());
        }

        voiceStateTrackers[channelId].Clear();
    }

    public static void StartTracking(DiscordClient sender)
    {
        var allPopulatedVoiceChannels = sender.Guilds.Values.SelectMany(guild =>
            guild.Channels.Values.Where(channel => channel.Type == ChannelType.Voice && channel.Users.Count(member => !member.IsBot) >= Constants.MinNumberOfUsersNeededToEarnPoints));
        foreach (var channel in allPopulatedVoiceChannels)
        {
            TrackAllInChannel(channel);
        }
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