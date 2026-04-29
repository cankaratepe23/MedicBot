using DSharpPlus.Entities;
using MedicBot.Model;
using MongoDB.Bson;

namespace MedicBot.Manager;

public interface IUserManager
{
    void AddPoints(DiscordUser member, int points);
    int GetPoints(DiscordUser user);
    Task<int> GetPointsByIdAsync(ulong userId);
    void AddPoints(DiscordUser member, TimeSpan time);
    void DeductPoints(DiscordUser member, int points);
    void DeductPoints(DiscordUser member, TimeSpan time);
    void Mute(DiscordMember member, int minutes);
    bool IsMuted(DiscordUser member);
    bool CanPlayAudio(DiscordMember member, AudioTrack audioTrack, out string reason);
    HashSet<ObjectId> GetFavoriteTrackIds(ulong userId);
    void AddTrackToFavorites(ulong userId, AudioTrack track);
    void RemoveTrackFromFavorites(ulong userId, AudioTrack track);
}
