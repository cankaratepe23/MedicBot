using MedicBot.Manager;
using MedicBot.Model;
using MedicBot.Utils;
using MongoDB.Driver;
using Serilog;

namespace MedicBot.Repository;

public static class RefreshTokenRepository
{
    private static readonly IMongoCollection<RefreshToken> RefreshTokensCollection;

    static RefreshTokenRepository()
    {
        RefreshTokensCollection = MongoDbManager.Database.GetCollection<RefreshToken>(RefreshToken.CollectionName);
        Log.Information(Constants.DbCollectionInitializedRefreshTokens);
    }

    public static void Add(RefreshToken refreshToken)
    {
        RefreshTokensCollection.InsertOne(refreshToken);
    }

    public static RefreshToken? FindByHash(string tokenHash)
    {
        return RefreshTokensCollection.Find(t => t.TokenHash == tokenHash).FirstOrDefault();
    }

    public static bool Update(RefreshToken refreshToken)
    {
        var result = RefreshTokensCollection.ReplaceOne(t => t.Id == refreshToken.Id, refreshToken);
        return result.MatchedCount == 1;
    }
}
