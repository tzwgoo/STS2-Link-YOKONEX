# STS2 Bridge MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 从零搭建一个符合需求文档的 STS2 本地桥接 Mod MVP，交付可测试的项目骨架、统一 JSON 协议、本地 HTTP API、基础事件/状态/动作框架、README 和 Python 示例客户端。

**Architecture:** 采用分层骨架实现：Mod 入口负责初始化，领域层封装配置、日志、事件总线、状态仓库与主线程调度，API 层通过 ASP.NET Core Minimal API 暴露 HTTP 接口，动作通过 `ActionRouter` 入队主线程执行，Hook 与真实游戏适配通过兼容层与占位接口隔离。由于当前仓库没有真实 STS2 依赖，本次先实现可编译、可测试、可扩展的 MVP 框架，并把游戏强耦合点约束在边界层。

**Tech Stack:** C# 13 / .NET 9、xUnit、ASP.NET Core Minimal API、System.Text.Json、HarmonyLib（先作为集成占位）、Python 示例客户端。

---

### Task 1: 建立解决方案与项目骨架

**Files:**
- Create: `STS2Bridge.sln`
- Create: `src/STS2Bridge/STS2Bridge.csproj`
- Create: `tests/STS2Bridge.Tests/STS2Bridge.Tests.csproj`
- Create: `Directory.Build.props`
- Create: `global.json`

- [ ] **Step 1: 创建解决方案与主项目目录**

建立 `src/STS2Bridge` 和 `tests/STS2Bridge.Tests`，准备 SDK 与统一编码设置。

- [ ] **Step 2: 创建测试项目并引用主项目**

使用 xUnit，为后续 TDD 留出独立测试入口。

- [ ] **Step 3: 添加最小可编译占位代码**

保证 `dotnet build` 至少能通过空骨架。

- [ ] **Step 4: 运行构建验证**

Run: `dotnet build STS2Bridge.sln`
Expected: BUILD SUCCEEDED

### Task 2: 配置、日志、事件总线与状态仓库

**Files:**
- Create: `src/STS2Bridge/Config/BridgeConfig.cs`
- Create: `src/STS2Bridge/Logging/ModLog.cs`
- Create: `src/STS2Bridge/Events/GameEvent.cs`
- Create: `src/STS2Bridge/Events/GameEventBus.cs`
- Create: `src/STS2Bridge/Events/EventTypes.cs`
- Create: `src/STS2Bridge/State/GameStateStore.cs`
- Create: `src/STS2Bridge/State/Dtos/StateSnapshotDto.cs`
- Test: `tests/STS2Bridge.Tests/Config/BridgeConfigTests.cs`
- Test: `tests/STS2Bridge.Tests/Events/GameEventBusTests.cs`
- Test: `tests/STS2Bridge.Tests/State/GameStateStoreTests.cs`

- [ ] **Step 1: 先写配置默认值测试**

覆盖默认绑定地址、端口、Token、事件白名单、动作白名单等行为。

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/STS2Bridge.Tests/STS2Bridge.Tests.csproj --filter BridgeConfigTests`
Expected: FAIL with missing type/member assertions

- [ ] **Step 3: 实现最小配置模型**

仅实现通过测试所需的属性与默认值。

- [ ] **Step 4: 为事件总线写测试**

验证发布、订阅、最近事件缓存、白名单过滤与容量限制。

- [ ] **Step 5: 运行测试确认失败**

Run: `dotnet test tests/STS2Bridge.Tests/STS2Bridge.Tests.csproj --filter GameEventBusTests`
Expected: FAIL

- [ ] **Step 6: 实现事件总线**

实现线程安全事件发布、最近事件缓存与简单过滤。

- [ ] **Step 7: 为状态仓库写测试**

验证初始快照、增量更新、版本递增、最近更新时间。

- [ ] **Step 8: 运行测试确认失败**

Run: `dotnet test tests/STS2Bridge.Tests/STS2Bridge.Tests.csproj --filter GameStateStoreTests`
Expected: FAIL

- [ ] **Step 9: 实现状态 DTO 与仓库**

采用线程安全快照更新模式，避免对外暴露可变对象。

- [ ] **Step 10: 运行全部领域测试**

Run: `dotnet test tests/STS2Bridge.Tests/STS2Bridge.Tests.csproj --filter "BridgeConfigTests|GameEventBusTests|GameStateStoreTests"`
Expected: PASS

### Task 3: 主线程调度与动作路由

**Files:**
- Create: `src/STS2Bridge/Threading/MainThreadDispatcher.cs`
- Create: `src/STS2Bridge/Actions/ActionRequest.cs`
- Create: `src/STS2Bridge/Actions/ActionResponse.cs`
- Create: `src/STS2Bridge/Actions/IActionExecutor.cs`
- Create: `src/STS2Bridge/Actions/ActionRouter.cs`
- Create: `src/STS2Bridge/Actions/Executors/EndTurnExecutor.cs`
- Create: `src/STS2Bridge/Actions/Executors/PlayCardExecutor.cs`
- Create: `src/STS2Bridge/Actions/Executors/ChooseRewardExecutor.cs`
- Create: `src/STS2Bridge/Actions/Executors/ChooseEventOptionExecutor.cs`
- Test: `tests/STS2Bridge.Tests/Threading/MainThreadDispatcherTests.cs`
- Test: `tests/STS2Bridge.Tests/Actions/ActionRouterTests.cs`

- [ ] **Step 1: 先写主线程调度测试**

验证排队、按顺序执行、执行异常隔离。

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/STS2Bridge.Tests/STS2Bridge.Tests.csproj --filter MainThreadDispatcherTests`
Expected: FAIL

