using MedicBot.Utils;

namespace MedicBot.EventHandler;

public static class BotSettingHandler
{
    public static void BotSettingChangedHandler(string key)
    {
        if (key == Constants.MinNumberOfUsersNeededToEarnPoints)
        {
            VoiceStateHandler.ReloadTracking();
        }
    }
}