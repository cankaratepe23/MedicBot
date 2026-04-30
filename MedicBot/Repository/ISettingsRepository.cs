using MedicBot.Model;

namespace MedicBot.Repository;

public interface ISettingsRepository
{
    BotSetting? Get(string key, bool includeHidden = false);
    T? GetValue<T>(string key);
    IEnumerable<BotSetting> All(bool includeHidden = false);
    void Set(string key, object value, bool includeHidden = false);
    void Delete(string key);
}
