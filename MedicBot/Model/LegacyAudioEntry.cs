#pragma warning disable CS8618
namespace MedicBot.Model;

public class LegacyAudioEntry
{
    public const string CollectionName = "legacyAudioTracks";
    
    public enum AudioType
    {
        File,
        Url,
        Youtube,
        Attachment
    }

    public string Name { get; set; }
    public string Extension { get; set; }
    public string FileName { get; set; }
    public AudioType Type { get; set; }
    public string Path { get; set; }
    public DateTime CreationDate { get; set; }
    public ulong OwnerId { get; set; }
    public string DownloadedFrom { get; set; }
    public string SecondaryPath { get; set; }
    public List<string> Aliases { get; set; }
    public List<string> Collections { get; set; }
}