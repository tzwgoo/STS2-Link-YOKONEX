# 《杀戮尖塔 2》方案 B 技术方案  
## Mod + 本地服务端事件驱动联动系统（交付 Codex 开发版）

> 文档目标：交付给 Codex / 开发同事，按本文档直接落地一个 **Slay the Spire 2（STS2）本地事件桥接与控制系统**。  
> 方案类型：**方案 B = Mod + 本地服务端 + 外部客户端/Agent**。  
> 文档版本：v1.0  
> 建议开发语言：**C# (.NET 9)**  
> 建议 Mod 形态：**Godot 4.5.1 .NET Mod + Harmony Patch + 本地 HTTP/WebSocket 服务**

---

## 1. 背景与目标

### 1.1 业务背景
需要为《杀戮尖塔 2》开发一个 Mod，使其能够：

1. 监听游戏内关键事件；
2. 将事件与状态通过本地接口暴露给外部程序；
3. 外部程序基于规则、脚本、AI Agent 或工作流系统进行决策；
4. 外部程序再通过本地接口调用 Mod，触发游戏内动作；
5. 整个链路仅在本机 `localhost` 上运行，不默认暴露到公网。

### 1.2 目标能力
本项目最终应支持以下核心能力：

- **事件采集**：战斗开始、战斗结束、回合开始、打出卡牌、进入房间、打开奖励、事件选项选择等；
- **状态快照**：玩家、敌人、手牌、能量、遗物、药水、地图、奖励、房间上下文；
- **动作执行**：出牌、结束回合、选择事件选项、选择奖励、浏览商店、购买、进入下一节点等；
- **外部联动**：支持本地 Python / Node / 桌面程序 / MCP Server / AI Agent 接入；
- **可配置**：端口、Token、日志级别、事件开关、动作白名单可配置；
- **可扩展**：后续可接入规则引擎、AI 控制器、可视化调试面板。

### 1.3 非目标
本阶段不追求：

- 公网远程控制；
- 云端多用户服务；
- 反作弊绕过；
- 全量 Hook 所有游戏内部方法；
- 第一版支持所有角色、所有房间、所有联机边缘分支。

---

## 2. 技术基线与约束

### 2.1 已确认的社区基线
当前 STS2 社区可行的 Mod 开发基线如下：

- 社区示例工程已验证 **Godot 4.5.1 .NET** + **.NET 9** + `sts2.dll` 引用方式可用；
- Mod 入口可使用 `[ModInitializer(...)]`；
- 社区实践明确建议通过 **反射或 Harmony** 修改运行时行为；
- `mods/` 目录加载 `json + dll + pck` 是常见部署方式；
- 社区已有项目将游戏状态与动作通过本地 API 暴露给外部程序；
- 社区已有基础依赖库 **BaseLib-StS2**；
- 社区已有通用配置框架 **ModConfig-STS2**，可在游戏设置页中注入 Mod 配置界面。

### 2.2 设计约束
- 必须兼容 STS2 当前 EA（抢先体验）阶段版本变动；
- 游戏更新后，部分 Hook 可能失效，架构上要允许快速修复；
- 只监听本地环回地址 `127.0.0.1 / localhost`；
- 动作调用必须有鉴权或至少本地 Token 校验；
- 事件与动作必须解耦，避免把业务写死在 Patch 中；
- 必须预留调试日志与回放能力。

---

## 3. 总体方案

### 3.1 架构总览

```text
┌───────────────────────────────────────────────────────────────┐
│                        Slay the Spire 2                      │
│                                                               │
│  ┌──────────────┐    ┌──────────────┐    ┌─────────────────┐  │
│  │ Harmony Hook │ -> │ Event Mapper │ -> │  In-Process Bus │  │
│  └──────────────┘    └──────────────┘    └─────────────────┘  │
│                                   │                           │
│                                   v                           │
│                          ┌─────────────────┐                  │
│                          │ Local API Host  │                  │
│                          │ HTTP + WS       │                  │
│                          └─────────────────┘                  │
│                                   │                           │
└───────────────────────────────────┼───────────────────────────┘
                                    │ localhost
                                    v
┌───────────────────────────────────────────────────────────────┐
│                        External Client / Agent                │
│                                                               │
│  ┌──────────────┐    ┌──────────────┐    ┌─────────────────┐  │
│  │ Event Client │ -> │ Rule Engine  │ -> │ Action Caller   │  │
│  └──────────────┘    └──────────────┘    └─────────────────┘  │
│                                                               │
└───────────────────────────────────────────────────────────────┘
```

