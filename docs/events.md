# 事件说明

当前项目保留并支持以下事件：

- `room.entered`
- `combat.started`
- `combat.ended`
- `turn.started`
- `player.hp_changed`
- `player.damaged`
- `player.healed`
- `player.energy_changed`
- `player.block_changed`
- `player.block_broken`
- `player.block_cleared`
- `player.died`
- `reward.opened`
- `reward.selected`
- `event.option_selected`
- `item.purchased`
- `card.upgraded`

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
