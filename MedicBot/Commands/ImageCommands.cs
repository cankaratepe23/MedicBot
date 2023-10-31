using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
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

    [Command("delete")]
    [Aliases("remove")]
    public async Task DeleteCommand(CommandContext ctx, [RemainingText] string imageName)
    {
        try
        {
            using (var fileStream = ImageManager.GetAsync(imageName))
            {
                var msg = new DiscordMessageBuilder().AddFile(fileStream).WithContent("Are you sure you want to delete this masterpiece? (Y or wait for timeout)");
                await ctx.RespondAsync(msg);
            }

            var result = await ctx.Message.GetNextMessageAsync(m =>
            {
                return m.Content.ToLower() == "y";
            });

            if (!result.TimedOut)
            {
                var response = await ImageManager.DeleteAsync(imageName, ctx.User.Id);
                await ctx.RespondAsync(response);
            }
            else
            {
                await ctx.RespondAsync("Timed out, not deleting anything.");
            }
        }
        catch (Exception e)
        {
            await ctx.RespondAsync(e.Message);
            return;
        }
    }
}