### 3.2 核心思想
核心设计原则如下：

1. **Patch 只采集，不做复杂决策**；
2. 统一映射为标准 `GameEvent`；
3. 通过进程内 `EventBus` 分发；
4. 由 `HTTP/WebSocket Server` 对外暴露事件与状态；
5. 外部程序通过 `Action API` 请求动作；
6. `ActionRouter` 在游戏主线程安全地执行动作；
7. 所有协议统一 JSON 序列化；
8. 为版本升级、字段变动、事件缺失预留兼容层。

---

## 4. 模块设计

---

### 4.1 模块一：ModEntry（Mod 启动入口）

#### 职责
- Mod 初始化；
- 读取配置；
- 安装 Harmony 补丁；
- 启动本地 API 服务；
- 初始化日志、事件总线、状态快照服务。

#### 设计要求
- 启动失败不能直接导致游戏崩溃；
- 任一模块初始化失败时，允许降级禁用 API，而不是拖垮整个 Mod；
- 输出清晰启动日志。

#### 参考伪代码
```csharp
[ModInitializer("Initialize")]
public static class ModEntry
{
    public static void Initialize()
    {
        ModLog.Info("STS2Bridge initializing...");

        BridgeConfig.Load();
        GameEventBus.Init();
        GameStateStore.Init();
        ActionRouter.Init();

        var harmony = new Harmony("com.company.sts2bridge");
        harmony.PatchAll();

        LocalApiServer.Start(
            BridgeConfig.BindHost,
            BridgeConfig.Port,
            BridgeConfig.Token
        );

        ModLog.Info("STS2Bridge initialized.");
    }
}
```

---

### 4.2 模块二：Harmony Hook 层

#### 职责
- 对关键游戏方法打补丁；
- 捕获游戏状态变化节点；
- 将原始上下文转换为统一事件；
- 不在此层编写复杂业务逻辑。

#### 设计要求
- 优先使用 `Postfix` 获取完整状态；
- 仅在确有必要时使用 `Prefix` 或 `Transpiler`；
- 每个补丁类只负责单一事件域；
- 必须记录 Hook 失败日志。

#### 建议分组
- `CombatHooks`
- `TurnHooks`
- `CardHooks`
- `RoomHooks`
- `RewardHooks`
- `MapHooks`
- `EventRoomHooks`
- `ShopHooks`
- `PlayerStateHooks`

---

### 4.3 模块三：Event Mapper（事件映射层）

#### 职责
将游戏内部对象映射为稳定的领域事件对象，避免外部协议直接依赖游戏内部类结构。

#### 统一事件模型
```csharp
public sealed class GameEvent
{
    public string EventId { get; set; } = default!;
    public string Type { get; set; } = default!;
    public long Timestamp { get; set; }
    public string RunId { get; set; } = default!;
    public int Floor { get; set; }
    public string? RoomType { get; set; }
    public object Payload { get; set; } = default!;
}
```

#### 事件命名规范
采用 **领域.动作** 命名：

- `run.started`
- `run.ended`
- `room.entered`
- `combat.started`
- `combat.ended`
- `turn.started`
- `turn.ended`
- `card.played`
- `card.drawn`
- `player.damaged`
- `reward.opened`
- `reward.selected`
- `event.option_selected`
- `shop.opened`
- `shop.purchased`

#### 设计要求
- 事件名称一旦发布尽量保持稳定；
- 事件字段允许扩展，不允许随意删核心字段；
- 每个事件必须携带 `eventId`、`timestamp`、`runId`；
- 对于外部消费方敏感的字段，优先扁平化。

---

### 4.4 模块四：GameEventBus（进程内事件总线）

