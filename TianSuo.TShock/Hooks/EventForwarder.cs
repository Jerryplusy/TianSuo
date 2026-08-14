using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TianSuo.Tool.Networking;
using TianSuo.Tool.Protocol;
using TianSuo.Settings;

namespace TianSuo.Hooks
{
    public sealed class EventForwarder
    {
        private readonly TerrariaPlugin _plugin;
        private readonly PluginSettings _settings;
        private readonly TianSuoServer _server;

        public EventForwarder(TerrariaPlugin plugin, PluginSettings settings, TianSuoServer server)
        {
            _plugin = plugin;
            _settings = settings;
            _server = server;
        }

        public void Register()
        {
            if (_settings.SubscribePlayerJoin)
                ServerApi.Hooks.ServerJoin.Register(_plugin, OnJoin);

            if (_settings.SubscribePlayerQuit)
                ServerApi.Hooks.ServerLeave.Register(_plugin, OnLeave);

            if (_settings.SubscribePlayerChat)
                ServerApi.Hooks.ServerChat.Register(_plugin, OnChat);

            if (_settings.SubscribePlayerDeath)
                GetDataHandlers.KillMe += OnKillMe;
        }

        public void Unregister()
        {
            if (_settings.SubscribePlayerJoin)
                ServerApi.Hooks.ServerJoin.Deregister(_plugin, OnJoin);

            if (_settings.SubscribePlayerQuit)
                ServerApi.Hooks.ServerLeave.Deregister(_plugin, OnLeave);

            if (_settings.SubscribePlayerChat)
                ServerApi.Hooks.ServerChat.Deregister(_plugin, OnChat);

            if (_settings.SubscribePlayerDeath)
                GetDataHandlers.KillMe -= OnKillMe;
        }

        private void OnJoin(JoinEventArgs e)
        {
            var player = TShock.Players[e.Who];
            if (player == null)
                return;

            _server.Broadcast(EnvelopeBuilder.Event(ProtocolNames.PlayerJoin, new JoinPayload { Player = ToView(player) }));
        }

        private void OnLeave(LeaveEventArgs e)
        {
            var player = TShock.Players[e.Who];
            var view = player != null ? ToView(player) : ToViewFromGame(e.Who);

            if (view == null)
                return;

            _server.Broadcast(EnvelopeBuilder.Event(ProtocolNames.PlayerQuit, new QuitPayload { Player = view }));
        }

        private void OnChat(ServerChatEventArgs e)
        {
            if (e.Handled)
                return;

            var player = TShock.Players[e.Who];
            if (player == null || string.IsNullOrWhiteSpace(e.Text))
                return;

            var payload = new ChatPayload { Player = ToView(player), Message = e.Text };
            _server.Broadcast(EnvelopeBuilder.Event(ProtocolNames.PlayerChat, payload));
        }

        private void OnKillMe(object sender, GetDataHandlers.KillMeEventArgs e)
        {
            if (e.Player == null)
                return;

            var message = e.PlayerDeathReason != null
                ? e.PlayerDeathReason.GetDeathText(e.Player.Name).ToString()
                : e.Player.Name;

            var payload = new DeathPayload
            {
                Player = ToView(e.Player),
                DeathMessage = message,
            };
            _server.Broadcast(EnvelopeBuilder.Event(ProtocolNames.PlayerDeath, payload));
        }

        private static PlayerView ToView(TSPlayer player)
        {
            return new PlayerView
            {
                Name = player.Name,
                Index = player.Index,
                Group = player.Group?.Name,
                Account = player.Account?.Name,
                Uuid = player.UUID,
                Ip = player.IP,
            };
        }

        private static PlayerView ToViewFromGame(int index)
        {
            if (index < 0 || index >= Main.player.Length || Main.player[index] == null)
                return null;

            var gamePlayer = Main.player[index];
            return new PlayerView
            {
                Name = gamePlayer.name,
                Index = index,
            };
        }
    }
}