- [ ] **Step 3: 实现最小调度器**

使用线程安全队列与错误回调。

- [ ] **Step 4: 为动作路由写测试**

覆盖未知动作、白名单拦截、排队返回、执行器分发、错误码输出、幂等请求去重。

- [ ] **Step 5: 运行测试确认失败**

Run: `dotnet test tests/STS2Bridge.Tests/STS2Bridge.Tests.csproj --filter ActionRouterTests`
Expected: FAIL

- [ ] **Step 6: 实现动作请求、响应与路由器**

统一校验动作名、请求 ID、允许列表，并把执行委托切回主线程。

- [ ] **Step 7: 实现首版四个执行器占位**

先返回符合协议的成功/失败结构，并为未来接真实 STS2 对象预留上下文接口。

- [ ] **Step 8: 运行动作与调度测试**

Run: `dotnet test tests/STS2Bridge.Tests/STS2Bridge.Tests.csproj --filter "MainThreadDispatcherTests|ActionRouterTests"`
Expected: PASS

### Task 4: 本地 HTTP API 与统一 JSON 协议

**Files:**
- Create: `src/STS2Bridge/Api/ApiJson.cs`
- Create: `src/STS2Bridge/Api/LocalApiServer.cs`
- Create: `src/STS2Bridge/Api/Http/ApiError.cs`
- Create: `src/STS2Bridge/Api/Http/ApiEnvelope.cs`
- Create: `src/STS2Bridge/Api/Http/ApiTokenValidator.cs`
- Create: `src/STS2Bridge/Api/Contracts/VersionResponse.cs`
- Create: `src/STS2Bridge/Api/Contracts/RecentEventsResponse.cs`
- Create: `src/STS2Bridge/Api/Contracts/ActionSchemaResponse.cs`
- Create: `src/STS2Bridge/Api/Contracts/EventSchemaResponse.cs`
- Test: `tests/STS2Bridge.Tests/Api/LocalApiServerTests.cs`

- [ ] **Step 1: 先写 API 测试**

覆盖 `/health`、`/version`、`/state`、`/events/recent`、`/action` 的返回结构、Token 鉴权和错误响应。

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/STS2Bridge.Tests/STS2Bridge.Tests.csproj --filter LocalApiServerTests`
Expected: FAIL

- [ ] **Step 3: 实现统一 JSON 序列化配置**

统一 camelCase、忽略 null、返回时间戳。

- [ ] **Step 4: 实现 Token 校验与错误封装**

保证 401/403/400/500 都返回统一 JSON。

- [ ] **Step 5: 实现 Minimal API 服务注册**

提供文档要求的 `/health`、`/version`、`/state`、`/events/recent`、`/action`、`/actions/schema`、`/events/schema`。

- [ ] **Step 6: 运行 API 测试**

Run: `dotnet test tests/STS2Bridge.Tests/STS2Bridge.Tests.csproj --filter LocalApiServerTests`
Expected: PASS

### Task 5: Mod 入口、Hook 占位与兼容边界

**Files:**
- Create: `src/STS2Bridge/ModEntry.cs`
- Create: `src/STS2Bridge/Hooks/CombatHooks.cs`
- Create: `src/STS2Bridge/Hooks/TurnHooks.cs`
- Create: `src/STS2Bridge/Hooks/CardHooks.cs`
- Create: `src/STS2Bridge/Hooks/RoomHooks.cs`
- Create: `src/STS2Bridge/Hooks/RewardHooks.cs`
- Create: `src/STS2Bridge/Compatibility/GameVersionDetector.cs`
- Create: `src/STS2Bridge/Compatibility/HookGuard.cs`
- Test: `tests/STS2Bridge.Tests/Compatibility/HookGuardTests.cs`

- [ ] **Step 1: 先写兼容层测试**

验证缺少真实游戏依赖时不会抛出致命异常，Hook 安装失败会被捕获并记录。

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/STS2Bridge.Tests/STS2Bridge.Tests.csproj --filter HookGuardTests`
Expected: FAIL

- [ ] **Step 3: 实现 ModEntry 与兼容层**

初始化配置、事件总线、状态仓库、动作路由、API 服务，并把 Hook 安装包在保护逻辑内。

- [ ] **Step 4: 实现首版 Hook 占位类**

不绑定真实方法，只保留分域结构和未来扩展接口。

- [ ] **Step 5: 运行兼容层测试**

Run: `dotnet test tests/STS2Bridge.Tests/STS2Bridge.Tests.csproj --filter HookGuardTests`
Expected: PASS

### Task 6: 文档、样例客户端与最终验证

**Files:**
- Create: `README.md`
- Create: `docs/protocol.md`
- Create: `docs/events.md`
- Create: `docs/actions.md`
- Create: `docs/dev-notes.md`
- Create: `test-client/python/demo_client.py`

- [ ] **Step 1: 编写 README**

覆盖构建、安装、运行、配置、接口示例、已实现范围与真实 STS2 集成说明。

- [ ] **Step 2: 编写协议文档**

把事件、动作、HTTP 返回结构从实现同步到文档。

- [ ] **Step 3: 编写 Python 示例客户端**

演示获取状态、请求动作、读取最近事件。

- [ ] **Step 4: 运行全部测试**

Run: `dotnet test STS2Bridge.sln`
Expected: PASS

- [ ] **Step 5: 运行整体构建**

Run: `dotnet build STS2Bridge.sln -c Release`
Expected: BUILD SUCCEEDED

- [ ] **Step 6: 自检实现与需求映射**

逐项核对文档中的 MVP 交付：目录结构、统一 JSON、失败错误码、README、Python demo、未来扩展边界。
