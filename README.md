# TianSuo 天梭

Terraria TShock 服务端插件，通过 WebSocket 将玩家事件实时推送给外部程序，并接收外部消息在游戏内广播

## 支持环境

- Terraria 1.4.4.9（及 TShock 5.2.x 所支持的更早版本）
- TShock 5.2.x，基于 .NET 6

## 构建

```bash
dotnet build TianSuo.sln -c Release
```

产物在：
- `TianSuo.TShock/bin/Release/net6.0/TianSuo.TShock.dll`
- `TianSuo.TShock/bin/Release/net6.0/TianSuo.Tool.dll`

## 安装

1. 将 `TianSuo.TShock.dll` 与 `TianSuo.Tool.dll` 复制到服务器的 `ServerPlugins` 文件夹
2. 启动服务器，插件会在 `tshock/` 下生成 `tiansuo.json` 配置文件
3. 修改配置后重启服务器生效

## 配置

```json
{
  "Enabled": true,
  "ServerName": "TianSuo",
  "AccessToken": "",
  "WebSocketHost": "127.0.0.1",
  "WebSocketPort": 8080,
  "SubscribePlayerJoin": true,
  "SubscribePlayerQuit": true,
  "SubscribePlayerChat": true,
  "SubscribePlayerDeath": true
}
```

- `ServerName`：客户端连接时必须携带 `x-self-name` 请求头，值需与之一致。
- `AccessToken`：非空时客户端还需携带 `Authorization: Bearer <token>` 请求头。

## 协议

所有消息为 UTF-8 文本帧，JSON 信封格式：

```json
{ "type": "event", "name": "player_join", "data": { "player": { "name": "Steve", "index": 3 } } }
{ "type": "api",   "name": "broadcast",   "data": { "message": "Hello" } }
```

API 响应为 `type=api`、`name=api_response`：

```json
{ "type": "api", "name": "api_response", "data": { "success": true, "message": "ok" } }
```

## 鸣谢

- [QueQiao](https://github.com/17TheWord/QueQiao)：最喜欢的MC互通插件
- [TShock](https://github.com/TShock/TShock) 与 [Pryaxis](https://github.com/Pryaxis)：Terraria 服务端框架与插件 API
- [Re-Logic](https://www.terraria.org/)：Terraria
