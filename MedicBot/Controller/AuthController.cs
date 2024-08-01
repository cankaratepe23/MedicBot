using Microsoft.AspNetCore.Authentication;
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
}