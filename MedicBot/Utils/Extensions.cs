using DSharpPlus.Entities;

namespace MedicBot.Utils;

public static class Extensions
{
    public static async Task RespondThumbsUpAsync(this DiscordMessage message)
    {
        await message.CreateReactionAsync(DiscordEmoji.FromUnicode(Constants.ThumbsUpUnicode));
    }
}