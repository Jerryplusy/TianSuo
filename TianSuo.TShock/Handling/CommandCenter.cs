using Newtonsoft.Json.Linq;
using TShockAPI;
using TianSuo.Tool.Networking;
using TianSuo.Tool.Protocol;

namespace TianSuo.Handling
{
    public sealed class CommandCenter
    {
        private readonly RconRunner _runner = new RconRunner();

        public void Handle(TianSuoServer server, Session session, string payload)
        {
            JObject root;
            try
            {
                root = JObject.Parse(payload);
            }
            catch
            {
                return;
            }

            var type = root["type"]?.ToString();
            if (type != ProtocolNames.ApiType)
                return;

            var name = root["name"]?.ToString();
            var data = root["data"] as JObject;

            switch (name)
            {
                case ProtocolNames.Broadcast:
                    HandleBroadcast(server, session, data);
                    break;
                case ProtocolNames.PrivateMessage:
                    HandlePrivateMessage(server, session, data);
                    break;
                case ProtocolNames.Title:
                    HandleTitle(server, session, data);
                    break;
                case ProtocolNames.ActionBar:
                    HandleActionBar(server, session, data);
                    break;
                case ProtocolNames.RconCommand:
                    HandleRcon(server, session, data);
                    break;
            }
        }

        private void HandleBroadcast(TianSuoServer server, Session session, JObject data)
        {
            var message = data?["message"]?.ToString();
            if (string.IsNullOrWhiteSpace(message))
            {
                Reply(server, session, false, "message is empty");
                return;
            }

            TShock.Utils.Broadcast(message, 127, 255, 212);
            Reply(server, session, true, "ok");
        }

        private void HandlePrivateMessage(TianSuoServer server, Session session, JObject data)
        {
            var target = data?["target"]?.ToString();
            var message = data?["message"]?.ToString();
            if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(message))
            {
                Reply(server, session, false, "target or message is empty");
                return;
            }

            var matches = TSPlayer.FindByNameOrID(target);
            if (matches.Count == 0)
            {
                Reply(server, session, false, "player not found");
                return;
            }

            foreach (var player in matches)
                player.SendMessage(message, 127, 255, 212);

            Reply(server, session, true, "ok");
        }

        private void HandleTitle(TianSuoServer server, Session session, JObject data)
        {
            var title = data?["title"]?.ToString();
            var subtitle = data?["subtitle"]?.ToString();

            if (string.IsNullOrWhiteSpace(title))
            {
                Reply(server, session, false, "title is empty");
                return;
            }

            TShock.Utils.Broadcast("====== " + title + " ======", 255, 215, 0);
            if (!string.IsNullOrWhiteSpace(subtitle))
                TShock.Utils.Broadcast(subtitle, 255, 250, 170);

            Reply(server, session, true, "ok (degraded: shown as chat banner)");
        }

        private void HandleActionBar(TianSuoServer server, Session session, JObject data)
        {
            var message = data?["message"]?.ToString();
            if (string.IsNullOrWhiteSpace(message))
            {
                Reply(server, session, false, "message is empty");
                return;
            }

            TShock.Utils.Broadcast("[ActionBar] " + message, 255, 215, 0);
            Reply(server, session, true, "ok (degraded: shown as chat banner)");
        }

        private void HandleRcon(TianSuoServer server, Session session, JObject data)
        {
            var command = data?["command"]?.ToString();
            if (string.IsNullOrWhiteSpace(command))
            {
                Reply(server, session, false, "command is empty");
                return;
            }

            var output = _runner.Run(command);
            Reply(server, session, true, output);
        }

        private void Reply(TianSuoServer server, Session session, bool success, string message)
        {
            var result = new ApiResult { Success = success, Message = message };
            var _ = session.SendAsync(EnvelopeBuilder.Api(ProtocolNames.ApiResponse, result));
        }
    }
}
