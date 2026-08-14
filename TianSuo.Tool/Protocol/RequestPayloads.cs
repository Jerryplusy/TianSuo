namespace TianSuo.Tool.Protocol
{
    public sealed class BroadcastRequest
    {
        public string Message { get; set; }
    }

    public sealed class PrivateMessageRequest
    {
        public string Target { get; set; }
        public string Message { get; set; }
    }

    public sealed class TitleRequest
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
    }

    public sealed class ActionBarRequest
    {
        public string Message { get; set; }
    }

    public sealed class RconRequest
    {
        public string Command { get; set; }
    }

    public sealed class ApiResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
