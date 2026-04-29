using MedicBot.Manager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicBot.Controller;

[ApiController]
public class MiscController : ControllerBase
{
    private readonly IMiscManager _miscManager;

    public MiscController(IMiscManager miscManager)
    {
        _miscManager = miscManager;
    }

    [HttpGet("selcuksports")]
    [Authorize(Policy = "CombinedPolicy")]
    public async Task<IActionResult> SelcukSports()
    {
        var url = await _miscManager.GetSelcukSportsUrlAsync();
        return Redirect(url);
    }
}
