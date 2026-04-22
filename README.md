# STS2-Link-YOKONEX

`STS2-Link-YOKONEX` 是一个面向《Slay the Spire 2》的本地桥接 Mod。它负责把游戏内状态、事件和动作统一暴露为本地接口，并支持把指定游戏事件联动到外部 IM WebSocket 服务的 `sendCommand` 指令。

## 当前能力

### 本地 API

- `GET /health`
- `GET /version`
- `GET /state`
- `GET /events/recent`
- `GET /actions/schema`
- `GET /events/schema`
- `POST /action`
- `GET /ws`

除 `/health` 和 `/version` 外，其余接口默认要求请求头 `X-STS2-Token`。

### 已接入事件

- `player.damaged`
- `player.healed`
- `player.energy_changed`
- `player.block_broken`
- `player.died`
- `orb.lightning.passive_triggered`
- `orb.lightning.evoked`
- `orb.frost.passive_triggered`
- `orb.frost.evoked`
- `orb.dark.passive_triggered`
- `orb.dark.evoked`
- `orb.plasma.passive_triggered`
- `orb.plasma.evoked`
- `item.purchased`
- `card.upgraded`
- `reward.selected`

### 已接入动作

- `play_card`
- `end_turn`
- `choose_reward`
- `choose_event_option`
- `proceed`

## 游戏内功能

### 事件与 IM 联动设置页

项目会在游戏 `Settings` 页面注入 `STS2-Link-YOKONEX Events` 入口。

面板中包含：

- 事件开关：控制事件是否继续通过桥接层发出
- IM 联动：填写 `UID`、`Token`，显示服务器地址、连接状态，并提供登录 / 登出按钮
- 阈值规则：支持“玩家掉血 N 点 / 掉甲 N 点，触发 M 次”

默认配置文件路径：

```text
C:\Users\<你的用户名>\AppData\Roaming\SlayTheSpire2\mods\STS2-Link-YOKONEX\bridge-settings.json
```

### 快捷键

- `F8`
- 用于打开或关闭 `STS2-Link-YOKONEX Events` 面板

## 外部 IM 联动

默认事件映射：

- `player.damaged -> player_hurt`
- `player.healed -> player_heal`
- `player.block_broken -> player_block_break`
- `player.died -> player_dead`
- `orb.lightning.passive_triggered -> orb_lightning_passive_triggered`
- `orb.lightning.evoked -> orb_lightning_evoked`
- `orb.frost.passive_triggered -> orb_frost_passive_triggered`
- `orb.frost.evoked -> orb_frost_evoked`
- `orb.dark.passive_triggered -> orb_dark_passive_triggered`
- `orb.dark.evoked -> orb_dark_evoked`
- `orb.plasma.passive_triggered -> orb_plasma_passive_triggered`
- `orb.plasma.evoked -> orb_plasma_evoked`
- `card.upgraded -> card_upgraded`
- `item.purchased -> item_purchased`
- `reward.selected -> reward_selected`

默认服务地址：

```text
ws://103.236.55.92:43001
```

## 球体事件

当前球体事件已经拆分为 8 条独立事件：

- `orb.lightning.passive_triggered`
- `orb.lightning.evoked`
- `orb.frost.passive_triggered`
- `orb.frost.evoked`
- `orb.dark.passive_triggered`
- `orb.dark.evoked`
- `orb.plasma.passive_triggered`
- `orb.plasma.evoked`

当前支持球体：

- `lightning`
- `frost`
- `dark`
- `plasma`

事件 payload 统一包含：

- `orbType`
- `amountKind`
- `amount`
- `ownerId`
- `displayName`

其中：

- `lightning` 的 `amountKind` 为 `damage`
- `frost` 的 `amountKind` 为 `block`
- `dark` 的 `amountKind` 为 `damage`
- `plasma` 的 `amountKind` 为 `energy`

## 构建与测试

构建：

```powershell
dotnet build D:\STS2-Link-YOKONEX\src\STS2Bridge\STS2-Link-YOKONEX.csproj -c Release
```

测试：

```powershell
dotnet test D:\STS2-Link-YOKONEX\tests\STS2Bridge.Tests\STS2-Link-YOKONEX.Tests.csproj
```

安装到游戏：

```powershell
powershell -ExecutionPolicy Bypass -File D:\STS2-Link-YOKONEX\scripts\install-mod.ps1
```

## 重要文档

- [install-and-verify.md](/D:/STS2-Link-YOKONEX/docs/install-and-verify.md)
- [runtime-integration-notes.md](/D:/STS2-Link-YOKONEX/docs/runtime-integration-notes.md)
- [websocket-integration.md](/D:/STS2-Link-YOKONEX/docs/websocket-integration.md)
- [event-command-map.md](/D:/STS2-Link-YOKONEX/docs/event-command-map.md)
- [WEBSOCKET_API.md](/D:/STS2-Link-YOKONEX/docs/WEBSOCKET_API.md)
- [actions.md](/D:/STS2-Link-YOKONEX/docs/actions.md)
- [events.md](/D:/STS2-Link-YOKONEX/docs/events.md)
- [protocol.md](/D:/STS2-Link-YOKONEX/docs/protocol.md)
