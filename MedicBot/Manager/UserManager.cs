using DSharpPlus.Entities;
using MedicBot.Model;
using MedicBot.Repository;
using Serilog;

namespace MedicBot.Manager;

public static class UserManager
{
    public static void AddPoints(DiscordUser member, int points)
    {
        UserPointsRepository.AddPoints(member.Id, points);
        Log.Debug("Added {Points} points to {Member}", points, member);
    }

    public static void AddPoints(DiscordUser member, TimeSpan time)
    {
        AddPoints(member, (int) Math.Floor(time.TotalSeconds));
    }

    public static void DeductPoints(DiscordUser member, int points)
    {
        UserPointsRepository.AddPoints(member.Id, (-1) * points);
        Log.Debug("Removed {Points} points from {Member}", points, member);
    }

    public static void DeductPoints(DiscordUser member, TimeSpan time)
    {
        DeductPoints(member, (int) Math.Floor(time.TotalSeconds));
    }

    public static bool IsMuted(DiscordUser member)
    {
        var userMute = UserMuteRepository.Get(member.Id);
        if (userMute == null)
        {
            return false;
        }

        var userMuteEndDateTime = userMute.EndDateTime;
        if (DateTime.UtcNow < userMuteEndDateTime)
        {
            return true;
        }

        UserMuteRepository.Delete(userMute.Id);
        return false;
    }

    public static bool CanPlayAudio(DiscordMember member, AudioTrack audioTrack)
    {
        return UserPointsRepository.GetPoints(member.Id) >= audioTrack.Price && !IsMuted(member);
    }
}