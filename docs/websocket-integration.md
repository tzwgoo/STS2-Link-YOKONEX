# WebSocket 对接文档

本文档说明项目当前的两类 WebSocket 能力：

1. 本地桥接服务的事件流 WebSocket
2. 外部 IM 服务的 `sendCommand` 联动

## 1. 本地事件流 WebSocket

对应代码：

- [WsHub.cs](D:\STS2-Link-YOKONEX\src\STS2Bridge\Api\WebSocket\WsHub.cs)
- [LocalApiServer.cs](D:\STS2-Link-YOKONEX\src\STS2Bridge\Api\LocalApiServer.cs)
- [RuntimeApiHost.cs](D:\STS2-Link-YOKONEX\src\STS2Bridge\Api\RuntimeApiHost.cs)

连接地址：

```text
ws://127.0.0.1:15526/ws
```

鉴权头：

```text
X-STS2-Token: change-me
```

当前支持的事件：

- `player.damaged`
- `player.healed`
- `player.energy_changed`
- `player.block_broken`
- `player.died`
- `item.purchased`
- `card.upgraded`
- `reward.selected`

## 2. 外部 IM WebSocket 联动

对应代码：

- [WEBSOCKET_API.md](D:\STS2-Link-YOKONEX\docs\WEBSOCKET_API.md)
- [ExternalImWebSocketClient.cs](D:\STS2-Link-YOKONEX\src\STS2Bridge\Integration\ExternalImWebSocketClient.cs)
- [IMCommandBridgeService.cs](D:\STS2-Link-YOKONEX\src\STS2Bridge\Integration\IMCommandBridgeService.cs)

默认连接地址：

```text
ws://103.236.55.92:43001
```

默认映射：

- `player.damaged -> player_hurt`
- `player.healed -> player_heal`
- `player.block_broken -> player_block_break`
- `player.died -> player_dead`
- `card.upgraded -> card_upgraded`
- `item.purchased -> item_purchased`
- `reward.selected -> reward_selected`

## 3. 排查顺序

1. 确认游戏已加载 `STS2-Link-YOKONEX.dll`
2. 查看 [godot.log](C:\Users\hosgoo\AppData\Roaming\SlayTheSpire2\logs\godot.log)
3. 确认事件在设置页中处于启用状态
4. 确认 `UID/Token` 已填写并登录成功
5. 确认对应事件存在默认或自定义 `commandId`
