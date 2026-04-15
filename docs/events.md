# 事件说明

当前首版骨架保留以下事件名与扩展位：

- `room.entered`
- `combat.started`
- `combat.ended`
- `turn.started`
- `card.played`
- `reward.opened`
- `reward.selected`
- `event.option_selected`

事件结构：

```json
{
  "eventId": "evt-1",
  "type": "combat.started",
  "timestamp": 1710000000,
  "runId": "run-1",
  "floor": 8,
  "roomType": "MonsterRoom",
  "payload": {}
}
```

当前仓库中的 Hook 仍为占位结构，真实事件采集需要在接入 STS2 运行时后补全。
