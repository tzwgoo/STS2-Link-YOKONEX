# 事件说明

当前项目支持以下事件：

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
- `orb.passive_triggered`
- `orb.evoked`
- `reward.opened`
- `reward.selected`
- `event.option_selected`
- `item.purchased`
- `card.upgraded`

## 通用事件结构

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

## 球体事件结构

### `orb.passive_triggered`

```json
{
  "eventId": "evt-2",
  "type": "orb.passive_triggered",
  "timestamp": 1710000001,
  "runId": "run-1",
  "floor": 8,
  "roomType": "Combat",
  "payload": {
    "orbType": "lightning",
    "amountKind": "damage",
    "amount": 3,
    "ownerId": "7",
    "displayName": "lightning.passive"
  }
}
```

### `orb.evoked`

```json
{
  "eventId": "evt-3",
  "type": "orb.evoked",
  "timestamp": 1710000002,
  "runId": "run-1",
  "floor": 8,
  "roomType": "Combat",
  "payload": {
    "orbType": "plasma",
    "amountKind": "energy",
    "amount": 2,
    "ownerId": "7",
    "displayName": "plasma.evoked"
  }
}
```

## 球体类型与含义

当前统一事件支持以下球体：

| 球体 | `orbType` | `amountKind` | 说明 |
| --- | --- | --- | --- |
| 闪电球 | `lightning` | `damage` | 伤害类球体 |
| 冰霜球 | `frost` | `block` | 格挡类球体 |
| 黑暗球 | `dark` | `damage` | 伤害类球体 |
| 等离子球 | `plasma` | `energy` | 能量类球体 |

## 说明

- `ownerId` 会尽量从球体拥有者对象中解析，解析不到时为 `unknown`
- `amount` 直接取游戏运行时对象上的 `PassiveVal` 或 `EvokeVal`
- 当前球体事件不会改写状态快照，只通过事件总线广播
