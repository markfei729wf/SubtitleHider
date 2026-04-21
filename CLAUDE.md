# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SubtitleHider is a WPF desktop application that creates a transparent overlay window to hide subtitles in videos. It runs on Windows 10/11 using .NET 8.

**Interaction Model:**
- The app starts directly showing the Hider overlay window (black bar, opacity=1) at the bottom-center of the screen
- Right-click on the Hider window opens a context menu with two options:
  - **设置透明度** - Opens a compact settings window positioned above the Hider
  - **关闭** - Closes the application
- The settings window is never shown by default; it is only opened via right-click context menu
- Window position and size are automatically remembered across sessions (saved to `%LocalAppData%/SubtitleHider/settings.json`)

## Build Commands

```bash
# Build
cd SubtitleHider
dotnet build

# Run
dotnet run

# Release build
dotnet build -c Release

# Publish framework-dependent (~172 KB, requires .NET 8 runtime)
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ./publish

# Publish self-contained (~155 MB, no dependencies)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish-full
```

## Architecture

**Two-Window Design:**
- [Hider.xaml.cs](SubtitleHider/Hider.xaml.cs) - Transparent overlay window (standard WPF `Window`). This is the primary window shown on startup. It has no title bar (`WindowStyle="None"`) and handles right-click for the context menu.
- [MainWindow.xaml.cs](SubtitleHider/MainWindow.xaml.cs) - Compact settings panel for opacity control. Only opened via Hider's right-click context menu. Receives the Hider instance via constructor injection. Window size is 160×200 with vertical StackPanel layout.

**Startup Flow:** [App.xaml.cs](SubtitleHider/App.xaml.cs) overrides `OnStartup` to create and show Hider directly. There is no `StartupUri` in App.xaml.

**Persistence:** [WindowSettings.cs](SubtitleHider/WindowSettings.cs) handles loading/saving window position, size, and opacity to a JSON file in `%LocalAppData%/SubtitleHider/settings.json`.

**Window Resizing:** Since `WindowStyle="None"` disables native edge resizing, [Hider.xaml.cs](SubtitleHider/Hider.xaml.cs) implements manual edge resizing using Win32 `SendMessage` with `WM_SYSCOMMAND` (see `Window_MouseMove` and `Window_MouseLeftButtonDown`). A 6-pixel edge zone detects resize vs drag intent.

**Always on Top:** A `DispatcherTimer` calls Win32 `SetWindowPos(HWND_TOPMOST)` every 500ms to ensure the Hider stays above video players in fullscreen mode.

**Visual Feedback:** The Hider window border (`WindowBorder`) is transparent by default and turns yellow on `MouseEnter` to indicate the window boundaries. It turns transparent again on `MouseLeave`.

**Key Window Properties:**
- Hider: `AllowsTransparency="True"`, `WindowStyle="None"`, `ResizeMode="CanResizeWithGrip"`, `Topmost=true`
- Default overlay size: 75px height × 1125px width with black background
- Settings window: `ResizeMode="NoResize"`, `WindowStartupLocation="Manual"`, positioned above Hider

**Dependencies:**
- .NET 8 (no external NuGet packages)

## Important Notes

- `MainWindow` requires a `Hider` instance passed to its constructor.
- The Hider window handles all user interaction via mouse events. `MouseLeftButtonDown` distinguishes between dragging (center area) and resizing (6px edge zone) using Win32 `SendMessage`.
- Settings are saved on window close. The first run uses default values (bottom-center of screen).
- The yellow border visibility is controlled by `Window_MouseEnter` / `Window_MouseLeave` events in Hider.xaml.cs.
