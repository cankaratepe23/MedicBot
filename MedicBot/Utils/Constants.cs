using DSharpPlus.Net;

namespace MedicBot.Utils;

public static class Constants
{
    public const string SerilogOutputTemplate =
        "[{Timestamp:yyyy-MM-dd HH:mm:ss zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    public const string AudioTracksPath = @"res";

    public const string LavalinkPassword = "5aJCTF!Z2&*853#79r7!xind*u^2LWy";
    private const string LavalinkHost = "127.0.0.1";
    private const int LavalinkPort = 3332;
    public const string BotTokenEnvironmentVariableName = "Bot_Token_Dev";
    public const string LiteDatabasePath = @"Filename=medicbot_store.db;connection=shared";

    public const string ThumbsUpUnicode = "👍";

    public const string JoinAsyncLavalinkNotConnectedLog =
        "JoinAsync() called before a Lavalink connection established";

    public const string LeaveAsyncLavalinkNotConnectedLog =
        "LeaveAsync() called before a Lavalink connection established";

    public const string LeaveAsyncLavalinkNotConnectedMessage =
        "LeaveAsync() called before a Lavalink connection established";

    public const string NotConnectedToVoiceLog = "LeaveAsync() called when bot not in a voice channel";
    public const string NotConnectedToVoiceMessage = "Not connected to any channel.";

    public const string DbCollectionInitializedAudioTracks = "AudioTrack collection in LiteDb initialized";
    public const string DbCollectionInitializedBotSettings = "BotSetting collection in LiteDb initialized";
    
    public const string MinNumberOfUsersNeededToEarnPoints = "min_number_of_users_needed_to_earn_points";
    
    public static readonly HashSet<string> IntegerSettingKeys = new() {MinNumberOfUsersNeededToEarnPoints};

    public static readonly char[] InvalidFileNameChars =
    {
        '\"', '<', '>', '|', '\0',
        (char) 1, (char) 2, (char) 3, (char) 4, (char) 5, (char) 6, (char) 7, (char) 8, (char) 9, (char) 10,
        (char) 11, (char) 12, (char) 13, (char) 14, (char) 15, (char) 16, (char) 17, (char) 18, (char) 19, (char) 20,
        (char) 21, (char) 22, (char) 23, (char) 24, (char) 25, (char) 26, (char) 27, (char) 28, (char) 29, (char) 30,
        (char) 31, ':', '*', '?', '\\', '/'
    };

    public static readonly ConnectionEndpoint LavalinkEndpoint = new(LavalinkHost, LavalinkPort);

    public static readonly string[] BotPrefixes = {"*"};
}