using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TianSuo.Tool.Networking;
using TianSuo.Handling;
using TianSuo.Hooks;
using TianSuo.Settings;

namespace TianSuo
{
    [ApiVersion(2, 1)]
    public sealed class TianSuoPlugin : TerrariaPlugin
    {
        public override string Name => "TianSuo";
        public override Version Version => new Version(1, 0, 0);
        public override string Author => "TianSuo Contributors";
        public override string Description => "Terraria 服务器事件与消息的 WebSocket 桥";

        private PluginSettings _settings;
        private TianSuoServer _server;
        private EventForwarder _forwarder;
        private CommandCenter _commandCenter;

        public TianSuoPlugin(Main game) : base(game)
        {
        }

        public override void Initialize()
        {
            _settings = SettingsStore.Load(TShock.SavePath);
            if (!_settings.Enabled)
                return;

            var options = new ListenerOptions
            {
                Host = _settings.WebSocketHost,
                Port = _settings.WebSocketPort,
                ServerName = _settings.ServerName,
                AccessToken = _settings.AccessToken,
            };

            _server = new TianSuoServer(options);
            _server.MessageReceived += OnInboundMessage;

            _forwarder = new EventForwarder(this, _settings, _server);
            _commandCenter = new CommandCenter();

            Commands.ChatCommands.Add(new Command("tiansuo", CmdTianSuo, "tiansuo", "ts"));

            _forwarder.Register();
            _server.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (_server == null)
                return;

            _forwarder?.Unregister();
            _server.MessageReceived -= OnInboundMessage;
            _server.Stop();
            _server = null;
        }

        private void OnInboundMessage(Session session, string payload)
        {
            _commandCenter.Handle(_server, session, payload);
        }

        private void CmdTianSuo(CommandArgs args)
        {
            if (_server == null)
            {
                args.Player.SendInfoMessage("[TianSuo] 插件已禁用（Enabled=false）。");
                return;
            }

            args.Player.SendInfoMessage($"[TianSuo] 监听 {_settings.WebSocketHost}:{_settings.WebSocketPort}，已连接 {_server.ConnectedCount} 个客户端。");
        }
    }
}