#### 职责
- 统一事件分发；
- 提供订阅/退订机制；
- 支持本地服务端广播；
- 可选写入事件日志或 ring buffer。

#### 参考实现
```csharp
public static class GameEventBus
{
    public static event Action<GameEvent>? OnEvent;

    public static void Init() { }

    public static void Publish(GameEvent evt)
    {
        OnEvent?.Invoke(evt);
    }
}
```

#### 扩展建议
- 增加最近 N 条事件缓存；
- 为 WS 新连接提供最近事件回放；
- 增加事件节流或去抖机制。

---

### 4.5 模块五：GameStateStore（状态快照仓库）

#### 职责
维护最新游戏状态快照，供 `/state` 接口返回。

#### 为什么需要
外部系统不能只靠事件流工作。  
事件是“发生了什么”，但 AI 或规则系统还需要知道“当前是什么状态”。

#### 推荐快照结构
```json
{
  "runId": "run-20260414-001",
  "seed": "abc123",
  "act": 1,
  "floor": 8,
  "roomType": "MonsterRoom",
  "player": {
    "hp": 52,
    "maxHp": 70,
    "block": 8,
    "energy": 3
  },
  "hand": [],
  "drawPile": [],
  "discardPile": [],
  "enemies": [],
  "relics": [],
  "potions": [],
  "screen": {
    "name": "combat",
    "subState": "player_turn"
  }
}
```

#### 设计要求
- 快照更新优先增量维护，避免每次全量深拷贝；
- 对外返回时可序列化为只读 DTO；
- 必须带 `stateVersion` 或 `updatedAt` 方便客户端判断是否新状态。

---

### 4.6 模块六：LocalApiServer（本地服务端）

#### 职责
- 提供 HTTP 查询与动作调用接口；
- 提供 WebSocket 实时事件推送；
- 实施 localhost 绑定、Token 鉴权、异常处理、日志。

#### 技术建议
优先使用轻量方案：

- HTTP：`HttpListener` 或 ASP.NET Core Minimal API（二选一）
- WS：`HttpListener + AcceptWebSocketAsync` 或 ASP.NET Core WebSocket Middleware

#### 推荐
如果追求代码最简和部署方便，建议优先：

- **HTTP + WebSocket 同进程**
- **Minimal API**
- **监听 `127.0.0.1:{port}`**

#### 设计要求
- 启动时检测端口占用；
- 启动失败要输出明确日志；
- 必须有健康检查接口；
- WebSocket 广播要处理断线重连；
- 所有异常不能向上传播至游戏主循环。

---

### 4.7 模块七：ActionRouter（动作路由器）

#### 职责
- 接收外部 Action 请求；
- 解析动作类型；
- 校验当前游戏状态是否允许执行；
- 调度到具体执行器；
- 保证在合适线程/时机执行游戏操作。

#### 统一动作请求模型
```csharp
public sealed class ActionRequest
{
    public string RequestId { get; set; } = default!;
    public string Action { get; set; } = default!;
    public Dictionary<string, object>? Params { get; set; }
}
```

#### 动作命名规范
- `end_turn`
- `play_card`
- `choose_reward`
- `choose_event_option`
- `select_map_node`
- `buy_shop_item`
- `proceed`
- `use_potion`
- `discard_potion`

#### 动作执行结果
```csharp
public sealed class ActionResponse
{
    public string RequestId { get; set; } = default!;
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }
}
```

#### 设计要求
- 先校验再执行；
- 动作结果必须明确成功/失败；
- 游戏状态不合法时返回可读错误码；
- 必须预留幂等性机制，防止客户端重复点击。

---

### 4.8 模块八：线程与主循环调度层

#### 风险点
大多数游戏对象操作对线程敏感。  
HTTP/WS 回调线程通常不是游戏主线程，不能直接改游戏状态。

#### 解决方案
实现一个主线程任务队列：

```csharp
public static class MainThreadDispatcher
{
    private static readonly ConcurrentQueue<Action> _queue = new();

    public static void Enqueue(Action action) => _queue.Enqueue(action);

    public static void Drain()
    {
        while (_queue.TryDequeue(out var action))
        {
            action();
        }
    }
}
```

