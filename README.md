# Bodycombat Spectator Camera Mod (Mono Version)

A MelonLoader mod for Les Mills Bodycombat that adds a third-person camera view for desktop viewers.

**This is the MONO version** - use this if your game does NOT have a `GameAssembly.dll` in the game folder.

## Features

- Third-person camera that follows behind the player
- Smooth camera movement
- Adjustable distance, height, and horizontal offset
- Real-time adjustments with keyboard shortcuts
- Settings saved between sessions

## Requirements

1. **Les Mills Bodycombat** (Steam version)
2. **MelonLoader v0.5.7 or v0.6.x** (or newer)
3. **.NET Framework 4.7.2** (usually pre-installed on Windows 10/11)
4. **Visual C++ Redistributables 2015-2022**

## How to Check if Your Game is Mono or IL2CPP

Look in your game folder:
- **If you see `GameAssembly.dll`** → IL2CPP (use the other version)
- **If you see `GameName_Data/Managed/Assembly-CSharp.dll`** → Mono (use this version)

## Installation

### Installing MelonLoader

1. Download MelonLoader from: https://github.com/LavaGang/MelonLoader/releases
2. Run the installer and select your game's EXE
3. For Mono games, MelonLoader v0.5.7 is very stable, but v0.7.x also works
4. Launch the game once to let MelonLoader initialize

### Installing the Mod

1. Build the mod (see below) or download the pre-built DLL
2. Copy `BodycombatSpectatorCam.dll` to your game's `Mods` folder:
   ```
   Steam/steamapps/common/LES MILLS BODYCOMBAT/Mods/
   ```
3. Launch the game

## Building from Source

### Prerequisites

- Visual Studio 2022 (or VS Code with C# extension)
- .NET Framework 4.7.2 SDK (included with Visual Studio)

### Steps

1. **Install MelonLoader on your game first**

2. **Update the game path** in `BodycombatSpectatorCam.csproj`:
   ```xml
   <GamePath>C:\Program Files (x86)\Steam\steamapps\common\LES MILLS BODYCOMBAT</GamePath>
   ```

3. **Verify the Data folder name** - Update the paths if your game's data folder has a different name:
   ```xml
   <HintPath>$(GamePath)\BodyCombat_Data\Managed\UnityEngine.dll</HintPath>
   ```

4. **Build the project**:
   ```bash
   dotnet build -c Release
   ```

5. **Copy the DLL** from `bin/Release/net472/BodycombatSpectatorCam.dll` to the game's `Mods` folder

## Controls

| Key | Action |
|-----|--------|
| **F8** | Toggle spectator camera on/off |
| **Numpad +** | Increase camera distance |
| **Numpad -** | Decrease camera distance |
| **Page Up** | Raise camera height |
| **Page Down** | Lower camera height |

## Configuration

Settings are saved in `UserData/MelonPreferences.cfg`:

```ini
[SpectatorCam]
Distance = 2.5        # Distance behind player (meters)
Height = 0.5          # Height offset above head (meters)
Smoothing = 8.0       # Camera smoothness (higher = snappier)
HorizontalOffset = 0.3 # Side offset (positive = right)
ToggleKey = F8        # Key to toggle camera
EnabledByDefault = true
```

## Troubleshooting

### Camera not appearing
- Make sure MelonLoader is properly installed (you should see a console on startup)
- Check the MelonLoader log at `MelonLoader/Logs/Latest.log` for errors
- The mod needs to find the VR camera - if it can't, check the log for "Found VR camera" messages

### Camera position is wrong
- Try adjusting Distance and Height in the config file
- The mod tries to find common VR camera setups - if the game uses unusual naming, the tracking might not be perfect

### Performance issues
- The spectator camera renders an additional view, which has some GPU cost

### References not found when building
- Verify the GamePath in the .csproj points to your actual game installation
- Check that `GameName_Data/Managed/` folder exists and contains the Unity DLLs
- The data folder name must match exactly (e.g., `BodyCombat_Data`)
- If DLLs are missing, try these alternate locations:
  - `MelonLoader/Managed/` (MelonLoader's copies)
  - `Mono/EmbedRuntime/` (older Unity versions)
  - `MelonLoader/net35/`
## How It Works

The mod creates a secondary Unity Camera that:
1. Follows behind and above the player's VR headset
2. Renders to Display 0 (the desktop mirror window)
3. Uses smooth interpolation for pleasant viewing
4. Ignores VR stereo rendering (renders as flat 2D)

## License

MIT License - Feel free to modify and share!

## Credits

- MelonLoader by Lava Gang
- Les Mills XR Bodycombat by Odders Lab
