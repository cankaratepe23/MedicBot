using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace MedicBot.Controller;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    [HttpGet("TestLogin")]
    public IActionResult TestLogin()
    {
        return Challenge(new AuthenticationProperties {RedirectUri = "https://medicbot.comaristan.com/"}, authenticationSchemes: "Discord");
    }

    [HttpGet("DiscordLogin")]
    public IActionResult DiscordLogin([FromQuery] string code)
    {
        return Ok();
    }
}