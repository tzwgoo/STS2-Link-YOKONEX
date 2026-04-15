# STS2-Link-YOKONEX

`STS2-Link-YOKONEX` 是一个面向《Slay the Spire 2》本地联动场景的 Mod 桥接项目。

它的目标是把游戏里的状态、事件和动作统一抽象成稳定的本地接口，方便外部程序通过 HTTP / WebSocket 读取游戏状态、监听事件，或向游戏发送动作请求。

当前仓库已经完成了真实 STS2 Mod 环境接入，并支持在游戏内运行、产出事件、打开设置页和切换事件开关。

## 当前能力

### API

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

- `room.entered`
- `combat.started`
- `turn.started`
- `combat.ended`
- `card.played`
- `player.hp_changed`
- `player.damaged`
- `player.healed`
- `player.energy_changed`
- `player.block_changed`
- `player.block_broken`
- `player.block_cleared`
- `player.died`
- `enemy.hp_changed`
- `enemy.damaged`
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

### 事件设置页

项目会在游戏 `Settings` 页面里注入 `STS2Bridge Events` 入口。

你可以在面板里逐项开启或关闭事件，修改后立即生效，并持久化到本地配置文件。

默认配置文件路径：

```text
C:\Users\<你的用户名>\AppData\Roaming\SlayTheSpire2\mods\STS2Bridge\bridge-settings.json
```

### 快捷键

- 默认快捷键：`F8`
- 作用：打开或关闭 `STS2-Link-YOKONEX Events` 面板

## 目录结构

```text
src/STS2Bridge
tests/STS2Bridge.Tests
docs/
scripts/
test-client/python
tools/Sts2MetadataInspector
```

## 开发环境

项目当前使用：

- `.NET 9`
- 游戏自带 `sts2.dll`
- 游戏自带 `GodotSharp.dll`
- 游戏自带 `0Harmony.dll`

游戏目录通过 [Directory.Build.props](D:\STS2-Link-YOKONEX\Directory.Build.props) 里的 `STS2GameDir` 配置。

当前本地联调使用的 STS2 游戏目录是：

```text
D:\Users\hosgoo\Downloads\Slay the Spire 2\Slay the Spire 2\data_sts2_windows_x86_64
```

## 构建与测试

### 构建

```powershell
dotnet build D:\STS2-Link-YOKONEX\src\STS2Bridge\STS2Bridge.csproj -c Release
```

构建后会整理出最小 Mod 包目录：

```text
D:\STS2-Link-YOKONEX\artifacts\mods\STS2Bridge
```

### 测试

```powershell
dotnet test D:\STS2-Link-YOKONEX\tests\STS2Bridge.Tests\STS2Bridge.Tests.csproj
```

## 安装到游戏

可以直接运行安装脚本：

```powershell
powershell -ExecutionPolicy Bypass -File D:\STS2-Link-YOKONEX\scripts\install-mod.ps1
```

当前脚本会把最新产物安装到：

```text
D:\Users\hosgoo\Downloads\Slay the Spire 2\Slay the Spire 2\mods\STS2Bridge
```

预期结构类似：

```text
<游戏目录>\mods\STS2Bridge\STS2Bridge.dll
<游戏目录>\mods\STS2Bridge\STS2Bridge.json
```

## 如何验证

### 1. 验证 Mod 是否加载

启动游戏后查看日志：

[godot.log](C:\Users\hosgoo\AppData\Roaming\SlayTheSpire2\logs\godot.log)

确认日志中存在类似内容：

- `Found mod manifest file`
- `Loading assembly DLL`
- `Calling initializer method of type STS2Bridge.ModEntry`
- `Finished mod initialization for 'STS2Bridge'`

### 2. 验证设置页与快捷键

- 打开游戏 `Settings`
- 找到 `STS2Bridge Events`
- 点击后确认面板能打开
- 在主菜单或游戏内按 `F8`
- 确认面板能打开或关闭

### 3. 验证事件流

推荐配合 Python 示例客户端：

[demo_client.py](D:\STS2-Link-YOKONEX\test-client\python\demo_client.py)

你也可以结合文档查看协议：

- [websocket-integration.md](D:\STS2-Link-YOKONEX\docs\websocket-integration.md)
- [protocol.md](D:\STS2-Link-YOKONEX\docs\protocol.md)

### 4. 验证事件开关是否即时生效

- 在设置页关闭某个事件，例如 `card.played`
- 回到游戏执行对应行为
- 确认不再收到该事件
- 再重新开启它，确认事件恢复

## 重要文档

- [install-and-verify.md](D:\STS2-Link-YOKONEX\docs\install-and-verify.md)
- [runtime-integration-notes.md](D:\STS2-Link-YOKONEX\docs\runtime-integration-notes.md)
- [websocket-integration.md](D:\STS2-Link-YOKONEX\docs\websocket-integration.md)
- [actions.md](D:\STS2-Link-YOKONEX\docs\actions.md)
- [events.md](D:\STS2-Link-YOKONEX\docs\events.md)
- [protocol.md](D:\STS2-Link-YOKONEX\docs\protocol.md)

## 当前边界

- `card.upgraded` 目前稳定覆盖 `rest_site_smith` 和 `forge`，还没有做到所有升级来源全覆盖
- API 宿主、动作路由和事件桥接已可用，但仍在持续补更细的运行时字段
- 项目以本地真实游戏目录联调为主，切换机器时需要同步更新 `STS2GameDir`
