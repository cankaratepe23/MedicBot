using MedicBot.Manager;
using Microsoft.AspNetCore.Mvc;

namespace MedicBot.Controller;

[ApiController]
[Route("[controller]")]
public class AudioController : ControllerBase
{
    [HttpGet("Join/{guildId}")]
    public async Task<OkResult> Join(ulong guildId)
    {
        await AudioManager.JoinAsync(guildId);
        return Ok();
    }

    [HttpGet("Leave/{guildId}")]
    public async Task<OkResult> Leave(ulong guildId)
    {
        await AudioManager.LeaveAsync(guildId);
        return Ok();
    }
}