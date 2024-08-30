using MedicBot.Manager;
using MedicBot.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicBot.Controller;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    // TODO Get userId from authorization instead of API path
    [HttpGet("{userId}/Favorites")]
    public IActionResult Get(ulong userId)
    {
        var userFavorites = UserManager.GetFavoriteTrackIds(userId).Select(id => id.ToString());
        return Ok(userFavorites);
    }

    [HttpPost("{userId}/Favorites")]
    public IActionResult Post(ulong userId, [FromBody] string trackId)
    {
        var track = AudioManager.FindById(trackId);
        if (track == null)
        {
            return NotFound();
        }

        UserManager.AddTrackToFavorites(userId, track);
        return Ok();
    }

    [HttpDelete("{userId}/Favorites")]
    public IActionResult Delete(ulong userId, [FromBody] string trackId)
    {
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
