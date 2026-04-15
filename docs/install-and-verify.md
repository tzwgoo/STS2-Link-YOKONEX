# 安装与验证

## 1. 构建 Mod

```powershell
dotnet build src/STS2Bridge/STS2-Link-YOKONEX.csproj -c Release
```

构建产物会整理到：

```text
D:\STS2-Link-YOKONEX\artifacts\mods\STS2-Link-YOKONEX
```

## 2. 安装到游戏目录

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\install-mod.ps1
```

默认会安装到：

```text
D:\Users\hosgoo\Downloads\Slay the Spire 2\Slay the Spire 2\mods\STS2-Link-YOKONEX
```

## 3. 当前包内容

- `STS2-Link-YOKONEX.dll`
- `STS2-Link-YOKONEX.json`

## 4. 当前验证目标

第一轮建议只验证这些：

1. 进入游戏后不崩溃
2. Mod 菜单或 Mod 加载流程里能看到 `STS2-Link-YOKONEX`
3. 初始化日志成功执行
4. 如果后续确认本地 API 已启动，再测 `/health`

## 5. 如果没被识别

优先排查：

1. `mods` 目录层级是否正确
2. `STS2-Link-YOKONEX.json` 字段是否符合游戏要求
3. 游戏是否要求额外的 `pck` 或资源文件
4. 是否需要社区基础库一起放入 Mod 目录
