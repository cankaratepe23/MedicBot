using MedicBot.Utils;

namespace MedicBot.EventHandler;

public class BotSettingHandler
{
    private readonly IVoiceStateHandler _voiceStateHandler;

    public BotSettingHandler(IVoiceStateHandler voiceStateHandler)
    {
        _voiceStateHandler = voiceStateHandler;
    }

    public void BotSettingChangedHandler(string key)
    {
        if (key == Constants.MinNumberOfUsersNeededToEarnPoints)
        {
            _voiceStateHandler.ReloadTracking();
        }
    }
}