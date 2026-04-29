namespace MedicBot.Options;

public class AuthOptions
{
    public const string SectionName = "Auth";

    public string JwtSecret { get; set; } = null!;
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);
}
