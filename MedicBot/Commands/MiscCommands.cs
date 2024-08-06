using System.Xml;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using HtmlAgilityPack;

namespace MedicBot.Commands;

public class MiscCommands : BaseCommandModule
{
    [Command("selçuk")]
    [Aliases("selcuk", "selcuksports")]
    public async Task SelcukSport(CommandContext ctx)
    {
        await ctx.RespondAsync(await MiscManager.GetSelcukSportsUrlAsync());
    }
}