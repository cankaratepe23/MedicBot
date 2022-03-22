using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using MedicBot.Exceptions;
using Serilog;

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

    public static DiscordChannel? VoiceChannelWithMostNonBotUsers(
        this IReadOnlyDictionary<ulong, DiscordChannel> channels)
    {
        return channels.Values
            .Where(c => c.Type == ChannelType.Voice)
            .OrderByDescending(c => c.Users.Count(u => !u.IsBot))
            .FirstOrDefault();
    }

    public static DiscordAttachment GetFirstAttachment(this CommandContext ctx)
    {
        if (ctx.Message.Attachments.Count == 0 || ctx.Message.Attachments[0] == null)
        {
            Log.Warning("No attachments found in {Message}", ctx.Message);
            throw new AttachmentMissingException("This command requires an attachment.");
        }

        if (ctx.Message.Attachments.Count > 1) Log.Information("Ignoring multiple attachments sent to add command");

        return ctx.Message.Attachments[0];
    }
}