# Avalonia Branch – Exhaustive Gap Analysis vs 0.6.x

> **Generated**: 2026-03-23  
> **Compared**: `upstream/avalonia` (HEAD) vs `upstream/0.6.x` (v0.6.7)  
> All findings are based on direct file-by-file inspection of both branches, not commit messages.

---

## How to read this

- `[MISSING]` – file/feature exists in 0.6.x but has **no equivalent** in avalonia  
- `[STUB]` – file exists in avalonia but is empty/placeholder with no real implementation  
- `[DIFF]` – file exists in both but avalonia is **missing specific functionality**

---

## 1. Daemon Startup (`OpenTabletDriver.Daemon/Program.cs`)

### `[DIFF]` Broken `--appdata` / `--config` CLI arguments

**0.6.x** reads `--appdata` and `--config` and sets `AppInfo.Current.AppDataDirectory` / `AppInfo.Current.ConfigurationDirectory` before the daemon starts.

**Avalonia** parses the arguments but the actual assignment is **commented out** with a `FIXME`:

```csharp
// FIXME: these should be set in appInfo
//if (!string.IsNullOrWhiteSpace(appdata))
//    appInfo.AppDataDirectory = FileUtilities.InjectEnvironmentVariables(appdata);
//if (!string.IsNullOrWhiteSpace(config))
//    appInfo.ConfigurationDirectory = FileUtilities.InjectEnvironmentVariables(config);
```

The daemon will silently ignore `--appdata` and `--config`. Systemd unit files, Docker setups, and anything that passes custom paths will silently break.

---

### `[MISSING]` SIGHUP and SIGINT handling

**0.6.x** registers:
- `Console.CancelKeyPress` → graceful shutdown on SIGINT / Ctrl+C  
- `PosixSignalRegistration.Create(PosixSignal.SIGHUP, ...)` → graceful shutdown on HUP  
- `AssemblyLoadContext.Default.Unloading` → daemon cleanup on unload  

**Avalonia** has none of these. The daemon cannot be gracefully stopped by `systemctl stop opentabletdriver` or Ctrl+C on Linux.

---

### `[MISSING]` Graceful daemon shutdown with cleanup (`CloseDaemon`)

**0.6.x** has a `CloseDaemon()` function that:
1. Sets `tokenCancelled = true`
2. Polls `daemonRunning` until the driver fully stops
3. Logs shutdown progress at each step

**Avalonia** exits via exception only — no graceful drain.

---

### `[MISSING]` `OpenTabletDriver.Daemon/DriverDaemon.cs` and `LogFile.cs`

In 0.6.x these lived in the executable project. Avalonia moved them to `OpenTabletDriver.Daemon.Library/`. The following features present in 0.6.x's `Daemon/DriverDaemon.cs` are **absent** from avalonia's `Daemon.Library/DriverDaemon.cs`:

| Feature | 0.6.x | Avalonia |
|---|---|---|
| `EnableDragBindings` profile flag dispatch | ✅ | ❌ |
| `WheelBindings` list dispatch (per-wheel) | ✅ | ❌ |
| `event DeviceReport` (raw report forwarding) | ✅ | ❌ (code commented out) |
| `ForceResynchronize()` method | ✅ | ❌ |
| High-priority process setup (Windows) | ✅ | ✅ |
| Power throttling disable (Windows 11) | ✅ | ✅ |

---

## 2. Daemon Contracts (`OpenTabletDriver.Daemon.Contracts/IDriverDaemon.cs`)

The avalonia interface is redesigned (per-tablet IDs instead of a single Settings blob), but these operations are **absent**:

| Method / Event | 0.6.x | Avalonia |
|---|---|---|
| `event DeviceReport` (raw report forwarding) | ✅ | ❌ |
| `event Resynchronize` | ✅ | ❌ |
| `ForceResynchronize()` | ✅ | ❌ |
| `LoadPlugins()` | ✅ | ❌ (replaced by `Initialize()`) |
| `GetDefaults(string typePath)` | ✅ | ❌ |

---

## 3. Binding System

### `[MISSING]` `Binding/AdaptiveBinding.cs`

