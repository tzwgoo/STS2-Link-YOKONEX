# 事件与指令对应关系

本文档整理 `STS2-Link-YOKONEX` 当前支持的游戏事件与默认 `commandId` 映射关系。

## 默认事件映射

| 中文事件 | Event Type | 默认 `commandId` | 说明 |
| --- | --- | --- | --- |
| 玩家受伤 | `player.damaged` | `player_hurt` | 玩家受到伤害时触发 |
| 玩家回血 | `player.healed` | `player_heal` | 玩家恢复生命时触发 |
| 玩家能量变化 | `player.energy_changed` | `player_energy_changed` | 玩家能量变化时触发 |
| 玩家破甲 | `player.block_broken` | `player_block_break` | 玩家格挡被打破时触发 |
| 玩家死亡 | `player.died` | `player_dead` | 玩家死亡时触发 |
| 闪电球被动触发 | `orb.lightning.passive_triggered` | `orb_lightning_passive_triggered` | 闪电球执行被动效果时触发 |
| 闪电球激发 | `orb.lightning.evoked` | `orb_lightning_evoked` | 闪电球被激发时触发 |
| 冰霜球被动触发 | `orb.frost.passive_triggered` | `orb_frost_passive_triggered` | 冰霜球执行被动效果时触发 |
| 冰霜球激发 | `orb.frost.evoked` | `orb_frost_evoked` | 冰霜球被激发时触发 |
| 黑暗球被动触发 | `orb.dark.passive_triggered` | `orb_dark_passive_triggered` | 黑暗球执行被动效果时触发 |
| 黑暗球激发 | `orb.dark.evoked` | `orb_dark_evoked` | 黑暗球被激发时触发 |
| 等离子球被动触发 | `orb.plasma.passive_triggered` | `orb_plasma_passive_triggered` | 等离子球执行被动效果时触发 |
| 等离子球激发 | `orb.plasma.evoked` | `orb_plasma_evoked` | 等离子球被激发时触发 |
| 卡牌升级 | `card.upgraded` | `card_upgraded` | 当前覆盖休息点升级与部分 forge 流程 |
| 购买道具 | `item.purchased` | `item_purchased` | 商店购买成功时触发 |
| 选择奖励 | `reward.selected` | `reward_selected` | 奖励被拿走后触发 |

## 球体事件补充

当前球体事件已经按球种拆分为 8 条独立事件，因此可以直接在事件列表里分别开关，也可以分别映射不同 `commandId`。

球体 payload 仍然会保留：

- `orbType`
- `amountKind`
- `amount`
- `ownerId`
- `displayName`

## 阈值规则

当前内置两类可配置规则：

- `player.damaged`
  - 单次掉血 `>= threshold` 时，发送 `repeatCount` 次
- `player.block_broken`
  - 仅对掉甲场景生效，单次掉甲 `>= threshold` 时，发送 `repeatCount` 次

## 相关文件

- 默认映射定义：[EventCommandCatalog.cs](/D:/STS2-Link-YOKONEX/src/STS2Bridge/Config/EventCommandCatalog.cs)
- 事件目录定义：[EventCatalog.cs](/D:/STS2-Link-YOKONEX/src/STS2Bridge/Config/EventCatalog.cs)
- 规则模型：[CommandTriggerRule.cs](/D:/STS2-Link-YOKONEX/src/STS2Bridge/Config/CommandTriggerRule.cs)
