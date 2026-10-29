# Reswin

A small Windows utility that lets you move and resize windows from anywhere inside them. Hold a modifier key, then use your mouse.

[Download 1.1.4](https://github.com/adzh1ev/Reswin/releases/tag/v1.1.4) · [Русский](README.ru.md)

## Get started

1. Download `Reswin-portable.zip` from [Releases](https://github.com/adzh1ev/Reswin/releases/latest).
2. Extract it into a permanent folder and run `Reswin.exe`. No installation is needed.
3. Point at a window and hold **Left Ctrl**:

| Mouse button | Default action |
| --- | --- |
| Left, hold | Move the window |
| Right, hold | Resize from the nearest edge or corner |
| Middle, click | Center on the current monitor |
| Back side button | Minimize |
| Forward side button | Maximize; click again to restore |

Release the button or modifier to end a drag. Centering respects the monitor's work area, including the taskbar.

Hold the selected modifier and double-click the mouse button assigned to **Move** to maximize or restore a window. To snap by dragging, move the pointer to an edge of the destination monitor and release the mouse button: the top maximizes the window, while the left and right edges place the window on that half of the work area without opening second-window suggestions. Both gestures are enabled by default and have separate switches in the top settings group.

To scroll, hold **Left Alt** and move the mouse up or down. Moving further from the starting point scrolls faster; release the key to stop. If window controls use Left Alt, scrolling automatically uses **Left Ctrl** instead. Choose **— Unassigned —** under Scrolling to disable it.

## Settings

Settings are split into three tabs: **Controls**, **Exclusions**, and **Settings**. The selected tab uses the coral accent; switching tabs preserves your assignments and list position. Click a tab or hold the left mouse button, drag across the tab strip and release to select a tab.

- Choose **Left Ctrl** or **Left Alt** and assign each of the five buttons independently. **— Unassigned —** is the first option.
- Turn **Snap to screen edges** and **Double-click to maximize** on or off independently. They use the mouse button assigned to Move.
- Scrolling uses the other left modifier automatically, or can be disabled. The setting displays the actual key.
- Adjust **Scroll sensitivity** from **25% to 200%** using the slider below Scrolling. 100% is this version's standard setting. The value is saved between launches.
- Use **Enable application**, next to Start with Windows, to pause or resume window controls and scrolling.
- Add running applications to **Exclusions** using **+**. All windows of that executable will be ignored. Remove an entry with **×**.
- Choose a light, dark or system theme, and Russian or English.
- Enable **Start with Windows** to launch in the tray when you sign in. It stays off until you turn it on.

Settings save automatically to `%LOCALAPPDATA%\Reswin\settings.xml`. Exclusions use the executable's full path; add an application again if that path changes.

Closing or minimizing settings hides the window in the system tray. Double-click the tray icon to reopen it. Use the tray menu to pause or exit. A second launch opens the existing instance.

`Reswin.exe --tray` starts directly in the tray. If you move the executable after enabling startup, turn startup on again from the new location.

## Requirements and limitations

Windows 10/11 x64 with .NET Framework 4.8 and WPF. The application uses C#, WPF and Windows APIs, without third-party packages, a browser engine or network requests.

Dragging a maximized window restores it once the pointer moves beyond the drag threshold. The two new gestures only maximize windows that support maximizing. Resize and center actions skip maximized windows. Fixed-size windows cannot be resized. Desktop, taskbar, hidden and some full-screen windows are ignored.

To control an application running as administrator, Reswin needs the same privilege level. Applications with custom frames, games and shared host processes may behave differently. Mixed-DPI multi-monitor setups still need hardware-specific validation.

## Source

| Files | Purpose |
| --- | --- |
| `src/WindowEngine.cs`, `src/Native.cs`, `src/HoldScroll.cs`, `src/ScrollAnimator.cs` | Input hooks, window operations and hold-to-scroll |
| `src/MainWindow.cs`, `src/TabbedNavigation.cs`, `src/Interface.cs` | Tabbed settings, page transitions and application picker |
| `src/Theme.cs`, `src/Interaction.cs`, `src/SmoothScrollViewer.cs`, `src/SensitivitySlider.cs` | Styling, motion, scrolling and sensitivity control |
| `src/Settings.cs`, `src/Localization.cs`, `src/Startup.cs` | Persistence, languages and opt-in startup |
| `src/Program.cs`, `src/TrayMenu.cs`, `src/Icons.cs`, `src/Typography.cs` | Application lifecycle, tray menu, icons and bundled fonts |
| `src/TestSuite.cs`, `src/UiChecks.cs` | Window and interface checks |

## Changes

See [what changed in 1.1.4](https://github.com/adzh1ev/Reswin/releases/tag/v1.1.4).

## Font and icon credits

Some interface icons are from [Lucide](https://lucide.dev/). Their license notices are in [assets/LUCIDE-LICENSE.txt](assets/LUCIDE-LICENSE.txt).

The [Manrope](https://github.com/googlefonts/manrope) font is bundled with the application under the SIL Open Font License 1.1; no system font installation is needed. See [assets/fonts/OFL.txt](assets/fonts/OFL.txt) and [font provenance](assets/fonts/SOURCE.txt). The portable archive includes `ThirdPartyNotices.txt` with the font and icon licenses.
