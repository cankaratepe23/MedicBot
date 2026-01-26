using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using MedicBot;
using Serilog;
using MedicBot.Manager;
using MedicBot.Model;
using MedicBot.Repository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicBot.Controller;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [HttpGet("Login")]
    public IActionResult Login()
    {
        return Challenge(new AuthenticationProperties {RedirectUri = "https://medicbot.comaristan.com/"}, authenticationSchemes: "Discord");
    }

    [HttpGet("LocalLogin")]
    public IActionResult LocalLogin()
    {
        return Challenge(new AuthenticationProperties {RedirectUri = "https://127.0.0.1:3000/"}, authenticationSchemes: "Discord");
    }

    [HttpGet("DiscordLogin")]
    public IActionResult DiscordLogin([FromQuery] string code)
    {
        return Ok();
    }

    [HttpPost("ExchangeDiscordCode")]
    public async Task<IActionResult> ExchangeDiscordCode([FromBody] DiscordCodeExchangeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Code) ||
            string.IsNullOrWhiteSpace(request?.CodeVerifier) ||
            string.IsNullOrWhiteSpace(request?.RedirectUri) ||
            string.IsNullOrWhiteSpace(request?.ClientId))
        {
            return BadRequest("Code, code verifier, redirect URI, and client ID are required.");
        }

        // Exchange authorization code for Discord access token (PKCE flow - no client_secret needed)
        var discordAccessToken = await ExchangeCodeForDiscordTokenAsync(
            request.Code, request.CodeVerifier, request.RedirectUri, request.ClientId);

        if (discordAccessToken == null)
        {
            return Unauthorized("Discord token exchange failed.");
        }

        // Validate the Discord token and get user info
        var discordUser = await GetDiscordUserAsync(discordAccessToken);
        if (discordUser?.Id == null || !ulong.TryParse(discordUser.Id, out var userId))
        {
            return Unauthorized("Failed to validate Discord user.");
        }

        // Issue MedicBot tokens
        var now = DateTime.UtcNow;
        var accessToken = TokenManager.GenerateTemporaryToken(discordUser.Id, TokenManager.AccessTokenLifetime);
        var refreshToken = TokenManager.GenerateRefreshToken();
        var refreshTokenHash = TokenManager.HashToken(refreshToken);

        var storedToken = new RefreshToken
        {
            TokenHash = refreshTokenHash,
            UserId = userId,
            IssuedAt = now,
            ExpiresAt = now.Add(TokenManager.RefreshTokenLifetime),
            UserAgent = Request.Headers.UserAgent.ToString(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };
        RefreshTokenRepository.Add(storedToken);

        return Ok(new AuthTokensResponse
        {
            AccessToken = accessToken,
            AccessTokenExpiresIn = (int)TokenManager.AccessTokenLifetime.TotalSeconds,
            RefreshToken = refreshToken,
            RefreshTokenExpiresIn = (int)TokenManager.RefreshTokenLifetime.TotalSeconds
        });
    }

    [HttpPost("ExchangeDiscordToken")]
    public async Task<IActionResult> ExchangeDiscordToken([FromBody] DiscordTokenExchangeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.AccessToken))
        {
            return BadRequest("Access token is required.");
        }

        var discordUser = await GetDiscordUserAsync(request.AccessToken);
        if (discordUser?.Id == null || !ulong.TryParse(discordUser.Id, out var userId))
        {
            return Unauthorized();
        }

        var now = DateTime.UtcNow;
        var accessToken = TokenManager.GenerateTemporaryToken(discordUser.Id, TokenManager.AccessTokenLifetime);
        var refreshToken = TokenManager.GenerateRefreshToken();
        var refreshTokenHash = TokenManager.HashToken(refreshToken);

        var storedToken = new RefreshToken
        {
            TokenHash = refreshTokenHash,
            UserId = userId,
            IssuedAt = now,
            ExpiresAt = now.Add(TokenManager.RefreshTokenLifetime),
            UserAgent = Request.Headers.UserAgent.ToString(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };
        RefreshTokenRepository.Add(storedToken);

        return Ok(new AuthTokensResponse
        {
            AccessToken = accessToken,
            AccessTokenExpiresIn = (int)TokenManager.AccessTokenLifetime.TotalSeconds,
            RefreshToken = refreshToken,
            RefreshTokenExpiresIn = (int)TokenManager.RefreshTokenLifetime.TotalSeconds
        });
    }

    [HttpPost("Refresh")]
    public IActionResult Refresh([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.RefreshToken))
        {
            return BadRequest("Refresh token is required.");
        }

        var tokenHash = TokenManager.HashToken(request.RefreshToken);
        var storedToken = RefreshTokenRepository.FindByHash(tokenHash);
        if (storedToken == null)
        {
            return Unauthorized();
        }

        if (storedToken.RevokedAt != null || storedToken.ReplacedByTokenHash != null || storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            return Unauthorized();
        }

        var now = DateTime.UtcNow;
        var newRefreshToken = TokenManager.GenerateRefreshToken();
        var newRefreshTokenHash = TokenManager.HashToken(newRefreshToken);

        storedToken.RevokedAt = now;
        storedToken.ReplacedByTokenHash = newRefreshTokenHash;
        RefreshTokenRepository.Update(storedToken);

        var rotatedToken = new RefreshToken
        {
            TokenHash = newRefreshTokenHash,
            UserId = storedToken.UserId,
            IssuedAt = now,
            ExpiresAt = now.Add(TokenManager.RefreshTokenLifetime),
            UserAgent = Request.Headers.UserAgent.ToString(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };
        RefreshTokenRepository.Add(rotatedToken);

        var accessToken = TokenManager.GenerateTemporaryToken(storedToken.UserId.ToString(), TokenManager.AccessTokenLifetime);

        return Ok(new AuthTokensResponse
        {
            AccessToken = accessToken,
            AccessTokenExpiresIn = (int)TokenManager.AccessTokenLifetime.TotalSeconds,
            RefreshToken = newRefreshToken,
            RefreshTokenExpiresIn = (int)TokenManager.RefreshTokenLifetime.TotalSeconds
        });
    }

    [Authorize]
    [HttpGet("TemporaryToken")]
    public IActionResult GenerateTemporaryToken()
    {
        var userClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier) ?? throw new InvalidCredentialException();
        var userId = userClaim.Value;

        var token = TokenManager.GenerateTemporaryToken(userId);
        return Ok(token);
    }

    private static async Task<DiscordUserInfo?> GetDiscordUserAsync(string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await Program.Client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DiscordUserInfo>(payload, JsonSerializerOptions);
    }

    private static async Task<string?> ExchangeCodeForDiscordTokenAsync(
        string code, string codeVerifier, string redirectUri, string clientId)
    {
        Log.Debug("Exchanging Discord code (PKCE). ClientId={ClientId}, RedirectUri={RedirectUri}, CodeLength={CodeLength}, CodeVerifierLength={CodeVerifierLength}",
            clientId, redirectUri, code?.Length ?? 0, codeVerifier?.Length ?? 0);
        
        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["code_verifier"] = codeVerifier
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token")
        {
            Content = new FormUrlEncodedContent(parameters)
        };

        var response = await Program.Client.SendAsync(request);
        var payload = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            Log.Warning("Discord token exchange failed. Status={StatusCode}, Response={ResponseBody}, ClientId={ClientId}, RedirectUri={RedirectUri}", 
                response.StatusCode, payload, clientId, redirectUri);
            return null;
        }

        var tokenResponse = JsonSerializer.Deserialize<DiscordTokenResponse>(payload, JsonSerializerOptions);
        
        if (tokenResponse?.AccessToken == null)
        {
            Log.Warning("Discord token exchange returned success but AccessToken was null. Response: {ResponseBody}", payload);
        }
        
        return tokenResponse?.AccessToken;
    }

    private sealed class DiscordTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }
}
