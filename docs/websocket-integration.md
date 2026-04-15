# WebSocket 对接文档

本文档说明如何接入 `STS2Bridge` 的 WebSocket 事件流。

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

错误包格式与 HTTP 一致：

```json
{
  "success": false,
  "errorCode": "UNAUTHORIZED",
  "message": "Missing X-STS2-Token header."
}
```

## 4. 消息格式

JSON 使用 camelCase 序列化，配置见 [ApiJson.cs](D:\STS2-Link-YOKONEX\src\STS2Bridge\Api\ApiJson.cs)。

### 4.1 hello 消息

连接成功后，服务端会先发：

```json
{
  "kind": "hello",
  "timestamp": 1710000000000,
  "data": {
    "message": "sts2 bridge websocket ready"
  }
}
```

字段说明：

- `kind`: 固定为 `hello`
- `timestamp`: 服务器时间戳，单位毫秒
- `data.message`: 固定欢迎文本

### 4.2 event 消息

每个事件会广播成：

```json
{
  "kind": "event",
  "type": "combat.started",
  "timestamp": 1710000000000,
  "data": {
    "eventId": "evt-123",
    "runId": "run-1",
    "floor": 7,
    "roomType": "MonsterRoom",
    "payload": {
      "source": "combat_manager"
    }
  }
}
```

字段说明：

- `kind`: 固定为 `event`
- `type`: 事件类型
- `timestamp`: 事件生成时间，单位毫秒
- `data.eventId`: 事件唯一 ID
- `data.runId`: 当前 run 标识
- `data.floor`: 当前楼层
- `data.roomType`: 当前房间类型
- `data.payload`: 事件负载，不同事件结构不同

事件结构定义见 [GameEvent.cs](D:\STS2-Link-YOKONEX\src\STS2Bridge\Events\GameEvent.cs)。

## 5. 当前已支持事件

当前白名单定义见 [BridgeConfig.cs](D:\STS2-Link-YOKONEX\src\STS2Bridge\Config\BridgeConfig.cs)。

当前已经接入真实运行时并会实际发出的事件：

- `combat.started`
- `turn.started`
- `combat.ended`
- `player.hp_changed`
- `player.damaged`
- `player.healed`
- `player.block_changed`

其中 `player.block_changed` 当前有三种 `reason`：

- `gained`: 获得格挡
- `lost`: 格挡减少
- `cleared`: 格挡被清空

## 6. player 事件示例

### 6.1 玩家受伤

```json
{
  "kind": "event",
  "type": "player.damaged",
  "timestamp": 1710000000000,
  "data": {
    "eventId": "evt-a",
    "runId": "run-1",
    "floor": 10,
    "roomType": "Combat",
    "payload": {
      "playerId": "ironclad",
      "amount": 9,
      "currentHp": 61,
      "maxHp": 80,
      "block": 4
    }
  }
}
```

### 6.2 玩家回血

```json
{
  "kind": "event",
  "type": "player.healed",
  "timestamp": 1710000000000,
  "data": {
    "eventId": "evt-b",
    "runId": "run-1",
    "floor": 11,
    "roomType": "Rest",
    "payload": {
      "playerId": "silent",
      "amount": 12,
      "currentHp": 52,
      "maxHp": 80,
      "block": 0
    }
  }
}
```

### 6.3 玩家格挡变化

```json
{
  "kind": "event",
  "type": "player.block_changed",
  "timestamp": 1710000000000,
  "data": {
    "eventId": "evt-c",
    "runId": "run-1",
    "floor": 12,
    "roomType": "Combat",
    "payload": {
      "playerId": "watcher",
      "delta": -5,
      "block": 4,
      "currentHp": 33,
      "maxHp": 60,
      "reason": "lost"
    }
  }
}
```

说明：

- `delta > 0` 表示格挡增加
- `delta < 0` 表示格挡减少
- `delta = null` 目前只会出现在 `reason = "cleared"` 的场景

## 7. JavaScript 对接示例

浏览器原生 `WebSocket` 不能自定义请求头，所以如果你在浏览器里直连，当前这版协议并不方便直接接入。

更推荐：

- Node.js 客户端
- Python 客户端
- 你自己的桌面端/服务端程序

Node.js 示例，使用 `ws`：

```js
import WebSocket from "ws";

const ws = new WebSocket("ws://127.0.0.1:15526/ws", {
  headers: {
    "X-STS2-Token": "change-me"
  }
});

ws.on("open", () => {
  console.log("connected");
});

ws.on("message", (data) => {
  const message = JSON.parse(data.toString());
  console.log(message);
});

ws.on("close", () => {
  console.log("closed");
});

ws.on("error", (error) => {
  console.error(error);
});
```

## 8. Python 对接示例

现成示例见 [demo_client.py](D:\STS2-Link-YOKONEX\test-client\python\demo_client.py)。

最小示例：

```python
import websocket

TOKEN = "change-me"

def on_message(ws, message):
    print(message)

ws = websocket.WebSocketApp(
    "ws://127.0.0.1:15526/ws",
    header=[f"X-STS2-Token: {TOKEN}"],
    on_message=on_message,
)

ws.run_forever()
```

## 9. 补拉最近事件

WebSocket 断线后，如果你想补拉最近事件，可以调用：

```text
GET /events/recent?limit=50
```

请求头同样需要：

```text
X-STS2-Token: change-me
```

返回结构：

```json
{
  "success": true,
  "data": {
    "items": []
  }
}
```

这适合做：

- 断线重连后的最近事件补偿
- 调试时查看最近广播内容

## 10. 对接建议

建议客户端这样处理：

1. 建立 WebSocket 连接
2. 收到 `hello` 后标记连接成功
3. 只处理 `kind = "event"` 的消息
4. 按 `type` 分发到不同业务处理器
5. 断线后自动重连
6. 重连成功后调用 `/events/recent` 补拉最近事件

推荐按以下键做幂等处理：

- `data.eventId`

## 11. 已知限制

当前版本的已知限制：

- API 宿主还没有默认自动启动
- 浏览器原生 WebSocket 由于不能自定义 header，不适合直接连这版协议
- `player.block_changed` 已支持 `gained / lost / cleared`，但后续仍可以继续补更细的伤害细节

## 12. 快速排查

如果连不上，按这个顺序查：

1. 游戏是否已经加载 `STS2Bridge.dll`
2. API 宿主是否真的已启动
3. 本机 `127.0.0.1:15526` 是否在监听
4. `X-STS2-Token` 是否正确
5. 是否误用了浏览器原生 WebSocket

可以先用这些接口确认：

- `GET http://127.0.0.1:15526/health`
- `GET http://127.0.0.1:15526/events/schema`
## 13. 事件开关与设置页

当前版本支持在游戏内设置页里控制事件开关。

入口：

- 打开游戏 `Settings`
- 点击 `STS2Bridge Events`

行为：

- 勾选表示该事件继续通过桥接层发出
- 取消勾选表示该事件会被 `GameEventBus` 在发布前过滤掉
- 修改后立即生效，不需要重启游戏

注意：

- `events/schema` 返回的是协议支持的事件列表，不代表它们当前一定启用
- 实际是否会发出，还取决于本地设置页里的开关状态
