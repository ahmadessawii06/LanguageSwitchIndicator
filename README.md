# Language Switch Indicator

A small Windows desktop utility that displays a macOS-inspired indicator when your keyboard language changes. Built with C# and WPF, with no browser or web server.

## Features

- Dark, translucent card with rounded corners, a keyboard icon, and a soft shadow.
- Smooth fade and scale animation; rapid language changes renew the popup without flashing.
- Four animation presets with instant previews: Mac style, Very fast, Normal, and Slow.
- Custom display and fade durations, plus configurable popup placement and offsets.
- System tray controls for settings, preview, enable/disable, and exit.
- Local settings stored in `%LOCALAPPDATA%\LanguageSwitchIndicator\settings.json`.

## Download and use

Download the ZIP from [Releases](https://github.com/ahmadessawii06/LanguageSwitchIndicator/releases/latest), extract **all files** into one folder, and run `LanguageSwitchIndicator.exe`.

Requirements: Windows 10 or 11 and **.NET Desktop Runtime 8**. The small download does not bundle .NET. Select **Desktop Runtime** on the [.NET 8 download page](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

Change your keyboard language to see the popup, or use **Preview popup** in Settings. Right-click the tray icon for **Open Settings**, **Enable/Disable Indicator**, **Preview Popup**, or **Exit**. Close any previous instance before starting another copy.

### Animation presets

Open **Settings → Animations → خيارات الحركة الجاهزة**. Selecting a preset saves it immediately and displays a preview.

| Preset | Motion | Display timer | Fade in | Fade out |
| --- | --- | --- | --- | --- |
| شكل ماك — Mac style | Fade + gentle scale | 1,000 ms | 180 ms | 250 ms |
| سريع جدًا — Very fast | Fade | 350 ms | 50 ms | 80 ms |
| طبيعي — Normal | Fade | 700 ms | 120 ms | 200 ms |
| بطيء — Slow | Fade + gentle scale | 1,600 ms | 350 ms | 500 ms |

The display timer starts when the popup is shown; fade-out follows it. Editing a timing field switches the selector to **مخصص — Custom**. Manual changes are saved when the Settings window closes.

## Build from source

Install the **.NET 8 SDK** on Windows, then run:

```powershell
git clone https://github.com/ahmadessawii06/LanguageSwitchIndicator.git
cd LanguageSwitchIndicator
dotnet restore LanguageSwitchIndicator.sln
dotnet build LanguageSwitchIndicator.sln -c Release
dotnet test LanguageSwitchIndicator.sln
dotnet run --project LanguageSwitchIndicator.App
```

Publish the small version that uses an installed .NET Desktop Runtime:

```powershell
dotnet publish LanguageSwitchIndicator.App -c Release --self-contained false -o publish
```

To bundle .NET for a Windows x64 machine instead (a larger download):

```powershell
dotnet publish LanguageSwitchIndicator.App -c Release -r win-x64 --self-contained true -o publish-win-x64
```

Distribute the entire output folder; the EXE alone is insufficient.

## Project structure

```text
LanguageSwitchIndicator.App/
  Models/       Settings and animation presets
  Native/       Windows API bindings
  Services/     Language monitoring, settings, popup and tray management
  Views/        Popup and settings windows
LanguageSwitchIndicator.Tests/
```

Build outputs, distribution archives, and IDE files are excluded from version control.

## Current limitations

- Language detection polls the foreground window's keyboard layout every 80 ms. It currently maps English (US), Arabic (Saudi Arabia), French, and German; other layouts fall back to English.
- The card is a translucent gradient, not native macOS material or a background blur effect.
- Placement uses the primary display's work area, except for the near-mouse option.
- Some settings are placeholders, including launch at startup, theme/color editing, and language mapping controls. The sidebar buttons are currently visual labels; scroll to reach each section.

## الاستخدام بالعربية

حمّل ملف ZIP من قسم **Releases**، وفكّ كل الملفات في مجلد واحد، ثم شغّل `LanguageSwitchIndicator.exe`. تحتاج Windows 10 أو 11 و **.NET Desktop Runtime 8**.

بدّل لغة الكيبورد لعرض المؤشر. من الإعدادات، مرّر إلى **Animations** واختر **شكل ماك** أو **سريع جدًا** أو **طبيعي** أو **بطيء**. سيُحفظ الاختيار وتظهر معاينة مباشرة. لإغلاق التطبيق بالكامل، اختر **Exit** من أيقونته بجانب الساعة.
