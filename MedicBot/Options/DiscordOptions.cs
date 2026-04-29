namespace MedicBot.Options;

public class DiscordOptions
{
    public const string SectionName = "Discord";

    public string Token { get; set; } = null!;
    public string OAuthClientId { get; set; } = null!;
    public string OAuthClientSecret { get; set; } = null!;
}