在可安全插入的 Godot `_Process`、游戏 Tick、或适合的 update hook 中调用 `Drain()`。

#### 规则
- 网络线程只接收请求，不直接操作游戏对象；
- 所有真正的游戏动作都入主线程队列；
- 动作执行后更新 `GameStateStore` 并回写结果。

---

### 4.9 模块九：配置管理

#### 配置项建议
- `enabled`
- `bindHost`
- `port`
- `authToken`
- `enableWebSocket`
- `enableHttp`
- `enableEventReplay`
- `enableDebugLog`
- `allowedActions`
- `eventWhitelist`
- `eventBlacklist`

#### 配置来源优先级
1. 配置文件
2. ModConfig UI
3. 默认值

#### 建议
如果集成 `ModConfig-STS2`，则把以下内容做进游戏内设置页：
- 端口
- Token
- 日志级别
- 是否启用 Instant Mode 兼容提示
- 是否输出详细事件日志

---

## 5. 对外接口设计

---

### 5.1 HTTP 接口总览

| 方法 | 路径 | 说明 |
|---|---|---|
| GET | `/health` | 健康检查 |
| GET | `/version` | 返回 Mod 版本、协议版本、游戏版本 |
| GET | `/state` | 获取当前状态快照 |
| GET | `/events/recent` | 获取最近事件 |
| POST | `/action` | 执行动作 |
| GET | `/actions/schema` | 获取动作定义与参数说明 |
| GET | `/events/schema` | 获取事件定义与字段说明 |

---

### 5.2 鉴权方案
#### Header 方案
```http
X-STS2-Token: <token>
```

#### 规则
- 未带 Token 返回 401；
- Token 错误返回 403；
- 本地 debug 可允许关闭鉴权，但默认开启。

---

### 5.3 健康检查接口

#### 请求
```http
GET /health
```

#### 响应
```json
{
  "ok": true,
  "serverTime": 1710000000,
  "modLoaded": true,
  "apiReady": true
}
```

---

### 5.4 版本接口

#### 响应示例
```json
{
  "modVersion": "0.1.0",
  "protocolVersion": "1.0",
  "gameVersion": "0.99.1",
  "features": [
    "http",
    "websocket",
    "state_snapshot",
    "actions"
  ]
}
```

---

### 5.5 状态接口

#### 请求
```http
GET /state
```

#### 响应示例
```json
{
  "stateVersion": 1024,
  "updatedAt": 1710000000,
  "runId": "run-20260414-001",
  "act": 1,
  "floor": 8,
  "screen": {
    "name": "combat",
    "subState": "player_turn"
  },
  "player": {
    "hp": 52,
    "maxHp": 70,
    "block": 8,
    "energy": 3,
    "gold": 88
  },
  "hand": [
    {
      "instanceId": "c_001",
      "cardId": "strike",
      "name": "Strike",
      "cost": 1,
      "playable": true,
      "targetRequired": true
    }
  ],
  "enemies": [
    {
      "instanceId": "e_001",
      "name": "Louse",
      "hp": 12,
      "maxHp": 12,
      "block": 0,
      "alive": true
    }
  ]
}
```

---

### 5.6 最近事件接口

#### 请求
```http
GET /events/recent?limit=50
```

#### 响应示例
```json
{
  "items": [
    {
      "eventId": "evt_001",
      "type": "combat.started",
      "timestamp": 1710000000,
      "floor": 8,
      "payload": {
        "enemyCount": 2
      }
    }
  ]
}
```

---

### 5.7 Action 接口

#### 请求
```http
POST /action
Content-Type: application/json
X-STS2-Token: dev-token

{
  "requestId": "req_001",
  "action": "play_card",
  "params": {
    "cardInstanceId": "c_001",
    "targetInstanceId": "e_001"
  }
}
```

#### 响应
```json
{
  "requestId": "req_001",
  "success": true,
  "message": "queued"
}
```

#### 错误响应示例
```json
{
  "requestId": "req_001",
  "success": false,
  "errorCode": "INVALID_STATE",
  "message": "Current screen is not combat."
}
```

---

