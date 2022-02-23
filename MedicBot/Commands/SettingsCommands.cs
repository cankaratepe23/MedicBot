using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using MedicBot.Repository;
using MedicBot.Utils;

namespace MedicBot.Commands;

[Group("setting")]
public class SettingsCommands : BaseCommandModule
{
    [Command("set")]
    public async Task SettingSetCommand(CommandContext ctx, string key, [RemainingText] string value)
    {
        SettingsRepository.SetBotSetting(key, value);
        await ctx.Message.RespondThumbsUpAsync();
    }

    [Command("delete")]
    public async Task SettingDeleteCommand(CommandContext ctx, string key)
    {
        SettingsRepository.DeleteBotSetting(key);
        await ctx.Message.RespondThumbsUpAsync();
    }

    [Command("get")]
    public async Task SettingGetCommand(CommandContext ctx, string key)
    {
        var botSetting = SettingsRepository.GetBotSetting(key);
        if (botSetting != null)
            await ctx.RespondAsync($"Setting {key} has value: {botSetting.Value}");
        else
            await ctx.RespondAsync($"Setting {key} does not exist.");
    }
}