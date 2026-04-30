using MedicBot.Model;
using MedicBot.Utils;
using MongoDB.Driver;
using Serilog;

namespace MedicBot.Repository;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IMongoCollection<RefreshToken> _collection;

    public RefreshTokenRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<RefreshToken>(RefreshToken.CollectionName);
        Log.Information(Constants.DbCollectionInitializedRefreshTokens);
    }

    public void Add(RefreshToken refreshToken)
    {
        _collection.InsertOne(refreshToken);
    }

    public RefreshToken? FindByHash(string tokenHash)
    {
        return _collection.Find(t => t.TokenHash == tokenHash).FirstOrDefault();
    }

    public bool Update(RefreshToken refreshToken)
    {
        var result = _collection.ReplaceOne(t => t.Id == refreshToken.Id, refreshToken);
        return result.MatchedCount == 1;
    }
}
