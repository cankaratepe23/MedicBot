using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using MedicBot.Model;

namespace MedicBot.Manager;

public interface IAudioManager
{
    Task AddAsync(string audioName, ulong userId, string url);
    void AddTag(AudioTrack audioTrack, string tagName);
    Task DeleteAsync(string audioName, ulong userId);
    Task<IEnumerable<AudioTrack>> FindAsync(string searchQuery, long limit = 10, DiscordGuild? guild = null, ulong? userId = null);
    AudioTrack? FindById(string id);
    IEnumerable<AudioTrack> GetNewTracksAsync(long limit = 10);
    DateTimeOffset GetLatestUpdateTime();
    IEnumerable<AudioTrack>? GetLastPlayedTracks(DiscordGuild guild);
    IEnumerable<RecentAudioTrack> GetFrequentlyUsedTracks(ulong userId);
    IEnumerable<RecentAudioTrack> GetRecentAudioTracks(ulong userId);
    IEnumerable<RecentAudioTrack> GetRecentAudioTracks(DiscordUser user);
    IEnumerable<RecentAudioTrack> GetFrequentlyUsedTracks(DiscordUser user);
    Task<Lavalink4NET.Players.LavalinkPlayer> JoinAsync(DiscordChannel channel);
    Task JoinChannelIdAsync(ulong channelId);
    Task JoinGuildIdAsync(ulong guildId);
    Task JoinGuildAsync(DiscordGuild guild);
    Task LeaveAsync(DiscordGuild guild);
    Task LeaveAsync(ulong guildId);
    Task<int> PlayAsync(AudioTrack audioTrack, DiscordGuild guild, DiscordMember member, CommandContext? ctx = null);
    Task PlayAsync(Uri audioUrl, DiscordGuild guild, DiscordMember member);
    Task<int> PlayAsync(string audioName, DiscordGuild guild, DiscordMember member, CommandContext? ctx = null, bool searchById = false);
    Task<int> PlayAsync(string audioNameOrId, ulong guildId, ulong memberId, bool searchById = false);
}
