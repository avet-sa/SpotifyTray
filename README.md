# SpotifyTray

A minimal, elegant system tray application for Windows that shows Spotify's now playing information with album art and media controls.

## Features

* 🎵 **System Tray Integration** – Tray icon shows album art while music is playing
* 🎮 **Media Controls** – Play/pause, previous, and next track directly from the tray
* 🖼️ **Album Artwork** – View song title, artist, album, and cover in a popup
* 🖱️ **Context Menu** – Right-click to open Spotify or exit the app
* 🚀 **Lightweight** – Runs quietly in the background without additional dependencies

## Requirements

* Windows 10 or 11 (64-bit)
* Spotify Desktop App

**Note:** If using the prebuilt executable, no .NET installation is required. The app is self-contained.

## Installation

### Option 1: Use Prebuilt Executable (Recommended)

1. Download `SpotifyTray.exe` from the release or build it as a single-file executable (see below).
2. Double-click to run.
3. Optionally, add to startup by creating a shortcut in the Windows Startup folder (`shell:startup`) or via Task Scheduler.

### Option 2: Build from Source

1. Clone the repository:
```bash
git clone <repository-url>
cd SpotifyTray
```

2. Publish a self-contained single EXE:
```bash
dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true /p:SelfContained=true
```

3. The resulting executable will be in:
```
bin\Release\net8.0-windows\win-x64\publish\SpotifyTray.exe
```

**Tip:** You do not need the .NET SDK on machines running the self-contained EXE.

## Usage

1. **Launch the app** – it runs in the system tray.
2. **Play music in Spotify** – the tray icon will display album art.
3. **Left-click** the tray icon to show or hide the now playing popup.
4. **Right-click** the tray icon for options:
   * Open Spotify
   * Exit the app

The widget automatically updates when the song changes and hides when Spotify is closed.

## How It Works

* Uses Windows' **Global System Media Transport Controls (GSMTC) API**
* Monitors Spotify's media session to update song information in real-time
* Generates tray icons dynamically from album artwork
* WPF-based popup UI with modern styling

## Keyboard / Media Controls

* **Previous Track** – Skip to previous song
* **Play/Pause** – Toggle playback
* **Next Track** – Skip to next song

These controls interact directly with Spotify via Windows' media session API.

## Development

### Project Structure
```
SpotifyTray/
├── App.xaml.cs              # Entry point, tray icon logic
├── NowPlayingWindow.xaml    # Now playing popup UI
├── MediaController.cs       # Spotify media session integration
└── SpotifyTray.csproj       # Project configuration
```

### Technologies

* **WPF** – User interface
* **Windows.Media.Control** – Media session integration
* **System.Drawing** – Tray icon generation
* **.NET 8.0** – Target framework

## Contributing

Contributions are welcome! You can:

* Report bugs
* Suggest features
* Submit pull requests

## License

MIT License – free and open source.