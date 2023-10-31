using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MedicBot.Utils;

namespace MedicBot;

[Group("image")]
[Aliases("im")]
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
    
    [Command("send")]
    [Aliases("s")]
    public async Task SendCommand(CommandContext ctx, [RemainingText] string imageName)
    {
        try
        {
            using var fileStream = ImageManager.GetAsync(imageName);
            var msg = new DiscordMessageBuilder().AddFile(fileStream);
            await ctx.RespondAsync(msg);
        }
        catch (Exception e)
        {
            await ctx.RespondAsync(e.Message);
            return;
        }
    }
}
