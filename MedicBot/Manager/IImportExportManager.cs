namespace MedicBot.Manager;

public interface IImportExportManager
{
    Task<int> Import(string url);
}