Switches between `IPenActionHandler` and `IMouseButtonHandler` depending on tool mode. Needed for tablets where pen and mouse modes share buttons. Entirely absent from avalonia.

---

### `[MISSING]` `Binding/MouseScrollBinding.cs`

Timer-based repeated scroll binding. While a button is held, scrolls at a configurable interval. Avalonia has `BindingSettings.MouseScrollDown/Up` fields but **no actual implementation**.

---

### `[MISSING]` `Binding/DeltaThresholdBindingState.cs`

Delta-based threshold binding state for wheel bindings. Required for multi-wheel support.

---

### `[MISSING]` `Binding/WheelBindings.cs`

Full per-wheel binding class. Handles relative wheel, absolute wheel, and wheel button reports. Maps to `WheelBindingSettings`. Entirely absent from avalonia.

---

### `[DIFF]` `Binding/BindingState.cs` — drag binding support absent

**0.6.x `BindingState`**:
```csharp
public bool RequiresPenPressure { set; get; } // "drag bindings"

public virtual void Invoke(TabletReference tablet, IDeviceReport report, bool newState)
{
    bool pressureThresholdIsMetOrUnneeded =
        !RequiresPenPressure || (RequiresPenPressure && report is ITabletReport { Pressure: > 0 });
    if (!newState || pressureThresholdIsMetOrUnneeded)
        ...
}
```

**Avalonia `BindingState`**: no `RequiresPenPressure`, no drag binding support. Bindings fire unconditionally.

---

### `[DIFF]` `Binding/BindingHandler.cs` — wheel and drag dispatch absent

**0.6.x** dispatches:
- `IWheelButtonReport` → `HandleWheelButtonReport`
- `IAbsoluteWheelReport` → `HandleAbsoluteWheelReport`
- `IRelativeWheelReport` → `HandleRelativeWheelReport`
- `Wheels` dictionary (per-wheel `WheelBindings` instances)
- `DragBindings` flag (marks `PenButtons` as drag-only)

**Avalonia**: none of the above. Wheel reports are silently dropped.

---

### `[MISSING]` `Binding/LinuxArtistMode/LinuxArtistModePadBinding.cs`

Sends Linux Wacom tablet pad events via `IVirtualPad`. Only `LinuxArtistModeButtonBinding.cs` (keyboard) exists in avalonia. Ring/wheel → tablet pad emulation is broken.

---

## 4. Profile / Persistence

### `[MISSING]` `Profiles/WheelBindingSettings.cs`

Per-wheel settings (`ClockwiseBinding`, `CounterClockwiseBinding`, `WheelButtons`, `StepSize`). Required for any tablet with a physical wheel.

---

### `[DIFF]` `Daemon.Contracts/Persistence/BindingSettings.cs` — missing fields

Avalonia has: `TipActivationThreshold`, `TipButton`, `EraserActivationThreshold`, `EraserButton`, `PenButtons`, `AuxButtons`, `MouseButtons`, `MouseScrollUp`, `MouseScrollDown`.

**0.6.x additionally has**:
```csharp
List<WheelBindingSettings> WheelBindings   // per-wheel config
bool EnableDragBindings                     // drag binding toggle
bool DisablePressure                        // suppress pressure input
bool DisableTilt                            // suppress tilt input
```

---

## 5. Input Interfaces (missing from `OpenTabletDriver/Tablet/`)

These exist in 0.6.x (`OpenTabletDriver.Plugin/Tablet/`) but have **no equivalent** in avalonia:

| File | Purpose |
|---|---|
| `Wheel/IAbsoluteWheelReport.cs` | Tablets reporting absolute wheel position |
| `Wheel/IRelativeWheelReport.cs` | Tablets reporting relative wheel delta |
| `Wheel/IWheelButtonReport.cs` | Tablets reporting wheel button state |
| `WheelSpecifications.cs` | Wheel hardware specs (step count, relative vs absolute) |
| `AnalogSpecifications.cs` | Analog strip/ring specs |
| `IAbsoluteAnalogReport.cs` | Absolute analog (ring/strip) position reports |
| `IRelativeAnalogReport.cs` | Relative analog delta reports |
| `IProximityReport.cs` | Pen hover proximity (`NearProximity`, `HoverDistance`) |
| `TabletPadEvent.cs` | Enum for virtual pad buttons (BUTTON_1…BUTTON_10) |
| `TabletReference.cs` | Tablet identity wrapper (renamed to `InputDevice` in avalonia) |

