using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace MedicBot.Controller;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    [HttpGet("TestLogin")]
    public async Task<IActionResult> EmulateLoginFromFrontend()
    {
        return Challenge(new AuthenticationProperties {RedirectUri = "/swagger/index.html"}, authenticationSchemes: "Discord");
    }

    [HttpGet("DiscordLogin")]
    public async Task<IActionResult> DiscordLogin([FromQuery] string code)
    {
        return Ok();
    }
}