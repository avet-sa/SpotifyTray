# 🎵 SpotifyTray

> A minimal, elegant system tray application for Windows that displays Spotify's now-playing information with album art and integrated media controls.

![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6?logo=windows)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![License](https://img.shields.io/badge/license-MIT-green)

## ✨ Features

- **🎨 Dynamic Album Art** - Tray icon automatically updates with the current album cover
- **🎮 Media Controls** - Play/pause, skip tracks, and control playback directly from the tray
- **📊 Rich Now Playing Display** - Elegant popup showing track title, artist, album, and artwork
- **⚡ Lightweight & Fast** - Minimal resource usage, runs silently in the background
- **🔄 Real-time Updates** - Automatically syncs with Spotify's playback state
- **🖱️ Intuitive Controls** - Left-click to toggle popup, right-click for quick actions
- **📦 Self-Contained** - No dependencies or .NET runtime installation required

## 📸 Screenshots

<img width="425" height="188" alt="image" src="https://github.com/user-attachments/assets/0180c167-aae7-4edd-a500-fe0d8e5bd4c9" />


## 🚀 Quick Start

### Download & Run (Recommended)

1. Download the latest `SpotifyTray.exe` from [Releases](../../releases)
2. Double-click to launch
3. The app will appear in your system tray
4. Start playing music in Spotify!

### Installation Options

<details>
<summary><b>Add to Windows Startup</b></summary>

**Method 1: Startup Folder**
1. Press `Win + R` and type `shell:startup`
2. Create a shortcut to `SpotifyTray.exe` in this folder

**Method 2: Task Scheduler** (More reliable)
1. Open Task Scheduler
2. Create a new task with these settings:
   - Trigger: At log on
   - Action: Start `SpotifyTray.exe`
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

# Build single-file executable
dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true /p:SelfContained=true

# Output location:
# bin\Release\net8.0-windows\win-x64\publish\SpotifyTray.exe
```
</details>

## 📖 Usage Guide

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

## 🔧 Requirements

- **OS**: Windows 10 (1809+) or Windows 11
- **Spotify**: Desktop application (not web player)
- **Architecture**: 64-bit Windows

> **Note**: The Microsoft Store version of Spotify may have limited media control support. The desktop version from spotify.com is recommended.

## 🏗️ Architecture

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
- **Distribution**: Self-contained single-file executable

## 📁 Project Structure

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

## 🐛 Troubleshooting

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

- Verify the shortcut in the Startup folder is valid
- Check Task Scheduler task configuration
- Run SpotifyTray as administrator once to ensure proper registration
</details>

<details>
<summary><b>High CPU/Memory usage</b></summary>

This shouldn't happen under normal circumstances. If you experience this:
- Check for multiple instances running
- Report the issue with details about your system
</details>

## 🤝 Contributing

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

## 📋 Roadmap

Potential future enhancements:

- [ ] Global hotkey support
- [ ] Customizable themes and colors
- [ ] Multi-monitor support with configurable popup position
- [ ] Support for other music players (YouTube Music, Apple Music, etc.)
- [ ] Notification system for track changes
- [ ] Mini-player mode
- [ ] Lyrics integration

## ❓ FAQ

**Q: Does this work with Spotify Web Player?**  
A: No, it requires the desktop application due to Windows media API limitations.

**Q: Does this collect any data?**  
A: No, SpotifyTray runs entirely locally and doesn't collect or transmit any data.

**Q: Can I use this with multiple Spotify accounts?**  
A: Yes, it works with whichever Spotify instance is currently playing.

**Q: Does this affect Spotify's performance?**  
A: No, it uses Windows' media APIs without directly interfacing with Spotify.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with Windows' Global System Media Transport Controls API
- Inspired by the need for a lightweight Spotify companion

## 📞 Support

- **Issues**: [GitHub Issues](../../issues)
- **Discussions**: [GitHub Discussions](../../discussions)

---

<p align="center">
  Made with ❤️ for Spotify lovers
  <br>
  <sub>Not affiliated with Spotify AB</sub>
</p>
