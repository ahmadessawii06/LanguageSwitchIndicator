<h1>
  <img src="LanguageSwitchIndicator.App/Assets/Icons/language_switch_64.png" alt="" width="40" height="40">
  Language Switch Indicator
</h1>

**A little clarity with every language switch.**<br>
A macOS-inspired keyboard language popup for Windows. Follows the screen where you type.

<p>
  <img src="https://img.shields.io/badge/Windows-10%20%2F%2011-0078D4?style=flat-square" alt="Windows 10 and 11">
  <img src="https://img.shields.io/badge/.NET-8-512BD4?style=flat-square" alt=".NET 8">
  <img src="https://img.shields.io/badge/Built%20with-C%23%20%C2%B7%20WPF-343A46?style=flat-square" alt="Built with C# and WPF">
</p>

[**Download for Windows ↗**](https://github.com/ahmadessawii06/LanguageSwitchIndicator/releases/latest) · [Get started](#quick-start) · [Build from source](#build-from-source)

## Dark or light

<img src="docs/images/themes.png" alt="Dark and Light indicator themes, both displaying English" width="660">

Choose **Dark**, **Light**, or **System** to follow Windows. Changes preview immediately and save automatically.

## Your screen, your position

- **Multiple monitors:** one popup, on the screen where you type.
- **Placement:** center, edges, corners, or beside the text caret.
- **Triggers:** language changes, caret placement, or both.
- **Animation:** four timing presets and custom fade durations.

## Quick start

1. Download and extract the ZIP from [Releases](https://github.com/ahmadessawii06/LanguageSwitchIndicator/releases/latest).
2. Run `LanguageSwitchIndicator.exe` and switch your keyboard language.
3. Right-click the tray icon beside the clock for **Settings**, preview, or exit.

**Requires:** Windows 10/11 and [.NET Desktop Runtime 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

## Build from source

With **.NET 8 SDK** on Windows, run from the repository folder:

```powershell
dotnet run --project LanguageSwitchIndicator.App
```

Run tests:

```powershell
dotnet test LanguageSwitchIndicator.Tests
```

Exit the running app from the tray before rebuilding.

## Settings

### General

<img src="docs/images/settings-general.png" alt="General settings with enable, pause, and popup trigger controls" width="720">

### Appearance

<img src="docs/images/settings-appearance.png" alt="Appearance settings with five sizes, theme selection, dimensions, and live preview" width="620">

### Animations

<img src="docs/images/settings-animations.png" alt="Animation settings with timing presets, display duration, fade in, and fade out" width="720">

## Five sizes

Choose your size in **Settings → Appearance**. Text and keyboard icon resize together.

<img src="docs/images/sizes.png" alt="Actual indicator sizes: Extra small, Small, Medium, Large, and Extra large" width="1000">
