using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Shapes;
using System.IO;

namespace SpotifyTray;

public partial class NowPlayingWindow : Window
{
    // Constants
    private const int WindowOffsetX = 10;
    private const int WindowOffsetY = 60;
    private const string PositionSettingsKey = "WindowPosition";
    
    private readonly MediaController _media;

    public NowPlayingWindow(MediaController media)
    {
        InitializeComponent();
        _media = media;
        _media.MediaChanged += UpdateDisplay;
        
        // Add keyboard shortcuts
        KeyDown += OnKeyDown;
        
        // Load saved position or use default
        LoadWindowPosition();
        
        UpdateDisplay();
    }

    private void LoadWindowPosition()
    {
        try
        {
            var savedPosition = Properties.Settings.Default.WindowPosition;
            if (!string.IsNullOrEmpty(savedPosition))
            {
                var parts = savedPosition.Split(',');
                if (parts.Length == 2 && 
                    double.TryParse(parts[0], out var left) && 
                    double.TryParse(parts[1], out var top))
                {
                    // Ensure window is visible on screen
                    var workArea = SystemParameters.WorkArea;
                    if (left >= workArea.Left && left + Width <= workArea.Right &&
                        top >= workArea.Top && top + Height <= workArea.Bottom)
                    {
                        Left = left;
                        Top = top;
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadWindowPosition error: {ex.Message}");
        }

        // Default position near system tray
        var defaultWorkArea = SystemParameters.WorkArea;
        Left = defaultWorkArea.Right - Width - WindowOffsetX;
        Top = defaultWorkArea.Bottom - Height - WindowOffsetY;
    }

    private void SaveWindowPosition()
    {
        try
        {
            Properties.Settings.Default.WindowPosition = $"{Left},{Top}";
            Properties.Settings.Default.Save();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SaveWindowPosition error: {ex.Message}");
        }
    }

    private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        switch (e.Key)
        {
            case System.Windows.Input.Key.Space:
                PlayPause(null, null);
                e.Handled = true;
                break;
            case System.Windows.Input.Key.Right:
                Next(null, null);
                e.Handled = true;
                break;
            case System.Windows.Input.Key.Left:
                Prev(null, null);
                e.Handled = true;
                break;
            case System.Windows.Input.Key.Escape:
                Hide();
                e.Handled = true;
                break;
        }
    }

    private async void UpdateDisplay()
    {
        try
        {
            var (title, artist, album, cover) =
                await _media.GetNowPlayingAsync();

            Dispatcher.Invoke(() =>
            {
                TitleText.Text = string.IsNullOrEmpty(title) ? "No media playing" : title;
                ArtistText.Text = artist ?? "";
                AlbumText.Text = album ?? "";
                
                // Update play/pause button icon
                var isPlaying = _media.IsPlaying();
                PlayPauseIcon.Data = Geometry.Parse(isPlaying 
                    ? "M7,5 L10,5 L10,19 L7,19 Z M14,5 L17,5 L17,19 L14,19 Z"  // Pause icon
                    : "M8,5 L19,12 L8,19 Z");   // Play icon

                if (cover != null)
                {
                    try
                    {
                        // Convert System.Drawing.Image to BitmapImage
                        using var ms = new MemoryStream();
                        cover.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        ms.Position = 0;

                        var img = new BitmapImage();
                        img.BeginInit();
                        img.CacheOption = BitmapCacheOption.OnLoad;
                        img.StreamSource = ms;
                        img.EndInit();

                        Cover.Source = img;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Cover conversion error: {ex.Message}");
                        Cover.Source = null;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Cover is null");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateDisplay error: {ex.Message}");
        }
    }

    private async void PlayPause(object? _, RoutedEventArgs? __)
    {
        var task = _media.PlayPause();
        if (task != null) await task;
    }

    private async void Next(object? _, RoutedEventArgs? __)
    {
        var task = _media.Next();
        if (task != null) await task;
    }

    private async void Prev(object? _, RoutedEventArgs? __)
    {
        var task = _media.Previous();
        if (task != null) await task;
    }

    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        SaveWindowPosition();
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        Hide();
    }

    protected override void OnClosed(EventArgs e)
    {
        // Unsubscribe from media changed event to prevent memory leak
        _media.MediaChanged -= UpdateDisplay;
        KeyDown -= OnKeyDown;
        base.OnClosed(e);
    }
}
