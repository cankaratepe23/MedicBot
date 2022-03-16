using MedicBot.Exceptions;
using MedicBot.Manager;
using Microsoft.AspNetCore.Mvc;

namespace MedicBot.Controller;

[ApiController]
[Route("[controller]")]
public class AudioController : ControllerBase
{
    [HttpGet("JoinGuild/{guildId}")]
    public async Task<ActionResult> JoinGuild(ulong guildId)
    {
        try
        {
            await AudioManager.JoinGuildIdAsync(guildId);
        }
        /* TODO Maybe send different error codes for different types of exceptions like lavalink exception, guild not found exception etc. */
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
        return Ok();
    }

    [HttpGet("JoinChannel/{channelId}")]
    public async Task<ActionResult> JoinChannel(ulong channelId)
    {
        try
        {
            await AudioManager.JoinChannelIdAsync(channelId);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
        return Ok();
    }

    [HttpGet("Leave/{guildId}")]
    public async Task<ActionResult> Leave(ulong guildId)
    {
        try
        {
            await AudioManager.LeaveAsync(guildId);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
        return Ok();
    }

    [HttpGet("Play/{guildId}")]
    public async Task<ActionResult> Play(ulong guildId, [FromQuery]string audioNameOrId, [FromQuery]bool searchById = false)
    {
        try
        {
            await AudioManager.PlayAsync(audioNameOrId, guildId);
        }
        catch (AudioTrackNotFoundException e)
        {
            return NotFound(e.Message);
        }
        return Ok();
    }
}