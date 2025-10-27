# Floating Clock

A lightweight, customizable clock for Windows users who want to hide their taskbar or need a persistent on-screen time display.

## Purpose

I recently decided I wanted a little bit more screen real estate on my Windows 10 desktop so I set the taskbar to Auto Hide. Unfortunately, as Windows has no other clock in its basic UI, I could no longer quickly glance to the corner of my screen to see the time and date.

This program is a small and lightweight app that displays the current time and date, stays on top of other windows, and intelligently positions itself based on your taskbar configuration.

![Floating Clock Screenshot](https://mcgallag.github.io/floating-clock.png)

_Screenshot of the Floating Clock application in the bottom right of my desktop._

## Features

- **Command Palette**: Press **E** to open an interactive command menu with all available actions
- **Multi-Monitor Support**: Cycle through monitors with keyboard shortcuts
- **Corner Docking**: Automatically dock to any screen corner with taskbar awareness
- **Auto-Hide Taskbar Support**: Detects and adapts to auto-hide taskbars
- **Adaptive Background**: Automatically adjusts transparency based on screen content behind the clock
- **Customizable Appearance**: Configure colors, fonts, date/time formats via settings.ini
- **Position Persistence**: Remembers your preferred corner, position, and monitor across sessions
- **Resolution Change Handling**: Validates and adjusts position when switching monitors or resolutions
- **Debug Mode**: Optional startup diagnostic information

## Usage

The application supports two positioning modes:

### Fixed Position Mode (Default)
The clock automatically docks to a screen corner and stays there. Use hotkeys 1-4 to select which corner.

### Free Position Mode
Click and drag the clock anywhere on screen, or use arrow keys for precise positioning.

Toggle between modes by pressing the **F** key.

## Hotkeys

| Key | Action |
|-----|--------|
| **E** | Open command palette (interactive menu of all commands) |
| **Escape** | Close command palette (if open), or exit application |
| **S** | Toggle seconds display on/off |
| **F** | Toggle between Fixed and Free position modes |
| **1** or **Numpad 1** | Dock to top-left corner |
| **2** or **Numpad 2** | Dock to top-right corner |
| **3** or **Numpad 3** | Dock to bottom-left corner |
| **4** or **Numpad 4** | Dock to bottom-right corner |
| **5** or **Numpad 5** | Cycle to next monitor (if multiple monitors available) |
| **N** | Cycle through corners (1→2→3→4→1) |
| **Arrow Keys** | Move window manually (switches to Free mode) |
| **Shift + Arrow Keys** | Move window in smaller increments (10px instead of 100px) |

## Command Palette

Press **E** to open the command palette - an interactive menu that displays all available commands with visual indicators for the current state.

### Features
- **Visual State Indicators**: Active commands are marked with ✓ (e.g., seconds visibility, current corner position)
- **Keyboard Navigation**: Use ↑/↓ arrow keys to select commands
- **Quick Execution**: Press Enter to execute the selected command
- **Smart Positioning**: The palette automatically positions itself to stay fully visible on screen
- **Customizable Appearance**: Colors, fonts, and sizing can be configured via settings.ini

### Usage
1. Press **E** to open the palette
2. Use **↑** and **↓** to navigate through commands
3. Press **Enter** to execute the selected command
4. Press **Escape** to close the palette without executing

The command palette provides access to all keyboard shortcuts in one convenient location, making it easier to discover and use features without memorizing hotkeys.

## Configuration

The application can be extensively customized by editing the `settings.ini` file in the application directory.

### Window Settings
```ini
[window]
x=3500              # X position (when fixed=0)
y=50                # Y position from bottom (when fixed=0)
width=220           # Window width in pixels
height=88           # Window height in pixels
fixed=1             # 1=Fixed corner mode, 0=Free position mode
fixed_corner=4      # Corner to dock to (1=TopLeft, 2=TopRight, 3=BottomLeft, 4=BottomRight)
monitor=            # Monitor device name (e.g., \\.\DISPLAY1) - auto-detected if empty
debug=0             # 1=Show taskbar detection info on startup, 0=Silent
```

### Background Settings
```ini
[background]
color=#99000000                    # Background color with alpha (AARRGGBB format)
auto_brightness_adjustment=1       # 1=Enable adaptive transparency, 0=Disable
threshold_change=0.05              # Minimum brightness change to trigger update
threshold_min=0.05                 # Minimum brightness threshold
threshold_max=0.47                 # Maximum brightness threshold
alpha_max=0.7                      # Maximum background opacity
alpha_min=0.1                      # Minimum background opacity
damping=0.1                        # Smoothing factor for brightness changes
```

### Date Display Settings
```ini
[date]
show=1                        # 1=Show date, 0=Hide date
x=0                          # Horizontal offset
y=0                          # Vertical offset
size=16                      # Font size in pixels
format=dd/MM/yyyy            # Date format (C# DateTime format string)
color=#15fc11                # Text color in hex
vertical_alignment=Top       # Top, Center, or Bottom
horizontal_alignment=Center  # Left, Center, or Right
```

### Time Display Settings
```ini
[time]
x=0                          # Horizontal offset
y=0                          # Vertical offset
size=42                      # Font size in pixels
format=HH:mm                 # Time format (C# DateTime format string)
color=#15fc11                # Text color in hex
vertical_alignment=Top       # Top, Center, or Bottom
horizontal_alignment=Left    # Left, Center, or Right
```

### Seconds Display Settings
```ini
[seconds]
show=1                       # 1=Show seconds, 0=Hide seconds
size=15                      # Font size in pixels
x=5                          # Horizontal offset
y=-7                         # Vertical offset
color=#15fc11                # Text color in hex
vertical_alignment=Top       # Top, Center, or Bottom
horizontal_alignment=Center  # Left, Center, or Right
```

### Font Settings
```ini
[font]
family=Serif LED Board-7     # Font family name (must be installed on system)
```

### Command Palette Settings
```ini
[command_palette]
background_color=#CC000000       # Background color with alpha (AARRGGBB format)
text_color=#FFFFFF               # Text color in hex
selected_background=#40FFFFFF    # Selected item background color with alpha
selected_text_color=#FFFFFF      # Selected item text color
font_family=Consolas             # Font family (monospace recommended)
font_size=14                     # Font size in pixels
width=400                        # Palette window width in pixels
padding=10                       # Border padding in pixels
item_padding=5                   # Padding between items in pixels
show_icons=1                     # 1=Show ✓ indicators, 0=Hide indicators
```

**Note:** All settings in the `[command_palette]` section are optional. If not specified or if the section is missing, the application will use the default values shown above.

**Color Format:** Colors use ARGB hex format (Alpha-Red-Green-Blue). For example:
- `#CC000000` = 80% transparent black (CC=80% alpha)
- `#FFFFFF` = Solid white
- `#40FFFFFF` = 25% transparent white (40=25% alpha)

## Installation & Building

### Prerequisites
- Visual Studio 2017 or later
- .NET Framework 4.6.1 or later

### Building from Source
1. Clone the repository
2. Open `FloatingClock.sln` in Visual Studio
3. Use NuGet Package Manager to restore packages:
   ```
   Install-Package ini-parser
   ```
4. Build the solution (F6)
5. Run the application (F5)

The compiled executable and `settings.ini` will be in the `bin/Debug` or `bin/Release` folder.

### Suggested Font

For the authentic LED clock look shown in screenshots, download and install:
- **Serif LED Board-7** font: https://www.1001fonts.com/serif-led-board-7-font.html

After installing the font, it will be used automatically by the application.