### `[DIFF]` `TabletSpecifications.cs` — Wheels and Strips properties absent

**0.6.x**:
```csharp
public List<WheelSpecifications>? Wheels { get; set; }
public List<AnalogSpecifications>? Strips { set; get; }
```
**Avalonia** has neither. All tablet configurations with wheel specs will fail to load properly.

---

## 6. Platform Interfaces (missing from `OpenTabletDriver/Platform/`)

| Interface | Purpose |
|---|---|
| `Keyboard/IVirtualPad` | Linux Wacom-compatible virtual pad device |
| `Pointer/IPenActionHandler` | Activate/Deactivate pen tip, eraser, barrel buttons |
| `Pointer/IMouseScrollHandler` | `ScrollVertically(int)`, `ScrollHorizontally(int)` |
| `Pointer/PenAction` (enum) | Tip, Eraser, BarrelButton1/2/3 |
| `Pointer/ScrollDirection` (enum) | Vertical, Horizontal |

`IMouseScrollHandler` and `IPenActionHandler` are missing entirely — even adding the files for `MouseScrollBinding` and `AdaptiveBinding` cannot fix things without these interfaces.

---

## 7. `OpenTabletDriver.Desktop/Interop/Input/Exotic/EvdevVirtualPad.cs`

Linux implementation of `IVirtualPad`. Creates a virtual Wacom-compatible evdev pad device for tablet ring/strip → pad button emulation. Entirely absent from avalonia.

**Dependencies that also need to exist**:
- `TabletPadEvent` enum (§5)
- `IVirtualPad` interface (§6)
- `EvdevDevice.EnableCustomCode` (already present in `Native.Linux`)
- Registration in `DesktopLinuxServiceCollection`

---

## 8. UI — No Daemon Auto-Launcher (`DaemonWatchdog`)

**0.6.x** has `OpenTabletDriver.UX/DaemonWatchdog.cs` which:
1. Detects platform at startup
2. On Windows and macOS: spawns `OpenTabletDriver.Daemon.exe` as a child process
3. Restarts it on unexpected exit
4. Kills it cleanly when the UI closes

**Avalonia** has no equivalent whatsoever. `IDaemonService` only exposes `ConnectAsync()` — it assumes the daemon is already running. On Windows and macOS (where the UI is expected to start the daemon), the avalonia UI will simply fail to connect and show the `DaemonConnectionView` indefinitely.

**Specifically absent**:
- `OpenTabletDriver.UX/DaemonWatchdog.cs` — no avalonia equivalent
- `App.EnableDaemonWatchdog` flag — no avalonia equivalent
- `App.DaemonWatchdog` static field — no avalonia equivalent
- `MainForm.StartDaemonWatchdog()` — no avalonia equivalent

---

## 9. UI — Linux/macOS Platform Services Commented Out

`ServiceExtensions.WithPlatformServices()` in avalonia:
```csharp
if (OperatingSystem.IsWindows())
    return services.AddWindowsServices();
// else if (OperatingSystem.IsLinux())
//     return services.AddLinuxServices();    ← COMMENTED OUT
// else if (OperatingSystem.IsMacOS())
//     return services.AddMacServices();      ← COMMENTED OUT
else
    return services;
```

On Linux and macOS, no platform services are registered. The UI will fail to function on those platforms.

---

## 10. UI — Missing Windows

No Avalonia equivalent exists for any of the following 0.6.x windows:

