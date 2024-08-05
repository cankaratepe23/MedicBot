using System.Security.Authentication;
using System.Security.Claims;
using MedicBot.Manager;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicBot.Controller;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
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

    [Authorize]
    [HttpGet("TemporaryToken")]
    public IActionResult GenerateTemporaryToken()
    {
        var userClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier) ?? throw new InvalidCredentialException();
        var userId = userClaim.Value;

        var token = TokenManager.GenerateTemporaryToken(userId);
        return Ok(token);
    }
}