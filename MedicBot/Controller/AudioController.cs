using System.Globalization;
using System.Security.Authentication;
using System.Security.Claims;
using MedicBot.Exceptions;
using MedicBot.Manager;
using MedicBot.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using MimeTypes;
using MedicBot.Model;

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

    // TODO This should be POST, why is it GET?
    [HttpGet("Play/{guildId}")] // TODO Play/audioId & guild ID from query instead
    [Authorize(Policy = "CombinedPolicy")]
    public async Task<IActionResult> Play(ulong guildId, [FromQuery] string audioNameOrId,
        [FromQuery] bool searchById = false)
    {
        var userClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier) ?? throw new InvalidCredentialException();
        int priceUsed = -1;
        Log.Debug("User's ID is: {UserId}", userClaim.Value);
        try
        {
            priceUsed = await AudioManager.PlayAsync(audioNameOrId, guildId, Convert.ToUInt64(userClaim.Value), searchById);
        }
        catch (AudioTrackNotFoundException e)
        {
            return NotFound(e.Message);
        }

        return Ok(priceUsed);
    }

    [HttpGet("Recents")]
    [Authorize(Policy = "CombinedPolicy")]
    public IActionResult Recents()
    {
        try
        {
            var userClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier) ?? throw new InvalidCredentialException();
            Log.Debug("User's ID is: {UserId}", userClaim.Value);
            var userId = Convert.ToUInt64(userClaim.Value);
            var userFavoriteTracks = UserManager.GetFavoriteTrackIds(userId);
            var recentTracks = AudioManager.GetRecentAudioTracks(Convert.ToUInt64(userClaim.Value))
                                            .Select(t => new RecentAudioTrackDto
                                            {
                                                AudioTrackDto = t.AudioTrack?.ToDto().Enrich(userFavoriteTracks.Contains(t.AudioTrack.Id)),
                                                Order = t.Count
                                            });
            return Ok(recentTracks);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet]
    [Authorize(Policy = "CombinedPolicy")]
    public async Task<IActionResult> Get([FromQuery] bool? enriched)
    {
        var userClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier) ?? throw new InvalidCredentialException();
        var userId = Convert.ToUInt64(userClaim.Value);
        try
        {
            if (enriched != null && enriched.Value)
            {
                // TODO Change caching logic to include enriched endpoint
                var userFavoriteTracks = UserManager.GetFavoriteTrackIds(userId);
                var allTracksToEnrich = await AudioManager.FindAsync(string.Empty, userId: userId);
                var allTrackDtosEnriched = allTracksToEnrich.Select(t => t.ToDto().Enrich(userFavoriteTracks.Contains(t.Id)));
                return Ok(allTrackDtosEnriched);
            }
            
            var lastUpdate = AudioManager.GetLatestUpdateTime();

            Response.Headers.LastModified = lastUpdate.ToHttpDate();
            Response.Headers.CacheControl = "no-cache";

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

            var allTracks = await AudioManager.FindAsync(string.Empty, userId: userId);
            return Ok(allTracks.Select(t => t.ToDto()));
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
                lastUpdate = (DateTimeOffset)track.LastModifiedAt;
            }

            Response.Headers.LastModified = lastUpdate.ToHttpDate();
            Response.Headers.CacheControl = "no-cache";

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

            var mimeType = MimeTypeMap.GetMimeType(track.Path[track.Path.LastIndexOf('.')..]);
            Response.Headers.ContentType = mimeType;

            var file = System.IO.File.OpenRead(track.Path);
            return Ok(file);
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

    [HttpHead("{audioId}")]
    [Authorize(Policy = "CombinedPolicy")]
    public IActionResult Head(string audioId, [FromQuery] ulong guildId)
    {
        try
        {
            var userClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier) ?? throw new InvalidCredentialException();
            Log.Debug("User's ID is: {UserId}", userClaim.Value);

            var track = AudioManager.FindById(audioId) ?? throw new AudioTrackNotFoundException($"No track was found with ID: {audioId}");

            var lastUpdate = DateTimeOffset.FromUnixTimeSeconds(track.Id.Timestamp);
            if (track.LastModifiedAt.HasValue)
            {
                lastUpdate = (DateTimeOffset)track.LastModifiedAt;
            }

            Response.Headers.LastModified = lastUpdate.ToHttpDate();
            Response.Headers.CacheControl = "no-cache";

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

            var mimeType = MimeTypeMap.GetMimeType(track.Path[track.Path.LastIndexOf('.')..]);
            Response.Headers.ContentType = mimeType;
            return Ok();
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