| Window | 0.6.x File | Status in Avalonia |
|---|---|---|
| Plugin Manager | `Windows/Plugins/PluginManagerWindow.cs` | ❌ Missing |
| Plugin Metadata Viewer | `Windows/Plugins/MetadataViewer.cs` | ❌ Missing |
| Plugin Drop Panel | `Windows/Plugins/PluginDropPanel.cs` | ❌ Missing |
| Plugin Metadata List | `Windows/Plugins/PluginMetadataList.cs` | ❌ Missing |
| Tablet Debugger | `Windows/Tablet/TabletDebugger.cs` | ❌ Missing |
| Self-Updater | `Windows/Updater/UpdaterWindow.cs` | ❌ Missing |
| About Window | `Windows/AboutWindow.cs` | ❌ Missing |
| Startup Greeter (6 pages) | `Windows/Greeter/` | ❌ Missing |
| Device String Reader | `Windows/DeviceStringReader.cs` | ❌ Missing |
| Area Converter Dialog | `Windows/AreaConverterDialog.cs` | ❌ Missing |
| Advanced Binding Editor | `Windows/Bindings/AdvancedBindingEditorDialog.cs` | ❌ Missing |
| Exception Dialog | `Dialogs/ExceptionDialog.cs` | ❌ Missing |
| Repository/changelog dialog | `Dialogs/RepositoryDialog.cs` | ❌ Missing |

---

### `[STUB]` `DiagnosticsView` and `DiagnosticsViewModel`

File exists but `DiagnosticsViewModel` body is empty:
```csharp
public class DiagnosticsViewModel : ActivatableViewModelBase
{
    public DiagnosticsViewModel() { }
}
```
The View only has an "Export Diagnostics" button — no data display.

---

## 11. UI — Missing Controls

| Control | 0.6.x File |
|---|---|
| Wheel Binding Editor | `Controls/Bindings/WheelBindingEditor.cs` |
| Log View panel | `Controls/LogView.cs` |
| Tray Icon | `TrayIcon.cs` |
| Plugin Setting Store Editor | `Controls/PluginSettingStoreEditor.cs` |
| Plugin Setting Store Collection Editor | `Controls/PluginSettingStoreCollectionEditor.cs` |
| Tablet Switcher Panel | `Controls/TabletSwitcherPanel.cs` |
| Composition Scheduler | `Tools/CompositionScheduler.cs` |
| Log Data Store | `Tools/LogDataStore.cs` |
| Parse Tools | `Tools/ParseTools.cs` |
| Area Control (outer shell) | `Controls/Output/Area/AreaControl.cs` |
| Rotation Area Editor | `Controls/Output/Area/RotationAreaEditor.cs` |
| Unit Group | `Controls/Output/Area/UnitGroup.cs` |
| UnitLabel | `Controls/Generic/UnitLabel.cs` |
| FloatSlider | `Controls/Generic/FloatSlider.cs` |
| Generic Reflection TypeDropDown | `Controls/Generic/Reflection/TypeDropDown.cs` |
| Generic Reflection TypeListBox | `Controls/Generic/Reflection/TypeListBox.cs` |
| ScheduledDrawable | `Controls/Generic/ScheduledDrawable.cs` |
| TextContent | `Controls/Generic/TextContent.cs` |
| DoubleNumberBox | `Controls/Generic/Text/DoubleNumberBox.cs` |
| FloatNumberBox | `Controls/Generic/Text/FloatNumberBox.cs` |

---

## 12. UI — `TabletViewModel` / `TabletView` Missing Features

`TabletView.axaml` and `TabletViewModel.cs` in avalonia do not implement:

- **Wheel bindings section** — no per-wheel binding UI
- **Drag bindings toggle** — `EnableDragBindings` not present
- **Disable Pressure / Disable Tilt checkboxes** — `DisablePressure` / `DisableTilt` missing
- **Per-button drag mode** — no `RequiresPenPressure` toggle per pen button
- **Wheel-count-based tab generation** — 0.6.x dynamically adds `Wheel N Bindings` tabs based on `tablet.Properties.Specifications.Wheels.Count`

---

## 13. Console Commands — Missing from Avalonia

The avalonia `ProgramCommands` has 33 commands. The 0.6.x `Program.cs` has 47. **Missing from avalonia**:

