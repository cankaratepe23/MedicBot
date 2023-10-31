using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using MedicBot.Utils;

namespace MedicBot;

[Group("image")]
public class ImageCommands : BaseCommandModule
{
    [Command("add")]
    public async Task AddCommand(CommandContext ctx, [RemainingText] string imageName)
    {
        try
        {
            await ImageManager.AddAsync(imageName, ctx.Message.Author.Id, ctx.Message.GetFirstAttachment().Url);
        }
        catch (Exception e)
        {
            await ctx.RespondAsync(e.Message);
            return;
        }

        await ctx.Message.RespondThumbsUpAsync();
    }
}
