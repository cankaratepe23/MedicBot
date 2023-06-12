using System.Globalization;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;

namespace MedicBot.Utils;

public class StringLowercaseConverter : IArgumentConverter<string>
{
    Task<Optional<string>> IArgumentConverter<string>.ConvertAsync(string value, CommandContext ctx)
    {
        if (Uri.IsWellFormedUriString(value, UriKind.Absolute))
        {
            return Task.FromResult(Optional.FromValue(value));
        }

        return Task.FromResult(Optional.FromValue(value.ToLower(CultureInfo.GetCultureInfo("tr"))));
    }
}