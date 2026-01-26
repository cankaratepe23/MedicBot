namespace MedicBot.Model;

public class DiscordCodeExchangeRequest
{
    public string? Code { get; set; }
    public string? CodeVerifier { get; set; }
    public string? RedirectUri { get; set; }
    public string? ClientId { get; set; }
}
