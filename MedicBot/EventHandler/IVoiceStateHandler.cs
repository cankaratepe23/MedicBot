using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace MedicBot.EventHandler;

public interface IVoiceStateHandler
{
    Task DiscordOnVoiceStateUpdated(DiscordClient sender, VoiceStateUpdateEventArgs e);
    void StartTracking();
    void ReloadTracking();
    void TrackerUserAddPoints(DiscordUser user);
    Task TrackerUserAddPointsAsync(ulong userId);
}