### 5.8 WebSocket 设计

#### 连接地址
```text
ws://127.0.0.1:15526/ws
```

#### 握手鉴权
可通过 query 或 header，推荐 header：

```http
X-STS2-Token: dev-token
```

#### 推送消息类型
统一 envelope：

```json
{
  "kind": "event",
  "type": "combat.started",
  "timestamp": 1710000000,
  "data": {
    "floor": 8,
    "enemyCount": 2
  }
}
```

其他类型：
- `kind = event`
- `kind = state`
- `kind = action_result`
- `kind = error`
- `kind = hello`

#### 建议能力
- 新连接后先发 `hello`
- 可配置是否推送全量状态
- 可选支持 `subscribe` 消息，只订阅部分事件

---

## 6. 事件模型设计

---

### 6.1 第一阶段必须支持的事件
建议第一版只做高价值事件，避免全量 Hook 失控。

| 事件名 | 说明 | 优先级 |
|---|---|---|
| `run.started` | 开局 | P1 |
| `run.ended` | 结束 | P1 |
| `room.entered` | 进入房间 | P1 |
| `combat.started` | 战斗开始 | P1 |
| `combat.ended` | 战斗结束 | P1 |
| `turn.started` | 回合开始 | P1 |
| `turn.ended` | 回合结束 | P1 |
| `card.played` | 打出卡牌 | P1 |
| `player.damaged` | 玩家受伤 | P2 |
| `reward.opened` | 奖励界面打开 | P1 |
| `reward.selected` | 选择奖励 | P1 |
| `event.option_selected` | 事件选项选择 | P1 |
| `shop.opened` | 商店打开 | P2 |
| `shop.purchased` | 购买商品 | P2 |

---

### 6.2 事件 payload 规范
#### `combat.started`
```json
{
  "encounterId": "enc_123",
  "enemyCount": 2,
  "enemyNames": ["Louse", "Cultist"]
}
```

#### `card.played`
```json
{
  "cardInstanceId": "c_001",
  "cardId": "strike",
  "name": "Strike",
  "cost": 1,
  "targetInstanceId": "e_001"
}
```

#### `event.option_selected`
```json
{
  "eventId": "mysterious_sphere",
  "optionIndex": 1,
  "optionText": "Take the relic"
}
```

---

## 7. 动作模型设计

---

### 7.1 第一阶段必须支持的动作
| 动作 | 说明 | 优先级 |
|---|---|---|
| `end_turn` | 结束回合 | P1 |
| `play_card` | 打牌 | P1 |
| `choose_reward` | 选择奖励 | P1 |
| `choose_event_option` | 选择事件项 | P1 |
| `proceed` | 继续/确认 | P1 |
| `select_map_node` | 选择地图节点 | P2 |
| `buy_shop_item` | 商店购买 | P2 |
| `use_potion` | 使用药水 | P2 |
| `discard_potion` | 丢弃药水 | P2 |

### 7.2 动作校验规则
以 `play_card` 为例：

校验内容：
- 当前 screen 必须是 combat；
- 当前必须是玩家操作阶段；
- 卡牌实例存在于手牌；
- 当前能量足够；
- 若需目标，则目标存在且可选；
- 卡牌未被标记为不可打出。

不满足时返回：
- `INVALID_STATE`
- `CARD_NOT_FOUND`
- `NOT_ENOUGH_ENERGY`
- `TARGET_REQUIRED`
- `INVALID_TARGET`

---

## 8. 目录结构建议

