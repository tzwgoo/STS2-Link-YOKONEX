# IM WebSocket 指令联动设计

**日期：** 2026-04-16  
**项目：** STS2-Link-YOKONEX

## 目标

在现有 STS2 游戏事件桥接基础上，接入 `docs/WEBSOCKET_API.md` 描述的外部 WebSocket IM 服务。

当游戏内事件触发时：

1. 根据配置映射查找对应 `commandId`
2. 向外部 IM WebSocket 服务发送 `sendCommand`
3. 由外部服务将指令投递到指定用户的 IM 会话

同时，在游戏内 Mod 设置页面提供 `uid` 与 `token` 输入框和登录/登出操作，让用户无需修改配置文件即可完成 IM 登录。

## 约束与范围

### 本次实现范围

- 新增外部 WebSocket 客户端，连接到 `ws://103.236.55.92:43001`
- 支持发送 `login`、`logout`、`ping`、`sendCommand`
- 订阅现有 `GameEventBus`
- 新增事件到 `commandId` 的可配置映射
- 在现有游戏内设置面板增加：
  - `UID` 输入框
  - `Token` 输入框
  - 连接状态显示
  - 登录按钮
  - 登出按钮
  - 外部服务地址展示
- 将 `uid`、`token`、自动登录开关、事件映射持久化到本地设置文件

### 本次不做

- 不做事件映射的游戏内可编辑表单，首版只在 UI 中展示映射结果
- 不做复杂的重连策略配置面板
- 不做外部 IM 服务统计页
- 不做响应耗时数据库统计的本地展示
- 不修改现有本地 HTTP/WebSocket 协议

## 推荐实现方案

采用“配置映射 + 外部客户端服务 + 设置页输入”三层结构。

### 方案核心

1. `GameEventBus` 保持不变，继续负责统一发布游戏事件
2. 新增 `IMCommandBridgeService`
   - 订阅 `GameEventBus`
   - 判断事件是否启用
   - 查找该事件是否存在 `commandId` 映射
   - 若外部 IM 已登录，则发送 `sendCommand`
3. 新增 `ExternalImWebSocketClient`
   - 维护外部 WebSocket 连接
   - 处理 `connected`、`loginResult`、`logoutResult`、`commandResult`、`error`、`heartbeat`、`pong`
   - 对外暴露 `LoginAsync`、`LogoutAsync`、`SendCommandAsync`
4. 设置页扩展为“事件开关 + IM 联动设置”的组合面板

## 默认事件映射

首版内置默认事件到指令映射如下：

| 游戏事件 | commandId |
|---|---|
| `player.damaged` | `player_hurt` |
| `player.healed` | `player_heal` |
| `player.block_broken` | `player_block_break` |
| `player.block_cleared` | `player_block_clear` |
| `player.died` | `player_dead` |
| `combat.started` | `combat_start` |
| `combat.ended` | `combat_end` |
| `turn.started` | `turn_start` |
| `card.played` | `card_played` |
| `card.upgraded` | `card_upgraded` |
| `item.purchased` | `item_purchased` |
| `reward.selected` | `reward_selected` |
| `room.entered` | `room_entered` |

映射需支持后续从配置文件覆盖。

## 配置设计

在现有 `BridgeSettings` 中新增以下字段：

- `ImWebSocketUrl`
- `ImUid`
- `ImToken`
- `ImAutoLogin`
- `EventCommandMap`

### 默认值

- `ImWebSocketUrl = "ws://103.236.55.92:43001"`
- `ImUid = ""`
- `ImToken = ""`
- `ImAutoLogin = false`
- `EventCommandMap = 默认映射表`

### 持久化位置

继续使用现有设置文件：

`%AppData%\SlayTheSpire2\mods\STS2-Link-YOKONEX\bridge-settings.json`

## 外部客户端协议设计

### 连接目标

`ws://103.236.55.92:43001`

### 发送消息

#### 登录

```json
{
  "type": "login",
  "uid": "123456",
  "token": "your_token_here"
}
```

#### 登出

```json
{
  "type": "logout",
  "userId": "123456"
}
```

#### 发送指令

```json
{
  "type": "sendCommand",
  "userId": "123456",
  "commandId": "player_hurt"
}
```

### 连接状态模型

需要跟踪以下本地状态：

