using DSharpPlus.Entities;

namespace MedicBot.Utils;

public static class Extensions
{
    public static async Task RespondThumbsUpAsync(this DiscordMessage message)
    {
        await message.CreateReactionAsync(DiscordEmoji.FromUnicode(Constants.ThumbsUpUnicode));
    }

    /// <summary>
    ///     Checks whether the given string contains any of the invalid characters listed in the Constants store.
    /// </summary>
    /// <returns>True, for strings that don't contain any invalid characters.</returns>
    public static bool IsValidFileName(this string stringToCheck)
    {
        return !stringToCheck.Any(Constants.InvalidFileNameChars.Contains);
    }
}