```text
STS2Bridge/
├─ STS2Bridge.csproj
├─ STS2Bridge.json
├─ STS2Bridge.pck
├─ assets/
│  └─ mod_image.png
├─ src/
│  ├─ ModEntry.cs
│  ├─ Config/
│  │  ├─ BridgeConfig.cs
│  │  └─ ConfigLoader.cs
│  ├─ Logging/
│  │  └─ ModLog.cs
│  ├─ Events/
│  │  ├─ GameEvent.cs
│  │  ├─ GameEventBus.cs
│  │  ├─ EventTypes.cs
│  │  └─ EventMapper.cs
│  ├─ State/
│  │  ├─ GameStateStore.cs
│  │  ├─ Dtos/
│  │  │  ├─ StateSnapshotDto.cs
│  │  │  ├─ PlayerDto.cs
│  │  │  ├─ CardDto.cs
│  │  │  └─ EnemyDto.cs
│  ├─ Hooks/
│  │  ├─ CombatHooks.cs
│  │  ├─ TurnHooks.cs
│  │  ├─ CardHooks.cs
│  │  ├─ RewardHooks.cs
│  │  ├─ RoomHooks.cs
│  │  ├─ EventRoomHooks.cs
│  │  └─ ShopHooks.cs
│  ├─ Actions/
│  │  ├─ ActionRequest.cs
│  │  ├─ ActionResponse.cs
│  │  ├─ ActionRouter.cs
│  │  ├─ IActionExecutor.cs
│  │  └─ Executors/
│  │     ├─ PlayCardExecutor.cs
│  │     ├─ EndTurnExecutor.cs
│  │     ├─ ChooseRewardExecutor.cs
│  │     └─ ChooseEventOptionExecutor.cs
│  ├─ Api/
│  │  ├─ LocalApiServer.cs
│  │  ├─ HttpHandlers/
│  │  │  ├─ HealthHandler.cs
│  │  │  ├─ VersionHandler.cs
│  │  │  ├─ StateHandler.cs
│  │  │  ├─ ActionHandler.cs
│  │  │  └─ SchemaHandler.cs
│  │  └─ WebSocket/
│  │     ├─ WsHub.cs
│  │     └─ WsClientSession.cs
│  ├─ Threading/
│  │  └─ MainThreadDispatcher.cs
│  └─ Compatibility/
│     ├─ GameVersionDetector.cs
│     └─ HookGuard.cs
├─ docs/
│  ├─ protocol.md
│  ├─ events.md
│  ├─ actions.md
│  └─ dev-notes.md
└─ test-client/
   ├─ python/
   │  └─ demo_client.py
   └─ node/
      └─ demo_client.js
```

---

## 9. 开发阶段划分

---

### Phase 1：基础骨架
#### 目标
- 项目可编译；
- Mod 能加载；
- Harmony 能工作；
- 本地 HTTP 服务能启动。

#### 交付物
- Mod 启动日志；
- `/health`
- `/version`
- 空事件总线；
- 基础配置文件。

#### 验收标准
- 进入游戏后 Mod 启动成功；
- `GET /health` 返回 200；
- API 只绑定 `localhost`。

---

### Phase 2：事件桥接 MVP
#### 目标
支持最小可用事件流。

#### 交付物
- `room.entered`
- `combat.started`
- `combat.ended`
- `turn.started`
- `card.played`
- `/events/recent`
- WebSocket 推送

#### 验收标准
- 战斗开始时能收到实时事件；
- 出牌时事件 payload 正确；
- 最近事件接口可返回最近 N 条记录。

---

### Phase 3：状态快照与动作
#### 目标
实现基本闭环：看状态 → 发动作 → 游戏响应。

#### 交付物
- `/state`
- `play_card`
- `end_turn`
- `choose_reward`
- `choose_event_option`
- 主线程调度器

#### 验收标准
- 外部脚本可读取当前手牌与敌人；
- 外部脚本可成功打牌并结束回合；
- 非法动作能返回明确错误码。

---

### Phase 4：稳定性与可配置
#### 目标
可长期调试、可升级、可容错。

#### 交付物
- Token 鉴权
- 配置文件持久化
- Hook 失败日志
- 事件白名单/黑名单
- 动作白名单
- 异常保护与性能日志

#### 验收标准
- 无 Token 无法调用动作；
- 关闭某事件后不再推送；
- API 异常不会让游戏崩溃。

---

### Phase 5：高级能力（可选）
#### 可选交付
- AI Agent 示例客户端
- 规则引擎 DSL
- 事件录制 / 回放
- UI 调试面板
- 多人模式兼容层
- MCP 协议适配层

---

## 10. Codex 开发任务拆分

---

### 10.1 任务清单（建议按 issue / story 切分）

