# Event Settings Panel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an in-game STS2Bridge settings panel that lets players toggle individual bridge events on and off, with changes applying immediately and persisting across launches.

**Architecture:** Introduce a dedicated bridge settings model and JSON-backed store, then route `GameEventBus` publication through a runtime event-toggle provider instead of a static-only whitelist. Add a lightweight Godot-based settings popup opened from the game's Settings screen via a dynamically injected button, keeping UI integration shallow and resilient to game updates.

**Tech Stack:** .NET 9, Harmony, GodotSharp, System.Text.Json, xUnit

---

### Task 1: Settings model and persistence

**Files:**
- Create: `D:\STS2-Link-YOKONEX\src\STS2Bridge\Config\BridgeSettings.cs`
- Create: `D:\STS2-Link-YOKONEX\src\STS2Bridge\Config\BridgeSettingsStore.cs`
- Test: `D:\STS2-Link-YOKONEX\tests\STS2Bridge.Tests\Config\BridgeSettingsStoreTests.cs`

- [ ] Write failing tests for default-enabled events and save/load roundtrip.
- [ ] Run the settings store tests to verify they fail for missing types.
- [ ] Implement minimal settings record and JSON store.
- [ ] Re-run the settings store tests until they pass.

### Task 2: Runtime event toggles

**Files:**
- Modify: `D:\STS2-Link-YOKONEX\src\STS2Bridge\Events\GameEventBus.cs`
- Create: `D:\STS2-Link-YOKONEX\src\STS2Bridge\Config\EventToggleService.cs`
- Modify: `D:\STS2-Link-YOKONEX\src\STS2Bridge\ModEntry.cs`
- Test: `D:\STS2-Link-YOKONEX\tests\STS2Bridge.Tests\Events\GameEventBusToggleTests.cs`

- [ ] Write failing tests for dynamic event enable/disable behavior.
- [ ] Run the event bus toggle tests to verify they fail.
- [ ] Implement runtime toggle service and wire it into `GameEventBus`.
- [ ] Re-run the tests until they pass.

### Task 3: Event settings metadata

**Files:**
- Create: `D:\STS2-Link-YOKONEX\src\STS2Bridge\Config\EventCatalog.cs`
- Test: `D:\STS2-Link-YOKONEX\tests\STS2Bridge.Tests\Config\EventCatalogTests.cs`

- [ ] Write failing tests for the event catalog containing the currently supported events.
- [ ] Run the catalog tests to verify they fail.
- [ ] Implement the event catalog used by settings UI and persistence defaults.
- [ ] Re-run the catalog tests until they pass.

### Task 4: In-game settings popup

**Files:**
- Create: `D:\STS2-Link-YOKONEX\src\STS2Bridge\Ui\EventSettingsPopup.cs`
- Create: `D:\STS2-Link-YOKONEX\src\STS2Bridge\Ui\SettingsScreenBridge.cs`
- Modify: `D:\STS2-Link-YOKONEX\src\STS2Bridge\ModEntry.cs`

- [ ] Implement a lightweight Godot popup panel with event checkboxes and save/apply behavior.
- [ ] Inject a `STS2Bridge Events` button into the game's Settings screen.
- [ ] Verify button re-entry, duplicate guard, and immediate toggle updates in code.

### Task 5: Verification and docs

**Files:**
- Modify: `D:\STS2-Link-YOKONEX\docs\websocket-integration.md`
- Modify: `D:\STS2-Link-YOKONEX\README.md`

- [ ] Run `dotnet test D:\STS2-Link-YOKONEX\tests\STS2Bridge.Tests\STS2Bridge.Tests.csproj`
- [ ] Run `dotnet build D:\STS2-Link-YOKONEX\src\STS2Bridge\STS2Bridge.csproj -c Release`
- [ ] Update docs to explain where event toggles live and how they affect event emission.
