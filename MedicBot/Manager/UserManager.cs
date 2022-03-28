using DSharpPlus.Entities;
using MedicBot.Repository;
using Serilog;

namespace MedicBot.Manager;

public static class UserManager
{
    public static void AddPoints(DiscordUser member, int score)
    {
        UserPointsRepository.AddPoints(member.Id, score);
        Log.Debug("Added {Points} points to {Member}", score, member);
    }

    public static void AddPoints(DiscordUser member, TimeSpan time)
    {
        AddPoints(member, (int) Math.Floor(time.TotalSeconds));
    }
}