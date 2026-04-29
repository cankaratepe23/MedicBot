using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using MedicBot.Manager;

namespace MedicBot.Commands;

public class MiscCommands : BaseCommandModule
{
    private readonly IMiscManager _miscManager;

    public MiscCommands(IMiscManager miscManager)
    {
        _miscManager = miscManager;
    }

    [Command("selçuk")]
    [Aliases("selcuk", "selcuksports")]
    public async Task SelcukSport(CommandContext ctx)
    {
        await ctx.RespondAsync(await _miscManager.GetSelcukSportsUrlAsync());
    }
}