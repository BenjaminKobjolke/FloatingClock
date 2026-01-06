# Theme System Documentation

## Overview

The FloatingClock Settings Window supports automatic theme detection based on the Windows system theme, with an option for users to override the setting.

## Theme Options

- **Auto (follow system)**: Automatically detects Windows light/dark theme
- **Light**: Forces light theme regardless of system setting
- **Dark**: Forces dark theme regardless of system setting

## Implementation

### Files Involved

| File | Purpose |
|------|---------|
| `ThemeHelper.cs` | Theme detection and effective theme calculation |
| `SettingsManager.cs` | Theme preference persistence |
| `SettingsWindow.xaml` | Theme dropdown UI |
| `SettingsWindow.xaml.cs` | Theme application logic |
| `settings.ini` | Theme setting storage (`[theme]` section) |
| `lang/en.json` | Localization strings |

### Theme Detection

Windows theme is detected via registry:
```
HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize
Key: AppsUseLightTheme (1 = Light, 0 = Dark)
```

### Dark Mode Styling

In dark mode, the following elements are styled:
- Window background: `#2D2D2D`
- All text: White (`#FFFFFF`)
- TextBox backgrounds: `#3C3C3C`
- TextBox borders: `#505050`
- Separators: `#444444`

## Known Limitations

### ComboBox Dropdowns Cannot Be Styled Dark

**Issue**: In WPF, ComboBox dropdown popups use system-level styling that is difficult to override without completely re-templating the control.

**Behavior**: ComboBoxes retain their default light theme styling even when the rest of the Settings Window is in dark mode.

**Attempted Solutions**:

1. **ItemContainerStyle with Background/Foreground setters**
   - Result: Did not apply to dropdown items correctly

2. **ItemContainerStyle with Triggers (IsHighlighted)**
   - Result: Triggers did not differentiate between closed and open states

3. **DropDownOpened/DropDownClosed event handlers**
   - Approach: Change `ComboBox.Foreground` dynamically when dropdown opens/closes
   - Result: Did not affect the dropdown item colors, only the selection box

4. **Direct ComboBoxItem styling via iteration**
   - Result: Styles applied but overridden by WPF's internal templating

**Final Decision**: Keep ComboBoxes with default light theme styling for readability. This provides a consistent, readable experience without fighting WPF's complex ComboBox templating.

## Settings Storage

Theme preference is stored in `settings.ini`:
```ini
[theme]
mode=auto
```

Valid values: `auto`, `light`, `dark`

## Adding New Themed Elements

To add theme support to new controls in `SettingsWindow.xaml.cs`:

```csharp
private void ApplyTheme(bool isDark)
{
    Color textColor = isDark ? Colors.White : Color.FromRgb(51, 51, 51);

    // Apply to your controls
    ApplyToAllControls<YourControl>(MainSettingsPanel, ctrl =>
    {
        ctrl.Foreground = new SolidColorBrush(textColor);
    });
}
```
