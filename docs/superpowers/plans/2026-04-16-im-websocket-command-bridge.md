# IM WebSocket 指令联动实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让 STS2-Link-YOKONEX 在游戏事件触发后，按配置映射自动调用外部 IM WebSocket 的 `sendCommand`，并在游戏内设置页提供 `UID/Token` 登录能力。

**Architecture:** 扩展现有设置模型以保存 IM 登录信息和事件映射；新增一个外部 IM WebSocket 客户端与事件桥接服务，订阅 `GameEventBus` 后按映射触发外部 `sendCommand`；复用现有设置弹层，在顶部增加 IM 联动区块用于输入、登录、登出和显示状态。

**Tech Stack:** .NET 9、GodotSharp、Harmony、xUnit、System.Net.WebSockets、System.Text.Json

---

### Task 1: 扩展设置模型和默认映射

**Files:**
- Modify: `D:\STS2-Link-YOKONEX\src\STS2Bridge\Config\BridgeSettings.cs`
- Create: `D:\STS2-Link-YOKONEX\src\STS2Bridge\Config\EventCommandCatalog.cs`
- Modify: `D:\STS2-Link-YOKONEX\src\STS2Bridge\Config\BridgeSettingsStore.cs`
- Test: `D:\STS2-Link-YOKONEX\tests\STS2Bridge.Tests\Config\BridgeSettingsStoreTests.cs`

- [ ] **Step 1: 写 failing tests，覆盖默认映射和 IM 设置字段**

补充测试：
- `CreateDefault` 包含默认 `ImWebSocketUrl`
- `CreateDefault` 包含默认事件映射
- `Save/Load` 能 roundtrip `ImUid`、`ImToken`、`EventCommandMap`

- [ ] **Step 2: 运行配置测试并确认失败**

Run: `dotnet test D:\STS2-Link-YOKONEX\tests\STS2Bridge.Tests\STS2-Link-YOKONEX.Tests.csproj --filter BridgeSettingsStoreTests`
Expected: FAIL，提示 `BridgeSettings` 缺少新字段或默认值不匹配

- [ ] **Step 3: 实现最小配置扩展**

在 `BridgeSettings` 中新增：
- `ImWebSocketUrl`
- `ImUid`
- `ImToken`
- `ImAutoLogin`
- `EventCommandMap`

新增默认映射目录 `EventCommandCatalog`，由 `CreateDefault()` 注入。

- [ ] **Step 4: 运行配置测试并确认通过**

Run: `dotnet test D:\STS2-Link-YOKONEX\tests\STS2Bridge.Tests\STS2-Link-YOKONEX.Tests.csproj --filter BridgeSettingsStoreTests`
Expected: PASS


### Task 2: 添加外部 IM WebSocket 客户端

**Files:**
- Create: `D:\STS2-Link-YOKONEX\src\STS2Bridge\Integration\ExternalImConnectionState.cs`
- Create: `D:\STS2-Link-YOKONEX\src\STS2Bridge\Integration\ExternalImStatus.cs`
- Create: `D:\STS2-Link-YOKONEX\src\STS2Bridge\Integration\ExternalImWebSocketClient.cs`
- Test: `D:\STS2-Link-YOKONEX\tests\STS2Bridge.Tests\Integration\ExternalImWebSocketClientTests.cs`

- [ ] **Step 1: 写 failing tests，覆盖消息解析与状态切换**

覆盖：
- `loginResult(success=true)` 更新为 `LoggedIn`
- `loginResult(success=false)` 更新为 `LoginFailed`
- `logoutResult` 更新为 `Connected` 或 `Disconnected`
- `error` 更新错误信息

- [ ] **Step 2: 运行客户端测试并确认失败**

Run: `dotnet test D:\STS2-Link-YOKONEX\tests\STS2Bridge.Tests\STS2-Link-YOKONEX.Tests.csproj --filter ExternalImWebSocketClientTests`
Expected: FAIL，因为客户端尚不存在

- [ ] **Step 3: 实现最小客户端**

实现：
- 连接外部 `ws://103.236.55.92:43001`
- `LoginAsync`
- `LogoutAsync`
- `SendCommandAsync`
- 文本消息解析
- 线程安全状态对象

- [ ] **Step 4: 运行客户端测试并确认通过**

Run: `dotnet test D:\STS2-Link-YOKONEX\tests\STS2Bridge.Tests\STS2-Link-YOKONEX.Tests.csproj --filter ExternalImWebSocketClientTests`
Expected: PASS


### Task 3: 添加事件到指令的桥接服务

