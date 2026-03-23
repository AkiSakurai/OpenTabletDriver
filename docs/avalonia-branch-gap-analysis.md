# Avalonia Branch Gap Analysis vs `0.6.x`

> **Context**: The `avalonia` branch is a major rewrite of OpenTabletDriver's UI using
> [Avalonia UI](https://avaloniaui.net/) to replace [Eto.Forms](https://github.com/picoe/Eto).
> Development has stalled and the branch is feature-incomplete compared to the stable `0.6.x`
> branch. This document identifies every missing piece that must be addressed before the
> Avalonia branch can replace `0.6.x` without regressions or missing functionality.
>
> **Base comparison**: `upstream/0.6.x` @ `dfe5868` vs `upstream/avalonia` @ `83d2105`.

---

## 1  Missing UI Windows / Dialogs

| Feature | 0.6.x location | Status in `avalonia` |
|---|---|---|
| Plugin Manager (install/remove/view plugins) | `OpenTabletDriver.UX/Windows/Plugins/` | ❌ Not implemented |
| Tablet Debugger | `OpenTabletDriver.UX/Windows/Tablet/TabletDebugger.cs` | ❌ Not implemented |
| Updater Window | `OpenTabletDriver.UX/Windows/Updater/UpdaterWindow.cs` | ❌ No UI (daemon updater code exists) |
| About Window | `OpenTabletDriver.UX/Windows/AboutWindow.cs` | ❌ Not implemented |
| Startup Greeter / Welcome dialog | `OpenTabletDriver.UX/Windows/Greeter/` | ❌ Not implemented |
| Device String Reader | `OpenTabletDriver.UX/Windows/DeviceStringReader.cs` | ❌ Not implemented |
| Area Converter Dialog | `OpenTabletDriver.UX/Windows/AreaConverterDialog.cs` | ❌ Not implemented |
| Advanced Binding Editor Dialog | `OpenTabletDriver.UX/Windows/Bindings/` | ⚠️ Partial (only `BindingMenuDialog`) |
| Exception Dialog | `OpenTabletDriver.UX/Dialogs/ExceptionDialog.cs` | ❌ Not implemented |
| Log View | `OpenTabletDriver.UX/Controls/LogView.cs` | ❌ Not implemented |
| Diagnostics View | `OpenTabletDriver.UI.Library/Views/DiagnosticsView.axaml` | ⚠️ Stub only (export button, empty VM) |

---

## 2  Missing UI Controls

| Feature | 0.6.x location | Status in `avalonia` |
|---|---|---|
| Wheel Binding Editor | `OpenTabletDriver.UX/Controls/Bindings/WheelBindingEditor.cs` | ❌ Not implemented |
| Drag Binding toggle in Profile UI | (part of Binding settings panel) | ❌ Not implemented |
| Plugin Setting Store Collection Editor | `OpenTabletDriver.UX/Controls/PluginSettingStoreCollectionEditor.cs` | ❌ Not implemented (only scalar plugin settings) |
| Tray Icon (Windows/Linux) | `OpenTabletDriver.UX/TrayIcon.cs` | ❌ Not implemented |
| macOS Menu Bar Extra | `OpenTabletDriver.UX.MacOS/` (PR #4501) | ❌ Not implemented |
| Tablet Switcher Panel | `OpenTabletDriver.UX/Controls/TabletSwitcherPanel.cs` | ⚠️ Replaced with different approach but feature parity unverified |
| Area Converter | `OpenTabletDriver.UX/Windows/AreaConverterDialog.cs` | ❌ Not implemented |
| `UnitLabel` control | `OpenTabletDriver.UX/Controls/` | ❌ Missing (0.6.x commit `b492fef1`) |

---

## 3  Missing Core Daemon / Binding Features

### 3.1  Multi-Wheel Support
The entire multi-wheel binding system added via `0.6.x` PR [#4004](https://github.com/OpenTabletDriver/OpenTabletDriver/pull/4004)
and subsequent improvements is absent from the `avalonia` branch.

Missing classes / components:
- `WheelBindings.cs` — binding logic per wheel (absolute + relative wheel handling)
- `WheelBindingSettings.cs` — per-wheel profile settings
- `DeltaThresholdBindingState.cs` — state for delta-based threshold bindings
- `BindingSettings.WheelBindings` list property
- `BindingSettings.StepSize` / split step-count by wheel type
- `BindingHandler` wheel dispatch (`HandleRelativeWheelReport`, `HandleAbsoluteWheelReport`, `HandleWheelButtonReport`)

Relevant 0.6.x commits not in `avalonia`:
```
12fe0074  Feature: Multi-wheel support
ba6f9929  Wheels: Split StepCount into keys based on wheel type
56625e0c  Put wheel StepSize in WheelBindingSettings for Profile
60d08f2d  BindingSettings: Set up wheel defaults
9746741a  UX/WheelBindings: Clamp threshold values to range
1a0538f6  UX/Wheels: Only show wheel button group if wheel has buttons
3b089471  WheelBindingEditor: Add null-check to WheelButtons
3c639f86  BindingSettings: Ensure wheel activation threshold are above 0
f88011f8  BindingHandler: Don't call wheel bindings if no wheels are defined
7fd2fd84  BindingHandler: Only warn on missing wheel declarations if report isn't neutral
cca657fb  WheelBindings: Fix Erroneous +1 in steps per tick calculation
b9273782  Parser/InspiroyRelWheelReport: Don't throw on unsupported wheel values
2b7f07c7  Default to 0 instead of throwing on unknown wheel byte in KamvasRelWheelReport
f6ab77bb  Fix a crash on macOS when switching profiles with tablets that have scroll wheels
```

### 3.2  Drag Bindings
Drag bindings (reworked from drag scrolling) are not present in the `avalonia` branch.

Relevant 0.6.x commits:
```
bb6bb46d  Drag Bindings
afc44cf5  BindingState: Drag scrolling -> drag binding
```

### 3.3  EvdevVirtualPad / IVirtualPad (Linux)
Linux Evdev virtual pad emulation (allows using the tablet ring/wheel as a virtual pad device) is absent.

Missing:
- `IVirtualPad` interface
- `EvdevVirtualPad` implementation
- `LinuxArtistModePadBinding` (present in 0.6.x `LinuxArtistMode/`)

Relevant 0.6.x commits:
```
c439d10b  Add IVirtualPad interface
bf9d4996  Add EvdevVirtualPad
1e0732bb  IVirtualPad: Abstract key events via TabletPadEvent
5a757319  LAMPadBinding: Use DI for VirtualPad
4dd2369b  LAMPadBinding: Add ToString for clearer daemon binding output
d85c1f4b  LAMPadBinding: Button 0 -> Button 10
50430ca3  udev rules: Group OpenTabletDriver devices
```

### 3.4  Adaptive Binding
`AdaptiveBinding` (which switches between pen action and mouse button depending on input device) is not present.

### 3.5  MouseScrollBinding Improvements
Several improvements and fixes to `MouseScrollBinding` are not in `avalonia`:
```
86d82db4  Merge: refactor-mousescrollbinding
d6a5b624  MouseScrollBinding: Fixup confusing timer initialization code
26bc691e  MouseScrollBinding: Improve value checks
7467df25  MouseScrollBinding: Use expression body for 'ValidDirections'
c47213c4  MouseScrollBinding: Default `interval` to 1
```

---

## 4  Missing Profile Settings

The Avalonia `BindingSettings` class (in `OpenTabletDriver.Daemon.Contracts/Persistence/BindingSettings.cs`)
only contains `PenButtons`, `AuxButtons`, `MouseButtons`, `MouseScrollUp`, and `MouseScrollDown`.
The following fields from 0.6.x `BindingSettings` are absent:

| Setting | Description |
|---|---|
| `WheelBindings` | List of per-wheel binding settings |
| `EnableDragBindings` | Toggle for drag binding feature |
| `DisablePressure` | Disable pressure sensitivity |
| `DisableTilt` | Disable tilt input |

---

## 5  Bug Fixes in `0.6.x` Not Backported to `avalonia`

The following bug fixes exist in `0.6.x` but not in the `avalonia` branch.
These must be ported to prevent regressions:

| Fix | Commit | Description |
|---|---|---|
| Race condition in `DrainPendingPosition` | `2015ba01` | `NullReferenceException` when rapid position updates occur |
| `ReportFormatter` invalid implicit cast | `9d4cdaec` | Invalid cast to `uint` causing incorrect report formatting |
| Missing `DeviceName` property change event | `7bac8183` | DeviceName changes not propagated in UI |
| `OSInfo` lookup returns exception | `2a72d23a` | Should return null instead of throwing on unknown platform |
| `EvdevVKeyboard` ContextMenu mapping | `a94fa30d` | Incorrect key mapping for ContextMenu key |
| Linux `EventCode` invalid codes | `267a0903` | Several event codes were incorrect |
| Proximity event on eraser/pen switch | `04d2603b` | Proximity event not sent immediately on eraser ↔ pen tool switch |
| macOS wheel binding crash on profile switch | `f6ab77bb` | Crash when switching profiles on tablets with scroll wheels |
| XP-Pen Aux Report import | `b3008c4f` | Fix incorrect import in XP-Pen Aux parser |
| XP-Pen Deco 03 wheel definition | `242648bb` | Incorrect wheel byte definition |
| XP-Pen Aux parser syntax error | `9fb409fe` | Syntax error in parser |
| Tablet proximity fix for macOS | `965fa8cc` | Vendor pointer type needed for Qt5 compatibility |
| macOS modifier key binding with mouse events | `84f77db0` | Modifier keys broken with mouse events on macOS |

---

## 6  Performance / Refactoring Improvements in `0.6.x` Not in `avalonia`

| Improvement | Commit(s) | Description |
|---|---|---|
| Use concrete types for performance | `eeb2d923` | Avoid boxing/virtual dispatch in hot paths |
| Interfaces as covariant | `038d84cb` | Improve type safety and reduce casts |
| Native/Linux structs → record structs | `02372858` | Memory layout improvement |
| Native/OSX structs → record structs | `1fe4c93b` | Memory layout improvement |
| XDG path evaluation refactor | `8633bbba` | Cleaner Unix XDG path handling |
| IDisposable pattern fixes | `6d15fdc5` | Proper resource cleanup |
| Collection expression usage | `4d7e7c52` | Modern C# idioms |
| `DynamicallyAccessedMembers` annotations | `68451a89` | Better trimming support |
| Remove `IPointerProvider<T>` | `c091f58b` | Dead interface removed |
| Linux `ERRNO.ECANCELED` | `9a7cf02a` | Missing error code added |

---

## 7  Diagnostics / Daemon Improvements Not in `avalonia`

| Feature | Commit | Description |
|---|---|---|
| `IDriverDaemon.GetDiagnosticInfo()` | `0eec8494` | Diagnostics accessible from CLI/GUI |
| `DriverDaemon` emits AppDataDirectory | `e3af3a66` | Logs used AppData directory on startup |
| Diagnostics: Add HOME env var | `7ef989fe` | HOME included in diagnostic output |
| Daemon timeout improved message (Linux) | `790dd368` | More helpful message pointing to systemd setup |
| Log filter settings | `e459a69a` (PR #4459) | Log level filter configurable from settings |
| `AppInfo`: Require `AppDataDirectory` | `65e1338c` | `AppDataDirectory` is no longer optional |

---

## 8  Test Coverage Gaps

| Test | Commit | Description |
|---|---|---|
| Report parser types are structs | `a49a99df` | Ensures all report parsers are value types for performance |
| Config lint whitespace check | `31808afc` (avalonia) | Present in avalonia already |

---

## 9  Tablet Debugger Rework

The Tablet Debugger in 0.6.x was completely reworked (PR #4434) with many new capabilities.
This is entirely missing from the `avalonia` branch:

- `TabletDebuggerViewModel` class (moved to `OpenTabletDriver.Desktop`)
- Statistics / histogram system
- Report parser filtering by model + type
- Binary decoding mode
- Report rate calculation with moving average
- Additional stats (pen buttons, tilt, touch position, aux buttons)
- Tablet filter support (ignore specific tablets)
- Data recording and dump export
- `UnitLabel` control

Relevant commits (partial list):
```
0bbcf8c3  Merge: 06x-rework-tablet-debugger
66841019  Tablet Debugger: Move to ViewModel
1866090a  Tablet Debugger: Rework to horizontal layout
4ca7ee89  TabletDebugger: Move the ViewModel to OpenTabletDriver.Desktop
7c28ec11  TabletDebugger: Add binary decoding mode
384f4369  TabletDebugger: Filter by model + report type
c0a18f1b  TabletDebugger: Inline max groups per row
241684e1  TabletDebugger: Add Tilt to additional stats
(… 50+ more commits)
```

---

## 10  Tablet Configuration Backlog

The following tablet configurations and parser fixes exist in `0.6.x` but not in `avalonia`.
Many of these are additive (new tablets) but some fix existing configs:

- Huion H951P roller support (`14e4c85b`)
- Huion Q620M wheel rotation support (`0c36c27e`)
- Huion Inspiroy wheel parsing (`29278378`)
- XP-Pen Artist 15.6 Pro V2 wheel support (`82e90420`)
- XP-Pen Artist 13.3 Pro wheel support (`1deb275d`)
- XP-Pen Artist 24 Pro wheel support (`ea616a35`)
- XP-Pen Deco 03 wheel fixes (`242648bb`, `9c4ad0bd`)
- Wacom Intuos5 PTH-450/650/850 and PTK-450/650 config fixes (`196fdfe5`, `fc6459e5`)
- Gaomon PD1320 (`a5937333`)
- Artisul D22S (`a0ef88cb`)
- Bosto BT-12HD (`3a700440`)
- Huion Kamvas Pro 24 Gen 3 (`fd402979`)
- UGEE S640: Remove `OutputReportLength` (`148e60ea`)
- libinput quirks: `AttrPressureRange` (`2fa90f8c`)
- 30+ additional new tablet configurations

---

## Summary

The `avalonia` branch is a work-in-progress that needs significant effort to reach feature
parity with `0.6.x`. The most critical items (without which the driver will be non-functional
or significantly regressed) are:

1. **Multi-wheel support** — tablets with wheels will not work correctly
2. **Missing UI windows** — Plugin Manager, Updater, About, Debugger, Device String Reader
3. **Drag Bindings** — feature regression
4. **EvdevVirtualPad / IVirtualPad** — Linux pad emulation regression
5. **Bug fixes** — race condition, proximity events, parser fixes
6. **Diagnostics / Tray / macOS Menu Bar Extra** — usability regression
7. **Profile settings parity** — `DisablePressure`, `DisableTilt`, `WheelBindings`
8. **Tablet Debugger** — tool needed for troubleshooting and tablet support development

Total commits in `0.6.x` not yet in `avalonia`: **~2 030** (many are tablet configurations;
~860 are code/feature/fix commits).

---

*Generated by analysis of `upstream/0.6.x` (`dfe5868`) vs `upstream/avalonia` (`83d2105`)
on 2026-03-23.*
