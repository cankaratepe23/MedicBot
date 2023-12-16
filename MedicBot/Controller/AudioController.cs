using System.Security.Authentication;
using System.Security.Claims;
using MedicBot.Exceptions;
using MedicBot.Manager;
using MedicBot.Repository;
using MedicBot.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace MedicBot.Controller;

[ApiController]
[Route("[controller]")]
public class AudioController : ControllerBase
{
    [HttpGet("JoinGuild/{guildId}")]
    public async Task<IActionResult> JoinGuild(ulong guildId)
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
    public async Task<IActionResult> JoinChannel(ulong channelId)
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
    public async Task<IActionResult> Leave(ulong guildId)
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
    [Authorize]
    public async Task<IActionResult> Play(ulong guildId, [FromQuery] string audioNameOrId,
        [FromQuery] bool searchById = false)
    {
        var userClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userClaim is null)
        {
            throw new InvalidCredentialException();
        }

        Log.Debug("User's ID is: {UserId}", userClaim.Value);
        try
        {
            // TODO: Change null to the discord member when Authentication is implemented
            await AudioManager.PlayAsync(audioNameOrId, guildId, Convert.ToUInt64(userClaim.Value), searchById);
        }
        catch (AudioTrackNotFoundException e)
        {
            return NotFound(e.Message);
        }

        return Ok();
    }

    [HttpGet]
    public IActionResult Get()
    {
        try
        {
            return Ok(AudioRepository.All().Select(t => t.ToDto()));
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}