# 事件与指令对应关系

本文档整理 `STS2-Link-YOKONEX` 当前支持的游戏事件与默认 `commandId` 映射关系。

## 默认事件映射

| 中文事件 | Event Type | 默认 `commandId` | 说明 |
| --- | --- | --- | --- |
| 进入房间 | `room.entered` | `room_entered` | 进入新房间时触发 |
| 战斗开始 | `combat.started` | `combat_start` | 进入战斗并完成初始化时触发 |
| 回合开始 | `turn.started` | `turn_start` | 玩家新回合开始时触发 |
| 战斗结束 | `combat.ended` | `combat_end` | 战斗结束时触发 |
| 玩家生命变化 | `player.hp_changed` | `player_hp_changed` | 玩家生命值变化时触发 |
| 玩家受伤 | `player.damaged` | `player_hurt` | 玩家受到伤害时触发 |
| 玩家回血 | `player.healed` | `player_heal` | 玩家恢复生命时触发 |
| 玩家能量变化 | `player.energy_changed` | `player_energy_changed` | 玩家能量变化时触发 |
| 玩家格挡变化 | `player.block_changed` | `player_block_changed` | 玩家获得或失去格挡时触发 |
| 玩家破甲 | `player.block_broken` | `player_block_break` | 玩家格挡被打破时触发 |
| 玩家格挡清空 | `player.block_cleared` | `player_block_clear` | 玩家格挡被清空时触发 |
| 玩家死亡 | `player.died` | `player_dead` | 玩家死亡时触发 |
| 卡牌升级 | `card.upgraded` | `card_upgraded` | 当前覆盖休息点升级与部分 forge 流程 |
| 购买道具 | `item.purchased` | `item_purchased` | 商店购买成功时触发 |
| 选择奖励 | `reward.selected` | `reward_selected` | 奖励被拿走后触发 |

## 阈值规则

当前内置两类可配置规则：

- `player.damaged`
  - 单次掉血 `>= threshold` 时，发送 `repeatCount` 次
- `player.block_changed`
  - 仅对掉甲场景生效，单次掉甲 `>= threshold` 时，发送 `repeatCount` 次

## 相关文件

- 默认映射定义：[EventCommandCatalog.cs](D:\STS2-Link-YOKONEX\src\STS2Bridge\Config\EventCommandCatalog.cs)
- 事件目录定义：[EventCatalog.cs](D:\STS2-Link-YOKONEX\src\STS2Bridge\Config\EventCatalog.cs)
- 规则模型：[CommandTriggerRule.cs](D:\STS2-Link-YOKONEX\src\STS2Bridge\Config\CommandTriggerRule.cs)
