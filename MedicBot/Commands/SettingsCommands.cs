using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MedicBot.Repository;
using MedicBot.Utils;

namespace MedicBot.Commands;

[Group("setting")]
public class SettingsCommands : BaseCommandModule
{
    [Command("set")]
    public async Task SettingSetCommand(CommandContext ctx, string key, [RemainingText] string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            await ctx.RespondAsync("You need to enter a value for the setting.");
            return;
        }

        SettingsRepository.Set(key, value);
        await ctx.Message.RespondThumbsUpAsync();
    }

    [Command("delete")]
    public async Task SettingDeleteCommand(CommandContext ctx, string key)
    {
        SettingsRepository.Delete(key);
        await ctx.Message.RespondThumbsUpAsync();
    }

    [Command("get")]
    public async Task SettingGetCommand(CommandContext ctx, string key)
    {
        var botSetting = SettingsRepository.Get(key);
        if (botSetting != null)
        {
            await ctx.RespondAsync($"Setting {key} has value: {botSetting.Value}");
        }
        else
        {
            await ctx.RespondAsync($"Setting {key} does not exist.");
        }
    }

    [Command("get")]
    public async Task SettingGetCommand(CommandContext ctx)
    {
        var allSettings = SettingsRepository.All();
        var builder = new DiscordEmbedBuilder().WithTitle("MedicBot Settings");
        foreach (var botSetting in allSettings)
        {
            var settingValue = botSetting.Value?.ToString();
            settingValue ??= "**null**";
            builder.AddField(botSetting.Key, settingValue);
        }

        await ctx.RespondAsync(builder);
    }
}