| Command | Description |
|---|---|
| `save-defaults` | Save settings to the default settings file |
| `stdio` | Open with stdin/stdout for scripting |
| `install-plugin` | Install plugin from file path |
| `uninstall-plugin` | Uninstall plugin by folder name |
| `map-to-display-index` | Map tablet to a specific display index |
| `enable-tablet-filters` | Enable specific filters for a tablet |
| `disable-tablet-filters` | Disable specific filters for a tablet |
| `reset-tablet-filters` | Reset filters to defaults for a tablet |
| `enable-tools` | Enable specified tools |
| `disable-tools` | Disable specified tools |
| `get-misc-settings` | Get clipping/rotation/misc settings |
| `list-plugins` | List installed plugin folder names |
| `list-displays` | List all available displays |
| `has-update` | Check for available updates |
| `edit` | Open settings in `$EDITOR` |
| `set-enable-clipping` | Toggle input clipping |
| `set-enable-area-limiting` | Toggle area limiting |
| `set-lock-aspect-ratio` | Toggle aspect ratio lock |

---

## 14. Diagnostics — Incomplete `EnvironmentDictionary`

**0.6.x** platform-specific env vars:

- **Linux**: `USER`, `DISPLAY`, `WAYLAND_DISPLAY`, `XDG_CURRENT_DESKTOP`, `PATH`, `PWD`, `XDG_CONFIG_HOME`, `HOME`, `XDG_DATA_HOME`, `XDG_CACHE_HOME`, `XDG_RUNTIME_DIR`, `TEMP`
- **Windows**: `TEMP`, `TMP`, `TMPDIR`, `USERPROFILE`

**Avalonia's `LinuxEnvironmentDictionary`** only captures: `DISPLAY`, `WAYLAND_DISPLAY`, `PWD`, `PATH`.

**Missing**: `XDG_CURRENT_DESKTOP` (critical for diagnosing KDE/COSMIC quirks), `HOME`, all `XDG_*` dirs, `USERPROFILE` on Windows. Diagnostic reports will be incomplete.

---

## 15. Missing Test Files

| File | Purpose |
|---|---|
| `OpenTabletDriver.Tests/ConfigurationTest/AttributesTest.SelfTests.cs` | Self-tests for attribute validators |
| `OpenTabletDriver.Tests/ConfigurationTest/AttributesTest.cs` | Attribute validation tests |
| `OpenTabletDriver.Tests/ConfigurationTest/ConfigurationTest.cs` | Full configuration parsing tests |
| `OpenTabletDriver.Tests/ConfigurationTest/Consts.cs` | Test constants |
| `OpenTabletDriver.Tests/ConfigurationTest/DeviceIdentifierTest.SelfTests.cs` | Self-tests |
| `OpenTabletDriver.Tests/ConfigurationTest/DeviceIdentifierTest.cs` | Device identifier tests |
| `OpenTabletDriver.Tests/ConfigurationTest/Extensions.cs` | Test extensions |
| `OpenTabletDriver.Tests/ConfigurationTest/TestData.cs` | Test data |
| `OpenTabletDriver.Tests/ConfigurationTest/TestTabletConfiguration.cs` | Config test helpers |
| `OpenTabletDriver.Tests/ConfigurationTest/TestTypes.cs` | Test types |
| `OpenTabletDriver.Tests/Fakes/FakeHidDevice.cs` | HID device mock |
| `OpenTabletDriver.Tests/Fakes/FakeUpdater.cs` | Updater mock |
| `OpenTabletDriver.Tests/HidSharpEndpointTest.cs` | HidSharp integration test |
| `OpenTabletDriver.Tests/UXExtensionTests.cs` | UX extension tests |
| `OpenTabletDriver.Tests/CodeValidationTest.cs` | Code quality checks (report parsers must be structs) |
| `OpenTabletDriver.Benchmarks/Plugin/PluginManagerBenchmark.cs` | Plugin manager benchmark |

---

## 16. macOS — Missing Entry Point and Project

| Item | 0.6.x | Avalonia |
|---|---|---|
| `OpenTabletDriver.UX.MacOS/Program.cs` | ✅ | ❌ Missing |
| `OpenTabletDriver.UX.MacOS.csproj` | ✅ | ❌ Missing |
| `PermissionHelper.cs` | ✅ | ✅ (file only, no project) |

The macOS entry point is entirely absent. `PermissionHelper.cs` exists as an orphaned file with no project to build it.

---

## 17. CI / Build Infrastructure — Differences

