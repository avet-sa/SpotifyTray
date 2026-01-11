# SpotifyTray

> A minimal, elegant system tray application for Windows that displays Spotify's now-playing information with album art and integrated media controls.

![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6?logo=windows)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![License](https://img.shields.io/badge/license-MIT-green)
![Build](https://github.com/avet-sa/SpotifyTray/actions/workflows/build-and-release.yml/badge.svg)
![Release](https://img.shields.io/github/v/release/avet-sa/SpotifyTray?display_name=tag)

## Features

- **Dynamic Album Art** - Tray icon automatically updates with the current album cover
- **Media Controls** - Play/pause, skip tracks, and control playback directly from the tray
- **Rich Now Playing Display** - Elegant popup showing track title, artist, album, and artwork
- **Lightweight & Fast** - Minimal resource usage, runs silently in the background
- **Real-time Updates** - Automatically syncs with Spotify's playback state
- **Intuitive Controls** - Left-click to toggle popup, right-click for quick actions
- **Self-Contained** - No dependencies or .NET runtime installation required

## Screenshots

<img width="425" height="188" alt="image" src="https://github.com/user-attachments/assets/458ba8e5-c4c5-4663-bef0-1c570a556654" />


## Quick Start

### Download & Run (Recommended)

1. Download the latest release ZIP for your architecture from [Releases](../../releases):
  - `SpotifyTray-win-x64.zip` (64-bit)
  - `SpotifyTray-win-x86.zip` (32-bit)
2. Extract the entire ZIP to a location of your choice (e.g., `C:\Program Files\SpotifyTray\`)
3. Run `SpotifyTray.exe` from the extracted folder
4. The app will appear in your system tray
5. Start playing music in Spotify

> **Important**: Keep all files from the publish folder together. The application requires supporting files alongside the executable to run properly.

### Release Assets

Each release includes ZIP archives that contain the full publish output. Download and extract the one that matches your system:

- `SpotifyTray-win-x64.zip` – recommended for most modern systems
- `SpotifyTray-win-x86.zip` – for legacy 32-bit systems

Each ZIP is accompanied by a `.sha256` checksum file for integrity verification. To verify:

```powershell
Get-FileHash -Algorithm SHA256 .\SpotifyTray-Publish-win-x64.zip
```
Compare the hash to the contents of `.sha256`.

### Installation Options

<details>
<summary><b>Add to Windows Startup</b></summary>

**Method 1: Startup Folder**
1. Press `Win + R` and type `shell:startup`
2. Create a shortcut to `SpotifyTray.exe` (from your extracted publish folder) in the startup folder

**Method 2: Task Scheduler** (More reliable)
1. Open Task Scheduler
2. Create a new task with these settings:
   - Trigger: At log on
   - Action: Start `SpotifyTray.exe` (full path to your extracted folder)
   - Run whether user is logged on or not
</details>

<details>
<summary><b>Build from Source</b></summary>

**Prerequisites:**
- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code (optional)

**Build Steps:**
```bash
# Clone the repository
git clone https://github.com/avet-sa/SpotifyTray.git
cd SpotifyTray

# Build self-contained application (x64)
dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true --self-contained true

# Optional: build x86
dotnet publish -c Release -r win-x86 /p:PublishSingleFile=true --self-contained true

# Output location:
# bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\
```

The entire `publish` folder contains all necessary files for distribution. Distribute the full folder, not just the `.exe`.

## CI/CD

Releases are automated via GitHub Actions:
- On pushes to `master`: builds run and artifacts are uploaded.
- On tags like `v1.0.2`: x64/x86 ZIPs are built, checksums generated, and a Release is published with assets.
- Manual runs supported via the Actions tab (`Run workflow`).

To create a release:

```powershell
git tag -a v1.0.2 -m "Release v1.0.2"
git push origin v1.0.2
```
</details>

## Usage Guide

### Basic Controls

| Action | Result |
|--------|--------|
| **Left-click** tray icon | Toggle now playing popup |
| **Right-click** tray icon | Open context menu |
| **Previous button** | Skip to previous track |
| **Play/Pause button** | Toggle playback |
| **Next button** | Skip to next track |

### Context Menu Options

- **Open Spotify** - Launch or focus the Spotify window
- **Exit** - Close SpotifyTray

### Behavior

- **Auto-hide**: Popup automatically hides when Spotify stops playing
- **Auto-update**: Album art and track info update in real-time
- **Spotify detection**: Works automatically when Spotify is running

## Requirements

- **OS**: Windows 10 (1809+) or Windows 11
- **Spotify**: Desktop application (not web player)
- **Architecture**: 64-bit or 32-bit Windows (choose matching release ZIP)

> **Note**: The Microsoft Store version of Spotify may have limited media control support. The desktop version from spotify.com is recommended.

## Architecture

SpotifyTray uses Windows' native media integration APIs for seamless Spotify control:

```
┌─────────────────────────────────────┐
│      SpotifyTray Application        │
├─────────────────────────────────────┤
│  • WPF User Interface               │
│  • System Tray Integration          │
│  • Media Session Monitor            │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│  Windows Media Control API (GSMTC)  │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│         Spotify Desktop App         │
└─────────────────────────────────────┘
```

### Technology Stack

- **Framework**: .NET 8.0 / WPF
- **Media Integration**: Windows.Media.Control (GSMTC)
- **Graphics**: System.Drawing
- **Distribution**: Self-contained application package

## Project Structure

```
SpotifyTray/
├── App.xaml                # Application resources
├── App.xaml.cs             # Entry point & tray icon logic
├── MainWindow.xaml         # Main window (hidden)
├── MainWindow.xaml.cs      # Main window code-behind
├── NowPlayingWindow.xaml   # Now playing popup UI
├── NowPlayingWindow.xaml.cs # Popup logic
├── MediaController.cs      # Spotify media session interface
├── SpotifyTray.csproj      # Project configuration
└── README.md               # Documentation
```

## Troubleshooting

<details>
<summary><b>App won't start or crashes immediately</b></summary>

- Ensure you've extracted the **entire publish folder**, not just the .exe file
- Verify all supporting files are in the same directory as SpotifyTray.exe
- Try running as administrator
- Check Windows Event Viewer for error details
</details>

<details>
<summary><b>Album art not showing in tray</b></summary>

- Ensure Spotify is playing music
- Check that album artwork is available for the track
- Try restarting SpotifyTray
</details>

<details>
<summary><b>Media controls not responding</b></summary>

- Verify you're using the desktop version of Spotify (not web player)
- Restart both Spotify and SpotifyTray
- Check Windows media control permissions
</details>

<details>
<summary><b>App doesn't start with Windows</b></summary>

- Verify the shortcut in the Startup folder points to the correct location
- Ensure the entire publish folder hasn't been moved or deleted
- Check Task Scheduler task configuration
- Run SpotifyTray as administrator once to ensure proper registration
</details>

<details>
<summary><b>High CPU/Memory usage</b></summary>

This shouldn't happen under normal circumstances. If you experience this:
- Check for multiple instances running
- Report the issue with details about your system
</details>

## Contributing

Contributions are welcome! Here's how you can help:

1. **Report Bugs** - Open an issue with detailed reproduction steps
2. **Suggest Features** - Share your ideas in the issues section
3. **Submit Pull Requests** - Fork, create a feature branch, and submit a PR

### Development Setup

```bash
git clone https://github.com/avet-sa/SpotifyTray.git
cd SpotifyTray
dotnet restore
dotnet build
```

## Roadmap

Potential future enhancements:

- [ ] Global hotkey support
- [ ] Customizable themes and colors
- [ ] Multi-monitor support with configurable popup position
- [ ] Support for other music players (YouTube Music, Apple Music, etc.)
- [ ] Notification system for track changes
- [ ] Mini-player mode
- [ ] Lyrics integration

## FAQ

**Q: Why do I need to download the entire folder instead of just the .exe?**  
A: The application is self-contained and includes all necessary .NET runtime files and dependencies. These supporting files must be kept together with the executable.

**Q: Does this work with Spotify Web Player?**  
A: No, it requires the desktop application due to Windows media API limitations.

**Q: Does this collect any data?**  
A: No, SpotifyTray runs entirely locally and doesn't collect or transmit any data.

**Q: Can I use this with multiple Spotify accounts?**  
A: Yes, it works with whichever Spotify instance is currently playing.

**Q: Does this affect Spotify's performance?**  
A: No, it uses Windows' media APIs without directly interfacing with Spotify.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built with Windows' Global System Media Transport Controls API
- Inspired by the need for a lightweight Spotify companion

## Support

- **Issues**: [GitHub Issues](../../issues)
- **Discussions**: [GitHub Discussions](../../discussions)

---

<p align="center">
  Made with love for Spotify lovers
  <br>
  <sub>Not affiliated with Spotify AB</sub>
</p>
