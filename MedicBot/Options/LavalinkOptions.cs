namespace MedicBot.Options;

public class LavalinkOptions
{
    public const string SectionName = "Lavalink";

    public string Password { get; set; } = "5aJCTF!Z2&*853#79r7!xind*u2LWy";
    public string HttpEndpoint { get; set; } = "http://127.0.0.1:3332";
    public string WebSocketEndpoint { get; set; } = "ws://127.0.0.1:3332/v4/websocket";
}