**Files:**
- Create: `D:\STS2-Link-YOKONEX\src\STS2Bridge\Integration\IMCommandBridgeService.cs`
- Modify: `D:\STS2-Link-YOKONEX\src\STS2Bridge\ModEntry.cs`
- Test: `D:\STS2-Link-YOKONEX\tests\STS2Bridge.Tests\Integration\IMCommandBridgeServiceTests.cs`

- [ ] **Step 1: 写 failing tests，覆盖事件命中映射时发指令**

覆盖：
- 命中映射时发送正确 `commandId`
- 事件关闭时不发送
- 未登录时不发送
- 缺少映射时不发送

- [ ] **Step 2: 运行桥接测试并确认失败**

Run: `dotnet test D:\STS2-Link-YOKONEX\tests\STS2Bridge.Tests\STS2-Link-YOKONEX.Tests.csproj --filter IMCommandBridgeServiceTests`
Expected: FAIL，因为桥接服务尚不存在

- [ ] **Step 3: 实现最小桥接服务**

桥接服务订阅 `GameEventBus`，读取 `BridgeSettings.EventCommandMap`，在客户端已登录时调用 `SendCommandAsync`。

- [ ] **Step 4: 在 `ModEntry` 中接入客户端和桥接服务**

初始化：
- 设置存储
- 外部 IM 客户端
- 事件桥接服务
- 保存设置入口

- [ ] **Step 5: 运行桥接测试并确认通过**

Run: `dotnet test D:\STS2-Link-YOKONEX\tests\STS2Bridge.Tests\STS2-Link-YOKONEX.Tests.csproj --filter IMCommandBridgeServiceTests`
Expected: PASS


### Task 4: 扩展游戏内设置面板

**Files:**
- Modify: `D:\STS2-Link-YOKONEX\src\STS2Bridge\Ui\EventSettingsPopup.cs`
- Modify: `D:\STS2-Link-YOKONEX\src\STS2Bridge\Ui\EventSettingsUiController.cs`
- Modify: `D:\STS2-Link-YOKONEX\src\STS2Bridge\ModEntry.cs`
- Test: `D:\STS2-Link-YOKONEX\tests\STS2Bridge.Tests\Ui\EventSettingsPopupTests.cs`

- [ ] **Step 1: 写 failing tests，覆盖 UI 节点构建与登录动作回调**

覆盖：
- 有 `UID` 输入框
- 有 `Token` 输入框
- 有状态标签
- 有登录/登出按钮

- [ ] **Step 2: 运行 UI 测试并确认失败**

Run: `dotnet test D:\STS2-Link-YOKONEX\tests\STS2Bridge.Tests\STS2-Link-YOKONEX.Tests.csproj --filter EventSettingsPopupTests`
Expected: FAIL，因为节点尚不存在

- [ ] **Step 3: 实现 UI 扩展**

在面板顶部新增 `IM 联动` 区块：
- 服务器地址
- UID 输入框
- Token 输入框
- 状态文本
- 登录按钮
- 登出按钮

按钮调用 `ModEntry` 暴露的登录/登出入口，输入变更时保存设置。

- [ ] **Step 4: 运行 UI 测试并确认通过**

Run: `dotnet test D:\STS2-Link-YOKONEX\tests\STS2Bridge.Tests\STS2-Link-YOKONEX.Tests.csproj --filter EventSettingsPopupTests`
Expected: PASS


### Task 5: 全量验证与安装

**Files:**
- Modify: `D:\STS2-Link-YOKONEX\README.md`
- Modify: `D:\STS2-Link-YOKONEX\docs\websocket-integration.md`
- Verify: `D:\STS2-Link-YOKONEX\scripts\install-mod.ps1`

- [ ] **Step 1: 更新文档**

补充：
- 外部 IM WebSocket 联动说明
- UID/Token 设置说明
- 默认事件映射说明

- [ ] **Step 2: 运行全量测试**

Run: `dotnet test D:\STS2-Link-YOKONEX\tests\STS2Bridge.Tests\STS2-Link-YOKONEX.Tests.csproj`
Expected: PASS

- [ ] **Step 3: 运行 Release 构建**

Run: `dotnet build D:\STS2-Link-YOKONEX\src\STS2Bridge\STS2-Link-YOKONEX.csproj -c Release`
Expected: PASS

- [ ] **Step 4: 安装到游戏目录**

Run: `powershell -ExecutionPolicy Bypass -File D:\STS2-Link-YOKONEX\scripts\install-mod.ps1`
Expected: 输出 `Installed STS2-Link-YOKONEX ...`

- [ ] **Step 5: 记录验证方式**

确认用户可按以下步骤验证：
- 游戏内打开面板填写 `UID/Token`
- 点击登录
- 触发如 `player.damaged`
- 外部服务收到 `sendCommand`