#### T1 项目初始化
- 初始化 Godot 4.5.1 .NET 项目
- 引入 `sts2.dll`
- 创建 Mod manifest
- 编译出 dll + pck
- 可被游戏识别

#### T2 基础框架
- `ModEntry`
- `ModLog`
- `BridgeConfig`
- `GameEventBus`
- `GameStateStore`
- `MainThreadDispatcher`

#### T3 HTTP 服务
- `LocalApiServer`
- `GET /health`
- `GET /version`
- Token 中间件
- 基础异常处理

#### T4 WebSocket 服务
- `/ws`
- 客户端会话管理
- 事件广播
- 断线清理

#### T5 第一批 Hook
- 战斗开始
- 战斗结束
- 回合开始
- 打牌
- 进入房间
- 奖励打开

#### T6 事件协议
- 统一 envelope
- DTO 序列化
- `/events/schema`
- `/events/recent`

#### T7 状态快照
- 玩家状态
- 手牌
- 敌人
- 房间
- 当前 screen
- `/state`

#### T8 动作执行器
- `play_card`
- `end_turn`
- `choose_reward`
- `choose_event_option`

#### T9 调试与样例客户端
- Python demo
- Node demo
- README
- 联调脚本

#### T10 稳定性与配置
- 配置持久化
- ModConfig 集成（可选）
- 动作白名单
- 日志级别
- 性能采样

---

## 11. 关键实现细节要求

---

### 11.1 Patch 设计原则
- 不允许在 Patch 中堆砌业务逻辑；
- Patch 中仅做：
  - 取上下文
  - 映射 DTO
  - 发布事件
  - 更新少量状态快照
- 任何外部通信必须在 Patch 外处理。

### 11.2 线程安全要求
- `GameStateStore` 读写需保证一致性；
- WS 客户端集合需要线程安全；
- 网络请求线程不得直接操作游戏对象；
- 所有游戏动作必须切换回主线程执行。

### 11.3 日志要求
至少输出以下日志：
- Mod 初始化开始/成功/失败
- API 启动端口
- Hook 安装结果
- 关键事件发布
- 动作执行结果
- 异常堆栈
- 当前游戏版本 / 协议版本

### 11.4 性能要求
- 事件对象尽量轻量；
- 不要在高频 Hook 中频繁做大对象 JSON 序列化；
- 最近事件缓存限制条数；
- WebSocket 广播采用异步非阻塞发送；
- 状态快照按需更新。

---

## 12. 风险与应对

| 风险 | 说明 | 应对 |
|---|---|---|
| Hook 失效 | 游戏更新导致方法名或调用链变化 | 加版本检测、隔离 Hook 层、日志告警 |
| 线程问题 | 非主线程操作游戏对象导致异常 | 引入主线程调度器 |
| 协议漂移 | 事件字段变动影响外部客户端 | 固化协议版本，新增字段兼容扩展 |
| 状态不一致 | 事件与快照不同步 | 统一在状态更新点维护，动作后主动刷新 |
| 端口冲突 | 本地端口被占用 | 启动检测，允许配置端口 |
| 安全风险 | 外部程序滥用动作接口 | localhost + Token + 白名单 |
| 性能开销 | 高频事件导致卡顿 | 节流、采样、减少深序列化 |

---

## 13. 推荐默认配置

```json
{
  "enabled": true,
  "bindHost": "127.0.0.1",
  "port": 15526,
  "authToken": "change-me",
  "enableHttp": true,
  "enableWebSocket": true,
  "enableEventReplay": true,
  "enableDebugLog": true,
  "eventWhitelist": [
    "room.entered",
    "combat.started",
    "combat.ended",
    "turn.started",
    "card.played",
    "reward.opened",
    "reward.selected",
    "event.option_selected"
  ],
  "allowedActions": [
    "play_card",
    "end_turn",
    "choose_reward",
    "choose_event_option",
    "proceed"
  ]
}
```

---

## 14. 最小样例：外部 Python 客户端

