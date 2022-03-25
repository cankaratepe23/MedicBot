using DSharpPlus.Entities;
using Serilog;

namespace MedicBot.Manager;

public static class UserManager
{
    public static void AddScore(DiscordUser member, double score)
    {
        Log.Debug("Added {Points} points to {Member}", score, member);
    }

    public static void AddScore(DiscordUser member, TimeSpan time)
    {
        AddScore(member, time.TotalSeconds);
    }
}