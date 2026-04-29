using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using MedicBot.Manager;
using MedicBot.Utils;

namespace MedicBot.Commands;

[Group("image")]
[Aliases("im")]
public class ImageCommands : BaseCommandModule
{
    private readonly IImageManager _imageManager;

    public ImageCommands(IImageManager imageManager)
    {
        _imageManager = imageManager;
    }

    [Command("add")]
    public async Task AddCommand(CommandContext ctx, [RemainingText] string imageName)
    {
        try
        {
            await _imageManager.AddAsync(imageName, ctx.Message.Author.Id, ctx.Message.GetFirstAttachment().Url);
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
    public async Task SendCommand(CommandContext ctx, [RemainingText] string imageName = "")
    {
        try
        {
            using var fileStream = await _imageManager.FindAndOpenAsync(imageName);
            var msg = new DiscordMessageBuilder().AddFile(fileStream);
            await ctx.RespondAsync(msg);
        }
        catch (Exception e)
        {
            await ctx.RespondAsync(e.Message);
            return;
        }
    }

    [Command("list")]
    [Aliases("search")]
    public async Task ListCommand(CommandContext ctx, string searchTerm = "", long limit = 10)
    {
        try
        {
            var matchingImages = (await _imageManager.FindAsync(searchTerm, limit)).ToList();
            if (matchingImages.Count == 0)
            {
                await ctx.RespondAsync("No matching images found");
                return;
            }

            await ctx.Channel.SendPaginatedMessageAsync(ctx.User,
                ctx.Client.GetInteractivity().GeneratePagesInEmbed(string.Join("\n", matchingImages)));
        }
        catch (Exception e)
        {
            await ctx.RespondAsync(e.Message);
            throw;
        }
    }

    [Command("delete")]
    [Aliases("remove")]
    public async Task DeleteCommand(CommandContext ctx, [RemainingText] string imageName)
    {
        try
        {
            var imageToDelete = _imageManager.FindExact(imageName);
            using (var fileStream = _imageManager.OpenImage(imageToDelete))
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
                var response = await _imageManager.DeleteAsync(imageToDelete, ctx.User.Id);
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
