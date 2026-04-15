# 动作说明

## 已开放动作

- `play_card`
- `end_turn`
- `choose_reward`
- `choose_event_option`

## 请求结构

```json
{
  "requestId": "req-1",
  "action": "play_card",
  "params": {
    "cardInstanceId": "c_001",
    "targetInstanceId": "e_001"
  }
}
```

## 响应结构

```json
{
  "success": true,
  "data": {
    "requestId": "req-1",
    "success": true,
    "message": "queued"
  }
}
```

## 当前已实现的校验

`play_card` 会校验：

- 当前 screen 是否为 `combat`
- `cardInstanceId` 是否存在
- 卡是否在手牌中
- 当前能量是否足够
- 需要目标的卡是否提供 `targetInstanceId`

## 当前保留的错误码

- `ACTION_NOT_ALLOWED`
- `DUPLICATE_REQUEST`
- `EXECUTOR_NOT_FOUND`
- `ACTION_EXECUTION_ERROR`
- `INVALID_STATE`
- `CARD_NOT_FOUND`
- `NOT_ENOUGH_ENERGY`
- `TARGET_REQUIRED`
