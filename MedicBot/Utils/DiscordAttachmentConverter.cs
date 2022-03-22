using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;

namespace MedicBot.Utils;

public class DiscordAttachmentConverter : IArgumentConverter<DiscordAttachment>
{
    public Task<Optional<DiscordAttachment>> ConvertAsync(string value, CommandContext ctx)
    {
        throw new NotImplementedException();
    }
}