| Item | 0.6.x | Avalonia |
|---|---|---|
| PR Labeler YAML | ✅ `pr-labeler.yml` | Uses `labeler.yml` (different format) |
| Release Notes YAML | ✅ `.github/release.yml` | ❌ Missing |
| Manual PR Labeler workflow | ✅ | ❌ Missing |
| Shell tests (`tests/`) | ✅ 2 shell test scripts | ❌ Missing |
| `build.ps1` (PowerShell build) | ✅ | ❌ Missing |
| `eng/windows/package.ps1` | ✅ | ❌ Missing |
| `scripts/find-tablet-support.sh` | ✅ | ❌ Missing |
| `OpenTabletDriver.sln.DotSettings` (ReSharper) | ✅ | ❌ Missing |
| `UseLocalHidSharp.ps1` / `.sh` | ❌ | ✅ Added in avalonia |
| Nix flake + devshell | ❌ | ✅ Added in avalonia |
| Roslyn Analyzers project | ❌ | ✅ Added in avalonia |

---

## 18. Other Missing Files

| File | Notes |
|---|---|
| `OpenTabletDriver.Desktop/Conversion/GaomonV2AreaConverter.cs` | Gaomon V2 area conversion unavailable |
| `OpenTabletDriver.Plugin/Globals.cs` | Legacy compat constant (`LegacyTabletConfigurationProperty`) |
| `OpenTabletDriver.Plugin/Tablet/IProximityReport.cs` | Hover distance reporting |
| `OpenTabletDriver.Native/Linux/Xorg/XLib.cs` / `XRandr.cs` / `XRRMonitorInfo.cs` | Linux X11 display enumeration (moved to `OpenTabletDriver.Native.Linux/` in avalonia) |
| `.run/MacOS UI & Daemon.run.xml` | JetBrains run configuration for macOS dev |

---

## Summary Priority Table

| Priority | Area | Impact |
|---|---|---|
| 🔴 Critical | `--appdata`/`--config` FIXME in daemon | Silent data directory misconfiguration |
| 🔴 Critical | No SIGHUP/SIGINT handling | Daemon cannot be gracefully stopped |
| 🔴 Critical | No `DaemonWatchdog` | UI cannot launch daemon on Windows/macOS |
| 🔴 Critical | Linux/macOS services commented out | UI crashes or does nothing on Linux/macOS |
| 🔴 Critical | `WheelSpecifications` / wheel interfaces missing | All wheel-equipped tablets silently fail |
| 🔴 Critical | Wheel dispatch absent from `BindingHandler` | Wheel inputs never processed |
| 🔴 Critical | `TabletSpecifications.Wheels` absent | Wheel tablet configs fail to parse |
| 🔴 Critical | `IProximityReport` missing | Hover distance not reported |
| 🟠 High | `AdaptiveBinding` missing | Pen↔mouse adaptive behavior gone |
| 🟠 High | `MouseScrollBinding` missing | Repeated scroll on held button gone |
| 🟠 High | Drag bindings (`RequiresPenPressure`) missing | Drag-only bindings never activate |
| 🟠 High | `EvdevVirtualPad` / `IVirtualPad` missing | Linux artist mode pad emulation broken |
| 🟠 High | Plugin Manager window missing | No way to install/remove plugins via UI |
| 🟠 High | `event DeviceReport` commented out | Tablet Debugger cannot receive raw reports |
| 🟡 Medium | Diagnostics view stub | Export/display of diagnostics absent |
| 🟡 Medium | Tray icon missing | Cannot minimize to tray |
| 🟡 Medium | `XDG_CURRENT_DESKTOP` missing from env diag | Incomplete diagnostics |
| 🟡 Medium | 14 missing console commands | Scripting/automation regressions |
| 🟡 Medium | Startup Greeter absent | No first-run guidance |
| 🟡 Medium | Updater window missing | No self-update UI |
| 🟢 Low | `DisablePressure`/`DisableTilt` absent | Feature regression for specific workflows |
| 🟢 Low | `GaomonV2AreaConverter` missing | Gaomon V2 area conversion unavailable |
| 🟢 Low | macOS project files missing | Cannot build macOS UI |
| 🟢 Low | Release notes YAML, shell tests missing | CI/release automation gaps |
