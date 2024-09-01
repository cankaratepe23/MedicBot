using System.Security.Authentication;
using System.Security.Claims;
using MedicBot.Manager;
using MedicBot.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicBot.Controller;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    [HttpGet("@me/Favorites")]
    [Authorize]
    public IActionResult Get()
    {
        var userClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier) ?? throw new InvalidCredentialException();
        var userId = Convert.ToUInt64(userClaim.Value);
        var userFavorites = UserManager.GetFavoriteTrackIds(userId).Select(id => id.ToString());
        return Ok(userFavorites);
    }

    [HttpPost("@me/Favorites")]
    [Authorize]
    public IActionResult Post([FromBody] string trackId)
    {
        var userClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier) ?? throw new InvalidCredentialException();
        var userId = Convert.ToUInt64(userClaim.Value);
        var track = AudioManager.FindById(trackId);
        if (track == null)
        {
            return NotFound();
        }

        UserManager.AddTrackToFavorites(userId, track);
        return Ok();
    }

    [HttpDelete("@me/Favorites")]
    [Authorize]
    public IActionResult Delete([FromBody] string trackId)
    {
        var userClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier) ?? throw new InvalidCredentialException();
        var userId = Convert.ToUInt64(userClaim.Value);
        var track = AudioManager.FindById(trackId);
        // TODO Move to Manager method instead of directly using repository
        if (track == null || !UserFavoritesRepository.IsFavorited(userId, track.Id))
        {
            return NotFound();
        }

        UserFavoritesRepository.DeleteByUserAndTrackId(userId, track.Id);
        return Ok();
    }

}
