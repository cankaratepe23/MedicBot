using System.Security.Authentication;
using System.Security.Claims;
using MedicBot.Manager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicBot.Controller;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserManager _userManager;
    private readonly IAudioManager _audioManager;

    public UserController(IUserManager userManager, IAudioManager audioManager)
    {
        _userManager = userManager;
        _audioManager = audioManager;
    }

    [HttpGet("@me/Favorites")]
    [Authorize(Policy = "CombinedPolicy")]
    public IActionResult GetFavorites()
    {
        var userId = GetCurrentUserId();
        var userFavorites = _userManager.GetFavoriteTrackIds(userId).Select(id => id.ToString());
        return Ok(userFavorites);
    }

    [HttpPost("@me/Favorites/{trackId}")]
    [Authorize(Policy = "CombinedPolicy")]
    public IActionResult AddFavorite(string trackId)
    {
        var userId = GetCurrentUserId();
        var track = _audioManager.FindById(trackId);
        if (track == null)
        {
            return NotFound();
        }

        _userManager.AddTrackToFavorites(userId, track);
        return Ok();
    }

    [HttpDelete("@me/Favorites/{trackId}")]
    [Authorize(Policy = "CombinedPolicy")]
    public IActionResult DeleteFavorite(string trackId)
    {
        var userId = GetCurrentUserId();
        var track = _audioManager.FindById(trackId);
        if (track == null)
        {
            return NotFound();
        }

        _userManager.RemoveTrackFromFavorites(userId, track);
        return Ok();
    }

    [HttpGet("@me/Balance")]
    [Authorize(Policy = "CombinedPolicy")]
    public async Task<IActionResult> GetBalance()
    {
        var userId = GetCurrentUserId();
        var userBalance = await _userManager.GetPointsByIdAsync(userId);
        return Ok(userBalance);
    }

    private ulong GetCurrentUserId()
    {
        var userClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
                        ?? throw new InvalidCredentialException();
        return Convert.ToUInt64(userClaim.Value);
    }
}