- `Disconnected`
- `Connecting`
- `Connected`
- `LoggingIn`
- `LoggedIn`
- `LoginFailed`
- `Error`

并保存：

- `CurrentUid`
- `CurrentUserId`
- `LastError`
- `LastServerMessage`

## 事件联动行为

### 触发条件

只有同时满足以下条件才发送 `sendCommand`：

1. 本地游戏事件已发布
2. 该事件仍处于启用状态
3. 存在对应 `commandId` 映射
4. 外部 IM 已登录成功
5. 能解析出有效的 `userId`

### userId 规则

优先使用外部 IM 登录成功响应中返回的 `userId`。

不直接使用原始 `uid` 作为 `sendCommand.userId`，因为协议要求该字段使用纯数字 `userId`。

### 失败处理

若发送失败：

- 记录日志
- 更新状态文本
- 不影响本地事件总线运行
- 不阻断后续事件发布

## UI 设计

在现有 `STS2-Link-YOKONEX Events` 面板顶部新增 `IM 联动` 区块。

### 顶部区域包含

- 只读标签：`服务地址`
- 文本框：`UID`
- 文本框：`Token`
- 状态文本：`连接状态`
- 按钮：`登录 IM`
- 按钮：`登出 IM`

### 下方区域

继续保留原有事件开关列表。

首版可在事件行中追加映射展示文本，例如：

- `card.played -> card_played`
- `player.died -> player_dead`

但不提供编辑功能。

## 架构拆分

建议新增以下文件：

### 配置

- `src/STS2Bridge/Config/ImBridgeSettings.cs`
  - 如不单独拆分，则扩展 `BridgeSettings`
- `src/STS2Bridge/Config/EventCommandCatalog.cs`
  - 默认映射定义

### 外部 WebSocket

- `src/STS2Bridge/Integration/ExternalImWebSocketClient.cs`
  - 与外部 IM 服务通信
- `src/STS2Bridge/Integration/ExternalImMessageModels.cs`
  - 协议消息模型
- `src/STS2Bridge/Integration/IMCommandBridgeService.cs`
  - 订阅事件并发送指令

### UI

- 修改 `src/STS2Bridge/Ui/EventSettingsPopup.cs`
- 修改 `src/STS2Bridge/Ui/EventSettingsUiController.cs`

### 启动接入

- 修改 `src/STS2Bridge/ModEntry.cs`
  - 初始化客户端
  - 连接事件桥接服务
  - 根据设置决定是否自动登录

## 测试策略

必须覆盖以下测试：

### 配置测试

- 默认设置包含外部 IM 连接配置
- 默认事件映射正确写入
- 设置文件读写可保留 `uid/token/map`

### 客户端状态测试

- 解析 `loginResult` 成功消息
- 解析 `loginResult` 失败消息
- 解析 `logoutResult`
- 解析 `error`
- 状态切换正确

### 联动服务测试

- 事件命中映射时发送正确 `sendCommand`
- 事件关闭时不发送
- 未登录时不发送
- 无映射时不发送
- `userId` 缺失时不发送

### UI 测试

- 设置页可以显示 `UID/Token`
- 点击登录会调用登录逻辑
- 点击登出会调用登出逻辑
- 状态文本会反映当前连接状态

## 风险点

### 风险 1：游戏线程与网络线程切换

外部 WebSocket 接收消息可能不在主线程，需要避免在网络回调中直接操作 Godot UI。

解决方式：

- 网络客户端只更新普通状态对象
- UI 刷新通过现有控制器在安全位置读取状态

### 风险 2：发送过于频繁

某些事件如 `card.played` 或未来更高频事件可能导致短时间大量 `sendCommand`。

首版先不做复杂限流，但保留后续增加节流器的接口空间。

### 风险 3：登录状态不一致

WebSocket 已连接不等于 IM 已登录，必须区分：

- 底层连接状态
- 业务登录状态

## 成功标准

满足以下条件视为完成：

1. 用户可以在游戏内输入 `uid` 和 `token`
2. 用户可以在游戏内点击登录并看到状态变化
3. 登录成功后，触发指定游戏事件会自动发送 `sendCommand`
4. 关闭某个事件开关后，不再发送对应指令
5. 重启游戏后 `uid/token` 和映射配置仍然保留
6. 测试通过，构建通过，安装脚本可正常复制新产物
