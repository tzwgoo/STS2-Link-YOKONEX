# 协议说明

## HTTP

### 统一响应

```json
{
  "success": true,
  "data": {}
}
```

```json
{
  "success": false,
  "errorCode": "UNAUTHORIZED",
  "message": "Missing X-STS2-Token header."
}
```

## WebSocket

连接地址：

```text
ws://127.0.0.1:15526/ws
```

请求头：

```text
X-STS2-Token: change-me
```

### hello

```json
{
  "kind": "hello",
  "timestamp": 1710000000,
  "data": {
    "message": "sts2 bridge websocket ready"
  }
}
```

### event

```json
{
  "kind": "event",
  "type": "combat.started",
  "timestamp": 1710000000,
  "data": {
    "eventId": "evt-1",
    "runId": "run-1",
    "floor": 1,
    "roomType": "MonsterRoom",
    "payload": {}
  }
}
```
