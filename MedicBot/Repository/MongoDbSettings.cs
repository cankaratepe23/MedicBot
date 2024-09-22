namespace MedicBot.Repository;

public class MongoDbSettings
{
    public string? ConnectionString { get; set; }
    public string? Database { get; set; }

    public string? GetConnectionStringWithDatabaseName()
    {
        if (ConnectionString == null || Database == null)
        {
            return null;
        }

        var split = ConnectionString.Split('?');
        return split[0] + Database + '?' + split[1];
    }
}