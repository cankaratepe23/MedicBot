using DSharpPlus.Net;

namespace MedicBot.Utils;

public static class Constants
{
    public const string SerilogOutputTemplate =
        "[{Timestamp:yyyy-MM-dd HH:mm:ss zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    public const string AudioTracksPath = @"res";
    public const string ImagesPath = @"img";
    public const string TempFilesPath = @"temp";

    public const ulong OwnerId = 134336937224830977;
    public static readonly HashSet<ulong> WhitelistedGuilds = new() { 463052720509812736 };
    public const string LavalinkPassword = "5aJCTF!Z2&*853#79r7!xind*u2LWy";
    public const string LavalinkHttp = "http://127.0.0.1:3332";
    public const string LavalinkWebSocket = "ws://127.0.0.1:3332/v4/websocket";
    public const string BotTokenEnvironmentVariableName = "MedicBot_Token_Dev";
    public const string OAuthClientIdEnvironmentVariableName = "MedicBot_OAuth_ClientID";
    public const string OAuthClientSecretEnvironmentVariableName = "MedicBot_OAuth_ClientSecret";
    public const string JwtTokenSecretEnvironmentVariableName = "MedicBot_Jwt_Secret";
    public const string LiteDatabasePath = @"Filename=medicbot_store.db";

    public const string ThumbsUpUnicode = "👍";

    public const string JoinAsyncLavalinkNotConnectedLog =
        "JoinAsync() called before a Lavalink connection established";

    public const string LeaveAsyncLavalinkNotConnectedLog =
        "LeaveAsync() called before a Lavalink connection established";

    public const string LeaveAsyncLavalinkNotConnectedMessage =
        "LeaveAsync() called before a Lavalink connection established";

    public const string NotConnectedToVoiceLog = "LeaveAsync() called when bot not in a voice channel";
    public const string NotConnectedToVoiceMessage = "Not connected to any channel.";

    // TODO All DB collection init logs should be using this format string
    public const string DbCollectionInitializedFormatString = "{0} collection initialized";
    public const string DbCollectionInitializedAudioTracks = "AudioTrack collection initialized";
    public const string DbCollectionInitializedReactionImages = "ReactionImage collection initialized";
    public const string DbCollectionInitializedBotSettings = "BotSetting collection initialized";
    public const string DbCollectionInitializedUserPoints = "UserPoints collection initialized";
    public const string DbCollectionInitializedUserMutes = "UserMute collection initialized";
    public const string DbCollectionInitializedUserFavorites = "UserFavorites collection initialized";
    public const string DbCollectionInitializedRefreshTokens = "RefreshToken collection initialized";


    public const string MinNumberOfUsersNeededToEarnPoints = "min_number_of_users_needed_to_earn_points";
    public const string DefaultScore = "default_score";
    public const string PriceIncreasePerUse = "price_increase_per_use";
    public const string PriceDecreasePerMinute = "price_decrease_per_minute";
    public const string PriceMaximum = "price_maximum";
    public const string RandomTimeout = "random_timeout";
    public const string SillyZonkaWonka = "silly_zonka_wonka";
    public const string GlobalTesters = "global_testers";

    public static readonly HashSet<string> ObservedSettingKeys = new() {MinNumberOfUsersNeededToEarnPoints};

    public static readonly HashSet<string>
        IntegerSettingKeys = new() {MinNumberOfUsersNeededToEarnPoints, DefaultScore, PriceIncreasePerUse, PriceDecreasePerMinute, PriceMaximum};

    public static readonly HashSet<string> HiddenSettingsKeys = new() {SillyZonkaWonka};

    public static readonly char[] InvalidFileNameChars =
    {
        '\"', '<', '>', '|', '\0',
        (char) 1, (char) 2, (char) 3, (char) 4, (char) 5, (char) 6, (char) 7, (char) 8, (char) 9, (char) 10,
        (char) 11, (char) 12, (char) 13, (char) 14, (char) 15, (char) 16, (char) 17, (char) 18, (char) 19, (char) 20,
        (char) 21, (char) 22, (char) 23, (char) 24, (char) 25, (char) 26, (char) 27, (char) 28, (char) 29, (char) 30,
        (char) 31, ':', '*', '?', '\\', '/'
    };

    public static readonly string[] BotPrefixes = {"*", "$"};
}
