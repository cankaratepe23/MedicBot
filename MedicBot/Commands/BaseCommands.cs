using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using MedicBot.Repository;
using Serilog;

namespace MedicBot.Commands;

public class BaseCommands : BaseCommandModule
{
    [Command("test")]
    public async Task TestCommand(CommandContext ctx, [RemainingText] string remainingText)
    {
        Log.Information($"Test command called by {ctx.User}");
        await ctx.RespondAsync($"Test! Current time is: {DateTime.Now}");
        var message = "";

        var botSetting = SettingsRepository.GetBotSetting(remainingText);
        message = botSetting == null
            ? $"Could not find setting with key: \"{remainingText}\""
            : $"Found setting value: {botSetting}";

        await ctx.RespondAsync(message);
    }
}