namespace MedicBot.Utils;

public static class Constants
{
    public const string SerilogOutputTemplate =
        "[{Timestamp:yyyy-MM-dd HH:mm:ss zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    public const string LavalinkPassword = "5aJCTF!Z2&*853#79r7!xind*u^2LWy";
    public const string BotTokenEnvironmentVariableName = "Bot_Token_Dev";
    public const string LiteDatabasePath = @"medicbot_store.db";

    public const string ThumbsUpUnicode = "👍";


    public const string JoinAsyncLavalinkNotConnectedLog =
        "JoinAsync() called before a Lavalink connection established";

    public const string JoinAsyncLavalinkNotConnectedMessage =
        "JoinAsync() called before a Lavalink connection established";

    public const string LeaveAsyncLavalinkNotConnectedLog =
        "LeaveAsync() called before a Lavalink connection established";

    public const string LeaveAsyncLavalinkNotConnectedMessage =
        "LeaveAsync() called before a Lavalink connection established";

    public const string NotVoiceChannel = "Not a voice channel.";
    public const string NotConnectedToVoiceLog = "LeaveAsync() called when bot not in a voice channel";
    public const string NotConnectedToVoiceMessage = "Not connected to any channel.";

    public const string DbCollectionInitializedBotSettings = "BotSetting collection in LiteDb initialized";

    public static readonly string[] BotPrefixes = {"*"};
}