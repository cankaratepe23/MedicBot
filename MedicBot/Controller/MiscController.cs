using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicBot.Controller;

[ApiController]
public class MiscController : ControllerBase
{
    [HttpGet("selcuksports")]
    [Authorize(Policy = "CombinedPolicy")]
    public async Task<IActionResult> SelcukSports()
    {
        var url = await MiscManager.GetSelcukSportsUrlAsync();
        return Redirect(url);
    }
}
