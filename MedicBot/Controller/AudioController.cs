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
    private readonly IAudioManager _audioManager;
    private readonly IUserManager _userManager;

    public AudioController(IAudioManager audioManager, IUserManager userManager)
    {
        _audioManager = audioManager;
        _userManager = userManager;
    }

    [HttpGet("JoinGuild/{guildId}")]
    public async Task<IActionResult> JoinGuild(ulong guildId)
    {
        try
        {
            await _audioManager.JoinGuildIdAsync(guildId);
        }
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
            await _audioManager.JoinChannelIdAsync(channelId);
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
            await _audioManager.LeaveAsync(guildId);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }

        return Ok();
    }

    [HttpGet("Play/{guildId}")]
    [Authorize(Policy = "CombinedPolicy")]
    public async Task<IActionResult> Play(ulong guildId, [FromQuery] string audioNameOrId,
        [FromQuery] bool searchById = false)
    {
        var userId = GetCurrentUserId();
        int priceUsed = -1;
        Log.Debug("User's ID is: {UserId}", userId);
        try
        {
            priceUsed = await _audioManager.PlayAsync(audioNameOrId, guildId, userId, searchById);
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
            var userId = GetCurrentUserId();
            var userFavoriteTracks = _userManager.GetFavoriteTrackIds(userId);
            var recentTracks = _audioManager.GetRecentAudioTracks(userId)
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

    [HttpGet("Search")]
    [Authorize(Policy = "CombinedPolicy")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int limit = 10, [FromQuery] bool enriched = false)
    {
        var userId = GetCurrentUserId();
        
        limit = Math.Clamp(limit, 1, 30);
        
        try
        {
            var results = await _audioManager.FindAsync(q, limit, null, userId);
            
            if (enriched)
            {
                var userFavoriteTracks = _userManager.GetFavoriteTrackIds(userId);
                var enrichedResults = results.Select(t => t.ToDto().Enrich(userFavoriteTracks.Contains(t.Id)));
                return Ok(enrichedResults);
            }
            
            return Ok(results.Select(t => t.ToDto()));
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
        var userId = GetCurrentUserId();
        try
        {
            if (enriched != null && enriched.Value)
            {
                var userFavoriteTracks = _userManager.GetFavoriteTrackIds(userId);
                var allTracksToEnrich = await _audioManager.FindAsync(string.Empty, userId: userId);
                var allTrackDtosEnriched = allTracksToEnrich.Select(t => t.ToDto().Enrich(userFavoriteTracks.Contains(t.Id)));
                return Ok(allTrackDtosEnriched);
            }
            
            var lastUpdate = _audioManager.GetLatestUpdateTime();

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

            var allTracks = await _audioManager.FindAsync(string.Empty, userId: userId);
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
            var track = _audioManager.FindById(audioId) ?? throw new AudioTrackNotFoundException($"No track was found with ID: {audioId}");

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
            var track = _audioManager.FindById(audioId) ?? throw new AudioTrackNotFoundException($"No track was found with ID: {audioId}");

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

    private ulong GetCurrentUserId()
    {
        var userClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
                        ?? throw new InvalidCredentialException();
        return Convert.ToUInt64(userClaim.Value);
    }
}
