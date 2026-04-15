# WebSocket 对接文档

本文档说明如何接入 `STS2-Link-YOKONEX` 的 WebSocket 事件流。

当前实现对应代码：

- [WsHub.cs](D:\STS2-Link-YOKONEX\src\STS2Bridge\Api\WebSocket\WsHub.cs)
- [LocalApiServer.cs](D:\STS2-Link-YOKONEX\src\STS2Bridge\Api\LocalApiServer.cs)
- [RuntimeApiHost.cs](D:\STS2-Link-YOKONEX\src\STS2Bridge\Api\RuntimeApiHost.cs)
- [GameEvent.cs](D:\STS2-Link-YOKONEX\src\STS2Bridge\Events\GameEvent.cs)

## 1. 连接信息

默认连接地址：

```text
ws://127.0.0.1:15526/ws
```

鉴权方式：

```text
X-STS2-Token: change-me
```

默认配置来源：

- `BindHost = 127.0.0.1`
- `Port = 15526`
- `AuthToken = change-me`

配置定义见 [BridgeConfig.cs](D:\STS2-Link-YOKONEX\src\STS2Bridge\Config\BridgeConfig.cs)。

## 2. 前置条件

要收到 WebSocket 事件，前提是桥接服务已经启动。

当前代码现状：

- WebSocket 服务能力已经实现
- 但当前版本还没有在 [ModEntry.cs](D:\STS2-Link-YOKONEX\src\STS2Bridge\ModEntry.cs) 的 `Initialize()` 中自动调用 `StartApiAsync()`

这意味着：

- 如果你已经在别处手动启动了 `RuntimeApiHost`，可以直接按本文档连接
- 如果还没有启动 API 宿主，仅安装 Mod 并不会自动打开 `15526` 端口

## 3. 握手流程

客户端连接到 `/ws` 后：

1. 服务端校验 `X-STS2-Token`
2. 校验通过后升级为 WebSocket
3. 服务端立即下发一条 `hello` 消息
4. 后续每产生一个游戏事件，就广播一条 `event` 消息

如果鉴权失败：

- 缺少 Token：HTTP `401`
- Token 错误：HTTP `403`

## 4. 当前已支持事件

当前已经接入真实运行时并会实际发出的事件：

- `combat.started`
- `turn.started`
- `combat.ended`
- `player.hp_changed`
- `player.damaged`
- `player.healed`
- `player.energy_changed`
- `player.block_changed`
- `player.block_broken`
- `player.block_cleared`
- `player.died`
- `enemy.hp_changed`
- `enemy.damaged`
- `card.played`
- `item.purchased`
- `card.upgraded`
- `reward.selected`

## 5. 快速排查

如果连不上，按这个顺序查：

1. 游戏是否已经加载 `STS2-Link-YOKONEX.dll`
2. API 宿主是否真的已启动
3. 本机 `127.0.0.1:15526` 是否在监听
4. `X-STS2-Token` 是否正确

## 6. 事件开关与设置页

当前版本支持在游戏内设置页里控制事件开关。

入口：

- 打开游戏 `Settings`
- 点击 `STS2-Link-YOKONEX Events`

行为：

- 勾选表示该事件继续通过桥接层发出
- 取消勾选表示该事件会被 `GameEventBus` 在发布前过滤掉
- 修改后立即生效，不需要重启游戏
