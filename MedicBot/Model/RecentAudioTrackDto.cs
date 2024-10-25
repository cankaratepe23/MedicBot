using System;

namespace MedicBot.Model;

public class RecentAudioTrackDto
{
    public AudioTrackDto? AudioTrackDto { get; set; }
    public int Count { get; set; }
}
