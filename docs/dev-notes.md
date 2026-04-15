# 开发备注

## 当前实现定位

当前仓库是“可测试的桥接层骨架”，重点是模块边界、协议稳定性和后续扩展接口，不是已经接完真实 STS2 内部对象的成品 Mod。

## 目标框架

- 主项目：`net9.0`
- 测试项目：`net9.0`

这是为了与游戏自身的 `sts2.runtimeconfig.json` 保持一致。

## 已做的隔离

- 项目已直接引用游戏目录中的 `0Harmony.dll`
- `Compatibility/ModInitializerAttribute.cs`：占位初始化特性
- `Hooks/*`：按事件域切分，但未绑定真实方法
- `Actions/Executors/*`：协议层已就绪，真实游戏行为待接入

## 后续接真实环境建议

1. 新增 `Adapters/Sts2Runtime/` 目录承接对游戏对象的反射访问
2. Patch 只做取值、映射、发布事件，不把业务写死在 Hook 里
3. 对游戏线程敏感的操作全部通过 `MainThreadDispatcher`
4. 状态快照只对外暴露 DTO，不直接暴露游戏内部类
