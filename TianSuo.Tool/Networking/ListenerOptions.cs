namespace TianSuo.Tool.Networking
{
    public sealed class ListenerOptions
    {
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 8080;
        public string ServerName { get; set; } = "TianSuo";
        public string AccessToken { get; set; } = "";
    }
}
