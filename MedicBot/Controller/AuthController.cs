using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Security.Claims;
using System.Text.Json;
using MedicBot;
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
}
