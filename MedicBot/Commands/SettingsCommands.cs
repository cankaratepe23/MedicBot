using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MedicBot.EventHandler;
using MedicBot.Repository;
using MedicBot.Utils;

namespace MedicBot.Commands;

[Group("setting")]
[Aliases("settings")]
public class SettingsCommands : BaseCommandModule
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly BotSettingHandler _botSettingHandler;

    public SettingsCommands(ISettingsRepository settingsRepository, BotSettingHandler botSettingHandler)
    {
        _settingsRepository = settingsRepository;
        _botSettingHandler = botSettingHandler;
    }

    [Command("set")]
    public async Task SettingSetCommand(CommandContext ctx, string key, [RemainingText] string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            await ctx.RespondAsync("You need to enter a value for the setting.");
            return;
        }

        _settingsRepository.Set(key, value, ctx.IsPrivateChatWithOwner());
        _botSettingHandler.BotSettingChangedHandler(key);
        await ctx.Message.RespondThumbsUpAsync();
    }

    [Command("delete")]
    public async Task SettingDeleteCommand(CommandContext ctx, string key)
    {
        _settingsRepository.Delete(key);
        await ctx.Message.RespondThumbsUpAsync();
    }

    [Command("get")]
    public async Task SettingGetCommand(CommandContext ctx, string key)
    {
        var botSetting = _settingsRepository.Get(key, ctx.IsPrivateChatWithOwner());
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
        var allSettings = _settingsRepository.All(ctx.IsPrivateChatWithOwner());
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