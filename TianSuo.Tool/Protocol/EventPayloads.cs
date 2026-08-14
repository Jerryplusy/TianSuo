namespace TianSuo.Tool.Protocol
{
    public sealed class JoinPayload
    {
        public PlayerView Player { get; set; }
    }

    public sealed class QuitPayload
    {
        public PlayerView Player { get; set; }
    }

    public sealed class ChatPayload
    {
        public PlayerView Player { get; set; }
        public string Message { get; set; }
    }

    public sealed class DeathPayload
    {
        public PlayerView Player { get; set; }
        public string DeathMessage { get; set; }
    }
}
