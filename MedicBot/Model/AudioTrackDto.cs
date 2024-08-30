namespace MedicBot.Model;

public class AudioTrackDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public List<string>? Aliases { get; set; }
    public List<string>? Tags { get; set; }
    public bool? IsFavorite { get; set; }
}