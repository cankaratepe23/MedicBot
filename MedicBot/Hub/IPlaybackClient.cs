using System;

namespace MedicBot.Hub;

public interface IPlaybackClient
{
    Task ReceiveRecentPlay(string trackId);
}
