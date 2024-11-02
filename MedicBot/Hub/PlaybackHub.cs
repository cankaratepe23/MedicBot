using System;
using Microsoft.AspNetCore.SignalR;

namespace MedicBot.Hub;

public class PlaybackHub : Hub<IPlaybackClient>
{
    // TODO Might be removed later
    public async Task SendRecentPlay(string trackId)
    {
        await Clients.All.ReceiveRecentPlay(trackId);
    }
}
