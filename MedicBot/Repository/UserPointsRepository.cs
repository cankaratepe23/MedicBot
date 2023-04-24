﻿using MedicBot.Manager;
using MedicBot.Model;
using MedicBot.Utils;
using MongoDB.Driver;
using Serilog;

namespace MedicBot.Repository;

public static class UserPointsRepository
{
    private static readonly IMongoCollection<UserPoints> UserPointsCollection;

    static UserPointsRepository()
    {
        // Ensure db and collection is created.
        UserPointsCollection = MongoDbManager.Database.GetCollection<UserPoints>(UserPoints.CollectionName);
        Log.Information(Constants.DbCollectionInitializedUserPoints);
    }

    public static UserPoints? Get(ulong userId)
    {
        return UserPointsCollection.Find(p => p.Id == userId).FirstOrDefault();
    }

    public static int GetPoints(ulong userId)
    {
        // TODO Null reference exception
        return UserPointsCollection.Find(p => p.Id == userId).FirstOrDefault().Score;
    }

    public static void AddPoints(ulong userId, int score)
    {
        var currentPoints = Get(userId);

        if (currentPoints == null)
        {
            currentPoints = new UserPoints(userId, score);
        }
        else
        {
            currentPoints.Score += score;
        }

        UserPointsCollection.ReplaceOne(p => p.Id == currentPoints.Id, currentPoints, new ReplaceOptions {IsUpsert = true});
    }
}