namespace MedicBot.Model;

public class AuthTokensResponse
{
    public string? AccessToken { get; set; }
    public int AccessTokenExpiresIn { get; set; }
    public string? RefreshToken { get; set; }
    public int RefreshTokenExpiresIn { get; set; }
}
