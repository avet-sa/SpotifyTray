# SpotifyTray

A minimal, elegant system tray application for Windows that displays Spotify's now playing information with album art and media controls.

## Features

- 🎵 **System Tray Integration** - Displays current song's album art as the tray icon
- 🎨 **Modern Dark UI** - Beautiful dark-themed widget with rounded corners and shadows
- 🎮 **Media Controls** - Play/pause, previous, and next track controls
- 🖼️ **Album Artwork** - Shows full album cover with song, artist, and album information
- 🎯 **Smart Visibility** - Only appears when Spotify is actively playing
- 🚀 **Lightweight** - Minimal resource usage, runs quietly in the background
- 🖱️ **Context Menu** - Right-click to open Spotify or exit the app

## Screenshots

The widget displays:
- Album cover art (110x110)
- Song title
- Artist name
- Album name
- Media control buttons (Previous, Play/Pause, Next)

## Requirements

- Windows 10/11
- .NET 8.0 Runtime (or SDK)
- Spotify Desktop App

## Installation

### Option 1: Build from Source

1. Clone the repository:
```bash
git clone <repository-url>
cd SpotifyTray
```

2. Build the project:
```bash
dotnet build -c Release
```

3. Run the application:
```bash
dotnet run
```

### Option 2: Self-Contained Executable

Build a standalone executable that includes the .NET runtime:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The executable will be in: `bin\Release\net8.0-windows\win-x64\publish\SpotifyTray.exe`

## Usage

1. **Launch the app** - The app runs in the background
2. **Play music on Spotify** - The tray icon appears with the album cover
3. **Left-click** the tray icon to show/hide the now playing widget
4. **Right-click** the tray icon for options:
   - Open Spotify
   - Exit

The widget automatically:
- Updates when the song changes
- Hides when you click away
- Disappears when Spotify is closed

## Run on Startup

### Windows Startup Folder Method:

1. Press `Win + R` and type `shell:startup`
2. Create a shortcut to `SpotifyTray.exe` in this folder

### Alternative: Task Scheduler

For more control, use Windows Task Scheduler to run the app at login.

## How It Works

- Uses Windows' **Global System Media Transport Controls (GSMTC)** API
- Monitors media session changes for Spotify
- Dynamically generates tray icons from album artwork
- WPF-based modern UI with custom styling

## Keyboard Shortcuts

All media controls work through the Spotify integration:
- **Previous Track** - Skip to previous song
- **Play/Pause** - Toggle playback
- **Next Track** - Skip to next song

## Development

### Project Structure

```
SpotifyTray/
├── App.xaml.cs              # Application entry point, tray icon logic
├── MainWindow.xaml          # Hidden main window
├── NowPlayingWindow.xaml    # Now playing widget UI
├── MediaController.cs       # GSMTC integration
└── SpotifyTray.csproj       # Project configuration
```

### Technologies

- **WPF** - Windows Presentation Foundation for UI
- **Windows.Media.Control** - Media session integration
- **System.Drawing** - Image processing for tray icons
- **.NET 8.0** - Target framework

## Contributing

Contributions are welcome! Feel free to:
- Report bugs
- Suggest features
- Submit pull requests

## License

This project is open source and available under the MIT License.

## Credits

Created with ❤️ for Spotify users who want a clean, minimal now playing experience.

## Troubleshooting

**Tray icon doesn't appear:**
- Make sure Spotify is running and playing music
- Check that Spotify's media controls are enabled

**Widget doesn't show album art:**
- Ensure you have an active internet connection
- Some tracks may not have album artwork

**App doesn't start:**
- Verify .NET 8.0 Runtime is installed
- Run from command line to see error messages

## Future Enhancements

- [ ] Lyrics display
- [ ] Customizable themes
- [ ] Hotkey support
- [ ] Multiple monitor support
- [ ] Mini/compact mode
