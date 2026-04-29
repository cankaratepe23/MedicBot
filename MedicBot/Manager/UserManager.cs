using System.Text;
using DSharpPlus.Entities;
using MedicBot.EventHandler;
using MedicBot.Model;
using MedicBot.Repository;
using MedicBot.Utils;
using MongoDB.Bson;
using Serilog;

namespace MedicBot.Manager;

public class UserManager : IUserManager
{
    private readonly IUserPointsRepository _userPointsRepository;
    private readonly IUserMuteRepository _userMuteRepository;
    private readonly IUserFavoritesRepository _userFavoritesRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IVoiceStateHandler _voiceStateHandler;

    public UserManager(
        IUserPointsRepository userPointsRepository,
        IUserMuteRepository userMuteRepository,
        IUserFavoritesRepository userFavoritesRepository,
        ISettingsRepository settingsRepository,
        IVoiceStateHandler voiceStateHandler)
    {
        _userPointsRepository = userPointsRepository;
        _userMuteRepository = userMuteRepository;
        _userFavoritesRepository = userFavoritesRepository;
        _settingsRepository = settingsRepository;
        _voiceStateHandler = voiceStateHandler;
    }

    public void AddPoints(DiscordUser member, int points)
    {
        _userPointsRepository.AddPoints(member.Id, points);
        Log.Debug("Added {Points} points to {Member}", points, member);
    }

    public int GetPoints(DiscordUser user)
    {
        _voiceStateHandler.TrackerUserAddPoints(user);
        return _userPointsRepository.GetPoints(user.Id);
    }

    public async Task<int> GetPointsByIdAsync(ulong userId)
    {
        await _voiceStateHandler.TrackerUserAddPointsAsync(userId);
        return _userPointsRepository.GetPoints(userId);
    }

    public void AddPoints(DiscordUser member, TimeSpan time)
    {
        AddPoints(member, (int) Math.Floor(time.TotalSeconds));
    }

    public void DeductPoints(DiscordUser member, int points)
    {
        if (IsSillyZonkaWonka(member))
        {
            return;
        }

        _userPointsRepository.AddPoints(member.Id, (-1) * points);
        Log.Debug("Removed {Points} points from {Member}", points, member);
    }

    public void DeductPoints(DiscordUser member, TimeSpan time)
    {
        DeductPoints(member, (int) Math.Floor(time.TotalSeconds));
    }

    public void Mute(DiscordMember member, int minutes)
    {
        var userMute = _userMuteRepository.Get(member.Id);
        if (userMute == null)
        {
            _userMuteRepository.SetAsync(member.Id, DateTime.UtcNow.AddMinutes(minutes));
        }
        else
        {
            _userMuteRepository.SetAsync(member.Id, userMute.EndDateTime.AddMinutes(minutes));
        }
    }

    public bool IsMuted(DiscordUser member)
    {
        var userMute = _userMuteRepository.Get(member.Id);
        if (userMute == null)
        {
            return false;
        }

        var userMuteEndDateTime = userMute.EndDateTime;
        if (DateTime.UtcNow < userMuteEndDateTime)
        {
            return true;
        }

        _userMuteRepository.Delete(userMute.Id);
        return false;
    }

    private bool IsSillyZonkaWonka(DiscordUser user)
    {
        var sillyZonkaWonkaValue = _settingsRepository.GetValue<string>(Constants.SillyZonkaWonka);
        if (string.IsNullOrWhiteSpace(sillyZonkaWonkaValue))
        {
            return false;
        }
        var sillyZonkaWonkas = sillyZonkaWonkaValue.Split(',').Select(s => Convert.ToUInt64(s));
        var isSillyZonkaWonka = sillyZonkaWonkas.Contains(user.Id);
        return isSillyZonkaWonka;
    }

    public bool CanPlayAudio(DiscordMember member, AudioTrack audioTrack, out string reason)
    {
        var userPoints = GetPoints(member);
        var audioPrice = audioTrack.CalculateAndDecreasePrice(_settingsRepository);
        var trackPrice = audioPrice;

        var userHasEnoughPoints = userPoints >= trackPrice;
        var userIsNotMuted = !IsMuted(member);

        if (IsSillyZonkaWonka(member))
        {
            userHasEnoughPoints = true;
            userIsNotMuted = true;
        }

        var reasonBuilder = new StringBuilder();
        if (!userHasEnoughPoints)
        {
            reasonBuilder.Append($"You don't have enough points to play this audio. (You have: {userPoints}, you need: {trackPrice})");
        }
        if (!userHasEnoughPoints && !userIsNotMuted)
        {
            reasonBuilder.Append(" AND ");
        }
        if (!userIsNotMuted)
        {
            var muteEndDateTime = _userMuteRepository.GetEndDateTime(member.Id);
            var muteRemaining = muteEndDateTime.HasValue ? muteEndDateTime.Value - DateTime.UtcNow : TimeSpan.Zero;
            reasonBuilder.Append($"You are currently muted for the next {muteRemaining.ToPrettyString()}");
        }

        reason = reasonBuilder.ToString();
        return userHasEnoughPoints && userIsNotMuted;
    }

    public HashSet<ObjectId> GetFavoriteTrackIds(ulong userId)
    {
        var userFavorites = _userFavoritesRepository.GetUserFavorites(userId);
        return new HashSet<ObjectId>(userFavorites.Select(f => f.TrackId));
    }

    public void AddTrackToFavorites(ulong userId, AudioTrack track)
    {
        if (_userFavoritesRepository.IsFavorited(userId, track.Id))
        {
            Log.Information("Track {Track} is already favorited by {User}", track, userId);
            return;
        }

        _userFavoritesRepository.AddAsync(new UserFavorite(userId, track.Id));
    }

    public void RemoveTrackFromFavorites(ulong userId, AudioTrack track)
    {
        if (!_userFavoritesRepository.IsFavorited(userId, track.Id))
        {
            Log.Information("Track {Track} is not favorited by {User}", track, userId);
            return;
        }

        _userFavoritesRepository.DeleteByUserAndTrackIdAsync(userId, track.Id);
    }
}