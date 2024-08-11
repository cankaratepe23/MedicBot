using System.Globalization;
using System.Security.Authentication;
using System.Security.Claims;
using System.Text;
using MedicBot.Exceptions;
using MedicBot.Manager;
using MedicBot.Repository;
using MedicBot.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using MimeTypes;
using Serilog;
using ZstdSharp.Unsafe;

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

    [HttpGet("Play/{guildId}")] // TODO Play/audioId & guild ID from query instead
    [Authorize]
    public async Task<IActionResult> Play(ulong guildId, [FromQuery] string audioNameOrId,
        [FromQuery] bool searchById = false)
    {
        var userClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier) ?? throw new InvalidCredentialException();
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
    [Authorize]
    public IActionResult Get()
    {
        try
        {
            var lastUpdate = AudioManager.GetLatestUpdateTime();

            if (Request.Headers.IfModifiedSince.Count != 0)
            {
                var ifModifiedSinceStr = Request.Headers.IfModifiedSince[0];
                if (!string.IsNullOrEmpty(ifModifiedSinceStr))
                {
                    var ifModifiedSince = DateTimeOffset.ParseExact(ifModifiedSinceStr, "r", CultureInfo.InvariantCulture);
                    if (ifModifiedSince >= lastUpdate.AddTicks(-(lastUpdate.Ticks % TimeSpan.TicksPerSecond)))
                    {
                        return StatusCode(304);
                    }
                }
            }

            Response.Headers.LastModified = lastUpdate.ToHttpDate();
            Response.Headers.CacheControl = "no-cache";

            return Ok(AudioRepository.All().Select(t => t.ToDto()));
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("{audioId}")]
    [Authorize(Policy = "CombinedPolicy")]
    public IActionResult Get(string audioId, [FromQuery] ulong guildId)
    {
        try
        {
            var userClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier) ?? throw new InvalidCredentialException();
            Log.Debug("User's ID is: {UserId}", userClaim.Value);
            
            var track = AudioManager.FindById(audioId) ?? throw new AudioTrackNotFoundException($"No track was found with ID: {audioId}");

            var lastUpdate = DateTimeOffset.FromUnixTimeSeconds(track.Id.Timestamp);
            if (track.LastModifiedAt.HasValue)
            {
                lastUpdate = (DateTimeOffset) track.LastModifiedAt;
            }

            if (Request.Headers.IfModifiedSince.Count != 0)
            {
                var ifModifiedSinceStr = Request.Headers.IfModifiedSince[0];
                if (!string.IsNullOrEmpty(ifModifiedSinceStr))
                {
                    var ifModifiedSince = DateTimeOffset.ParseExact(ifModifiedSinceStr, "r", CultureInfo.InvariantCulture);
                    if (ifModifiedSince >= lastUpdate.AddTicks(-(lastUpdate.Ticks % TimeSpan.TicksPerSecond)))
                    {
                        return StatusCode(304);
                    }
                }
            }

            Response.Headers.LastModified = lastUpdate.ToHttpDate();
            Response.Headers.CacheControl = "no-cache";

            var fileBytes = System.IO.File.ReadAllBytes(track.Path);
            var fileBase64 = Convert.ToBase64String(fileBytes);
            var mimeType = MimeTypeMap.GetMimeType(track.Path[track.Path.LastIndexOf('.')..]);
            var htmlResponse = $"<audio controls style=\"margin: auto;padding-top: 50%;display: table;\"><source src=\"data:{mimeType};base64,{fileBase64}\" type=\"{mimeType}\"></audio>";
            return Content(htmlResponse, "text/html");
        }
        catch (AudioTrackNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}