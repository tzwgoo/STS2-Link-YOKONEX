# STS2 真实运行时接入笔记

基于本机游戏目录：

```text
D:\Users\hosgoo\Downloads\Slay the Spire 2\Slay the Spire 2\data_sts2_windows_x86_64
```

已确认：

- 游戏运行时为 `net9.0`
- 游戏内置 `0Harmony.dll 2.4.2.0`
- 游戏内置 `GodotSharp.dll 4.5.1`
- 游戏内置真实 Mod 体系，不只是裸程序集

## 已确认的 Mod 相关类型

命名空间：

```text
MegaCrit.Sts2.Core.Modding
```

关键类型：

- `ModInitializerAttribute`
- `ModManager`
- `ModManifest`
- `ModSettings`
- `ModManagerFileIo`

### ModManifest 已探测字段

- `id`
- `name`
- `author`
- `description`
- `version`
- `hasPck`
- `hasDll`
- `dependencies`
- `affectsGameplay`

## 已确认的 Hook 相关类型

命名空间：

```text
MegaCrit.Sts2.Core.Hooks
MegaCrit.Sts2.Core.Models
```

### `MegaCrit.Sts2.Core.Hooks.Hook`

已确认方法名：

- `BeforeCombatStart`
- `AfterCombatEnd`
- `AfterCardPlayed`
- `AfterRoomEntered`
- `AfterSideTurnStart`

### `MegaCrit.Sts2.Core.Models.AbstractModel`

已确认可覆写方法：

- `AfterRoomEntered`
- `BeforeCombatStart`
- `AfterCombatEnd`
- `AfterCardPlayed`
- `AfterSideTurnStart`

这说明首版事件采集未必必须从 Harmony Patch 开始，优先走模型 Hook 体系可能更稳。

## 已确认的动作/战斗相关类型

### `MegaCrit.Sts2.Core.GameActions.PlayCardAction`

已确认成员：

- `CardModelId`
- `TargetId`
- `Player`
- `ExecuteAction()`

### `MegaCrit.Sts2.Core.Commands.PlayerCmd`

已确认成员：

- `EndTurn`
- `GainEnergy`
- `LoseEnergy`
- `SetEnergy`
- `GainGold`
- `LoseGold`

### `MegaCrit.Sts2.Core.Combat.CombatManager`

已确认成员：

- `Instance`
- `CombatSetUp`
- `CombatEnded`
- `TurnStarted`
- `TurnEnded`
- `PlayerEndedTurn`
- `AboutToSwitchToEnemyTurn`
- `IsPlayPhase`
- `PlayerActionsDisabled`

## 当前结论

下一阶段优先建议：

1. 先验证最小 Mod 包能否被游戏识别
2. 再围绕 `CombatManager` 事件和 `AbstractModel` Hook 做首批真实事件桥接
3. 动作执行先验证 `PlayerCmd.EndTurn`，再推进 `PlayCardAction`
