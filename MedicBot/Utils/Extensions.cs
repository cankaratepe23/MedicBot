using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
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

    public static DiscordAttachment GetFirstAttachment(this DiscordMessage message)
    {
        if (message.Attachments.Count == 0 || message.Attachments[0] == null)
        {
            Log.Warning("No attachments found in {Message}", message);
            throw new AttachmentMissingException(
                "The message you sent has no attachment. This command requires an attachment.");
        }

        if (message.Attachments.Count > 1)
        {
            Log.Information("Ignoring multiple attachments sent to add command");
        }

        return message.Attachments[0];
    }

    /// <summary>
    ///     Finds a guild with the given guildId. Wraps Client.Guilds.TryGetValue() with logging and exception
    ///     that will be thrown if the guild cannot be found.
    /// </summary>
    /// <param name="guildId">The guild ID to search for.</param>
    /// <returns>The Guild object with the given ID.</returns>
    /// <exception cref="Exception">Exception with user-friendly message stating the guild cannot be found.</exception>
    public static DiscordGuild FindGuild(this DiscordClient client, ulong guildId)
    {
        var guildExists = client.Guilds.TryGetValue(guildId, out var guild);
        if (guildExists && guild != null)
        {
            return guild;
        }

        Log.Warning("Guild with ID: {Id} not found", guildId);
        throw new Exception($"Guild with ID: {guildId} not found");
    }

    public static IEnumerable<DiscordMember> GetNonBotUsers(this DiscordChannel channel)
    {
        return channel.Users.Where(member => !member.IsBot);
    }

    public static int CountNonBotUsers(this DiscordChannel channel)
    {
        return channel.Users.Count(member => !member.IsBot);
    }

    public static bool IsJoinEvent(this VoiceStateUpdateEventArgs e)
    {
        return (e.Before == null || e.Before.Channel == null) && e.After != null && e.After.Channel != null;
    }

    public static bool IsDisconnectEvent(this VoiceStateUpdateEventArgs e)
    {
        return (e.After == null || e.After.Channel == null) && e.Before != null && e.Before.Channel != null;
    }
}