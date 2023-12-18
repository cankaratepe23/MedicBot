using System.Text;
using DSharpPlus.Entities;
using MedicBot.Model;
using MedicBot.Repository;
using MedicBot.Utils;
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

    public static void Mute(DiscordMember member, int minutes)
    {
        var userMute = UserMuteRepository.Get(member.Id);
        if (userMute == null)
        {
            UserMuteRepository.Set(member.Id, DateTime.UtcNow.AddMinutes(minutes));
        }
        else
        {
            UserMuteRepository.Set(member.Id, userMute.EndDateTime.AddMinutes(minutes));
        }
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

    public static bool CanPlayAudio(DiscordMember member, AudioTrack audioTrack, out string reason)
    {
        var userPoints = UserPointsRepository.GetPoints(member.Id);
        var trackPrice = audioTrack.Price;

        var userHasEnoughPoints = userPoints >= trackPrice;
        var userIsNotMuted = !IsMuted(member);

        var reasonBuilder = new StringBuilder();
        if (!userHasEnoughPoints)
        {
            reasonBuilder.Append($"You don't have enough points to play this audio. (You have: {userPoints}, you need: {trackPrice})");
        }
        if (!userHasEnoughPoints && !userIsNotMuted)
        {
            reasonBuilder.Append(" AND ");
        }
        if (!userIsNotMuted)
        {
            var muteEndDateTime = UserMuteRepository.GetEndDateTime(member.Id);
            var muteRemaining = muteEndDateTime - DateTime.UtcNow;
            reasonBuilder.Append($"You are currently muted for the next {muteRemaining.ToPrettyString()}");
        }

        reason = reasonBuilder.ToString();
        return userHasEnoughPoints && userIsNotMuted;
    }
}