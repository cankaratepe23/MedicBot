﻿using System.Text;
using DSharpPlus.Entities;
using MedicBot.EventHandler;
using MedicBot.Model;
using MedicBot.Repository;
using MedicBot.Utils;
using MongoDB.Bson;
using Serilog;

namespace MedicBot.Manager;

public static class UserManager
{
    public static void AddPoints(DiscordUser member, int points)
    {
        UserPointsRepository.AddPoints(member.Id, points);
        Log.Debug("Added {Points} points to {Member}", points, member);
    }

    public static int GetPoints(DiscordUser user)
    {
        VoiceStateHandler.TrackerUserAddPoints(user);
        return UserPointsRepository.GetPoints(user.Id);
    }

    public static void AddPoints(DiscordUser member, TimeSpan time)
    {
        AddPoints(member, (int) Math.Floor(time.TotalSeconds));
    }

    public static void DeductPoints(DiscordUser member, int points)
    {
        if (member.Id == Constants.OwnerId)
        {
            var sillyZonkaWonkaValue = SettingsRepository.GetValue<string>(Constants.SillyZonkaWonka);
            var isSillyZonkaWonka = sillyZonkaWonkaValue != null && sillyZonkaWonkaValue.Trim().ToLowerInvariant() == "true";
            if (isSillyZonkaWonka)
            {
                return;
            }
        }

        UserPointsRepository.AddPoints(member.Id, (-1) * points);
        Log.Debug("Removed {Points} points from {Member}", points, member);
    }

    public static void DeductPoints(DiscordUser member, TimeSpan time)
    {
        DeductPoints(member, (int) Math.Floor(time.TotalSeconds));
    }

    public static void Mute(DiscordMember member, int minutes)
    {
        var userMute = UserMuteRepository.Get(member.Id);
        if (userMute == null)
        {
            UserMuteRepository.Set(member.Id, DateTime.UtcNow.AddMinutes(minutes));
        }
        else
        {
            UserMuteRepository.Set(member.Id, userMute.EndDateTime.AddMinutes(minutes));
        }
    }

    public static bool IsMuted(DiscordUser member)
    {
        var userMute = UserMuteRepository.Get(member.Id);
        if (userMute == null)
        {
            return false;
        }

        var userMuteEndDateTime = userMute.EndDateTime;
        if (DateTime.UtcNow < userMuteEndDateTime)
        {
            return true;
        }

        UserMuteRepository.Delete(userMute.Id);
        return false;
    }

    public static bool CanPlayAudio(DiscordMember member, AudioTrack audioTrack, out string reason)
    {
        var userPoints = GetPoints(member);
        var audioPrice = audioTrack.CalculateAndDecreasePrice();
        var trackPrice = audioPrice;

        var userHasEnoughPoints = userPoints >= trackPrice;
        var userIsNotMuted = !IsMuted(member);

        if (member.Id == Constants.OwnerId)
        {
            var sillyZonkaWonkaValue = SettingsRepository.GetValue<string>(Constants.SillyZonkaWonka);
            var isSillyZonkaWonka = sillyZonkaWonkaValue != null && sillyZonkaWonkaValue.Trim().ToLowerInvariant() == "true";
            if (isSillyZonkaWonka)
            {
                userHasEnoughPoints = true;
                userIsNotMuted = true;
            }
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
            var muteEndDateTime = UserMuteRepository.GetEndDateTime(member.Id);
            var muteRemaining = muteEndDateTime - DateTime.UtcNow;
            reasonBuilder.Append($"You are currently muted for the next {muteRemaining.ToPrettyString()}");
        }

        reason = reasonBuilder.ToString();
        return userHasEnoughPoints && userIsNotMuted;
    }

    public static HashSet<ObjectId> GetFavoriteTrackIds(ulong userId)
    {
        var userFavorites = UserFavoritesRepository.GetUserFavorites(userId);
        return new HashSet<ObjectId>(userFavorites.Select(f => f.TrackId));
    }

    public static void AddTrackToFavorites(ulong userId, AudioTrack track)
    {
        if (UserFavoritesRepository.IsFavorited(userId, track.Id))
        {
            Log.Information("Track {Track} is already favorited by {User}", track, userId);
            return;
        }

        UserFavoritesRepository.Add(new UserFavorite(userId, track.Id));
    }

    public static void RemoveTrackFromFavorites(ulong userId, AudioTrack track)
    {
        if (!UserFavoritesRepository.IsFavorited(userId, track.Id))
        {
            Log.Information("Track {Track} is not favorited by {User}", track, userId);
            return;
        }

        UserFavoritesRepository.DeleteByUserAndTrackId(userId, track.Id);
    }
}