```python
import requests
import websocket
import json

BASE = "http://127.0.0.1:15526"
TOKEN = "change-me"

def get_state():
    r = requests.get(
        f"{BASE}/state",
        headers={"X-STS2-Token": TOKEN},
        timeout=3
    )
    print(r.json())

def end_turn():
    payload = {
        "requestId": "req-end-turn-001",
        "action": "end_turn",
        "params": {}
    }
    r = requests.post(
        f"{BASE}/action",
        headers={
            "X-STS2-Token": TOKEN,
            "Content-Type": "application/json"
        },
        data=json.dumps(payload),
        timeout=3
    )
    print(r.json())

def on_message(ws, msg):
    print("EVENT:", msg)

def run_ws():
    ws = websocket.WebSocketApp(
        "ws://127.0.0.1:15526/ws",
        header=[f"X-STS2-Token: {TOKEN}"],
        on_message=on_message
    )
    ws.run_forever()

if __name__ == "__main__":
    get_state()
    # end_turn()
    # run_ws()
```

---

## 15. 对 Codex 的明确开发指令

请按以下要求实施：

1. 使用 **C# / .NET 9** 开发 STS2 Mod；
2. 使用 **Harmony** 实现关键事件 Hook；
3. 使用 **进程内 EventBus** 统一发布事件；
4. 使用 **本地 HTTP + WebSocket 服务** 对外暴露状态与事件；
5. 动作调用统一走 `ActionRouter`；
6. 所有游戏对象改动必须切回主线程；
7. 第一版仅实现：
   - `room.entered`
   - `combat.started`
   - `combat.ended`
   - `turn.started`
   - `card.played`
   - `/health`
   - `/version`
   - `/state`
   - `/events/recent`
   - `/action`
   - `play_card`
   - `end_turn`
   - `choose_reward`
   - `choose_event_option`
8. 输出完整 README，包括：
   - 构建步骤
   - 安装步骤
   - 运行说明
   - API 示例
9. 代码必须具备清晰目录结构，不允许把所有逻辑堆在一个文件中；
10. 需要附带至少一个 Python 示例客户端；
11. 需要为未来多人模式与 MCP 兼容保留扩展接口；
12. 所有接口返回统一 JSON；
13. 所有失败必须返回可读错误码和错误信息；
14. 需要写基础日志，并保证 Mod 初始化失败不会直接导致游戏崩溃。

---

## 16. 建议的首版验收清单

### 功能验收
- [ ] 游戏启动后 Mod 成功加载
- [ ] `/health` 返回正常
- [ ] `/version` 返回游戏/协议/Mod 版本
- [ ] `/state` 能返回当前状态
- [ ] 战斗开始时能推送 `combat.started`
- [ ] 打牌时能推送 `card.played`
- [ ] Python 客户端能连接 WS
- [ ] 外部客户端能调用 `end_turn`
- [ ] 外部客户端能调用 `play_card`
- [ ] 非法动作会返回明确错误

### 稳定性验收
- [ ] 接口异常不导致游戏崩溃
- [ ] Token 校验正常
- [ ] 端口可配置
- [ ] 关闭事件白名单后不再推送
- [ ] 高频操作下无明显卡顿

---

## 17. 结论

本方案的本质不是“做一个简单监听器”，而是做一个 **本地游戏事件桥接层（Game Bridge）**。  
它把 STS2 从“只能人工操作的游戏”抽象成：

- **可观测**：能读事件、读状态；
- **可控制**：能发动作；
- **可联动**：能接脚本、规则、AI Agent；
- **可扩展**：后续能做自动化、对局分析、回放、推荐、MCP 接入。

对 Codex 来说，建议严格按本文档的模块边界开发，先完成 MVP 闭环，再逐步补充更多事件和动作。

---

## 18. 备注（供开发参考）
当前社区已存在可参考的 STS2 方向项目，包括：
- Godot 4.5.1 + .NET 9 的 Mod 示例工程；
- BaseLib-StS2 依赖库；
- ModConfig-STS2 配置框架；
- STS2MCP 这类本地 API / MCP 方向项目；
- 其他已验证 `mods/` 目录安装方式的 STS2 Mod 项目。

本方案不要求直接复刻上述项目，但建议在开发时参考其目录组织、启动方式、配置方式和协议抽象思路。
