using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MedicBot.Manager;
using MedicBot.Utils;
using Serilog;

namespace MedicBot.EventHandler;

public static class VoiceStateHandler
{
    private static Dictionary<DiscordChannel, List<UserVoiceStateInfo>> voiceStateTrackers = new();

    public static Task DiscordOnVoiceStateUpdated(DiscordClient sender, VoiceStateUpdateEventArgs e)
    {
        // TODO Add logging
        // TODO Check if a semaphore is needed
        // TODO Get rid of magic numbers, make non-bot user count needed to earn scores configurable.
        // TODO If more functionality is to be added here, moving each separate function to different methods would increase readability.
        if (!e.User.IsBot)
        {
            var nonBotUsersCount = e.Channel.Users.Count(member => !member.IsBot);
            if (e.IsJoinEvent())
            {
                if (nonBotUsersCount < 2)
                {
                    // Don't do anything
                }
                else if (nonBotUsersCount == 2)
                {
                    // Add everyone, as the number of people has crossed the threshold
                    foreach (var member in e.Channel.GetNonBotUsers())
                        voiceStateTrackers[e.Channel].Add(new UserVoiceStateInfo(member) {StartTime = DateTime.UtcNow});
                }
                else
                {
                    // Add the newcomer
                    voiceStateTrackers[e.Channel].Add(new UserVoiceStateInfo(e.User) {StartTime = DateTime.UtcNow});
                }
            }
            else if (e.IsDisconnectEvent())
            {
                if (nonBotUsersCount < 2)
                {
                    // Party is over, remove everyone and save their scores
                    foreach (var voiceStateInfo in voiceStateTrackers[e.Channel])
                    {
                        voiceStateInfo.FinishTime = DateTime.Now;
                        UserManager.AddScore(e.User, voiceStateInfo.GetTimeSpentInVoice());
                    }

                    voiceStateTrackers[e.Channel].Clear();
                }
                else
                {
                    var voiceStateInfo = voiceStateTrackers[e.Channel].Find(i => i.User == e.User);
                    if (voiceStateInfo == null)
                        throw new InvalidOperationException(
                            $"User {e.User.Username} left voice chat, but was not found in the trackers list.");

                    voiceStateInfo.FinishTime = DateTime.Now;
                    UserManager.AddScore(e.User, voiceStateInfo.GetTimeSpentInVoice());
                }
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