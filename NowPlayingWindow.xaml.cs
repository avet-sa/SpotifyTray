using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;
using System.Windows.Media.Animation;
using System.Diagnostics;

namespace SpotifyTray;

public partial class NowPlayingWindow : Window
{
    // Constants
    private const int WindowOffsetX = 10;
    private const int WindowOffsetY = 60;
    private const int AnimationDurationMs = 90;
    private const string PlayIconUri = "pack://application:,,,/Icons/Play.png";
    private const string PauseIconUri = "pack://application:,,,/Icons/Pause.png";
    
    private readonly MediaController _media;
    private System.Windows.Controls.Image? _coverImage; // Reference to the main cover image
    private System.Windows.Controls.Image? _coverBlurred; // Reference to the blurred cover image
    private System.Windows.Controls.Image? _playPauseImage;
    private double _targetLeft; // Store the target position for animation
    private double _targetTop;
    private bool _suspendPositionTracking;
    private DateTime _lastOutsideHideUtc = DateTime.MinValue;

    public NowPlayingWindow(MediaController media)
    {
        InitializeComponent();
        _media = media;
        _media.MediaChanged += UpdateDisplay;
        
        // Add keyboard shortcuts
        KeyDown += OnKeyDown;
        
        // Load saved position or use default
        LoadWindowPosition();
        
        // Set initial window position to target (will be animated when shown)
        Left = _targetLeft;
        Top = _targetTop;
        
        // Find the cover images
        _coverImage = this.FindName("Cover") as System.Windows.Controls.Image;
        _coverBlurred = this.FindName("CoverBlurred") as System.Windows.Controls.Image;
        _playPauseImage = this.FindName("PlayPauseImage") as System.Windows.Controls.Image;
        
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
                        _targetLeft = left;
                        _targetTop = top;
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
        _targetLeft = defaultWorkArea.Right - Width - WindowOffsetX;
        _targetTop = defaultWorkArea.Bottom - Height - WindowOffsetY;
    }

    private void SaveWindowPosition()
    {
        try
        {
            Properties.Settings.Default.WindowPosition = $"{_targetLeft},{_targetTop}";
            Properties.Settings.Default.Save();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SaveWindowPosition error: {ex.Message}");
        }
    }
    
    public new void Show()
    {
        if (IsVisible) return; // Already visible
        
        // Make sure we're visible before animating
        _suspendPositionTracking = true;
        Opacity = 0;
        Left = _targetLeft;
        Top = _targetTop;
        base.Show();

        var fadeAnimation = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(AnimationDurationMs)
        };
        fadeAnimation.Completed += (s, e) => _suspendPositionTracking = false;
        BeginAnimation(Window.OpacityProperty, fadeAnimation);
    }
    
    public new void Hide()
    {
        if (!IsVisible) return;

        _suspendPositionTracking = true;

        var fadeAnimation = new DoubleAnimation
        {
            From = Opacity,
            To = 0.0,
            Duration = TimeSpan.FromMilliseconds(AnimationDurationMs)
        };
        fadeAnimation.Completed += (s, e) =>
        {
            base.Hide();
            Opacity = 1.0;
            _suspendPositionTracking = false;
        };
        BeginAnimation(Window.OpacityProperty, fadeAnimation);
    }

    public bool WasRecentlyHiddenByOutsideClick(int milliseconds)
    {
        if (_lastOutsideHideUtc == DateTime.MinValue) return false;
        var wasRecent = (DateTime.UtcNow - _lastOutsideHideUtc).TotalMilliseconds <= milliseconds;
        if (wasRecent)
        {
            _lastOutsideHideUtc = DateTime.MinValue;
        }
        return wasRecent;
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

            try
            {
                Dispatcher.Invoke(() =>
                {
                    TitleText.Text = string.IsNullOrEmpty(title) ? "No media playing" : title;
                    ArtistText.Text = artist ?? "";
                    AlbumText.Text = album ?? "";
                    
                    // Update play/pause button icon
                    var isPlaying = _media.IsPlaying();
                    if (_playPauseImage != null)
                    {
                        _playPauseImage.Source = new BitmapImage(new Uri(
                            isPlaying ? PauseIconUri : PlayIconUri));
                    }

                    if (cover != null)
                    {
                        try
                        {
                            // Convert System.Drawing.Image to a single BitmapImage and reuse for both layers.
                            using var ms = new MemoryStream();
                            cover.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            ms.Position = 0;

                            var img = new BitmapImage();
                            img.BeginInit();
                            img.CacheOption = BitmapCacheOption.OnLoad;
                            img.StreamSource = ms;
                            img.EndInit();
                            img.Freeze();

                            if (_coverImage != null)
                                _coverImage.Source = img;

                            if (_coverBlurred != null)
                                _coverBlurred.Source = img;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Cover conversion error: {ex.Message}");
                            ClearCoverImages();
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Cover is null");
                        ClearCoverImages();
                    }
                });
            }
            finally
            {
                cover?.Dispose();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateDisplay error: {ex.Message}");
        }
    }

    private void ClearCoverImages()
    {
        if (_coverImage != null)
            _coverImage.Source = null;
        
        if (_coverBlurred != null)
            _coverBlurred.Source = null;
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

    private void OpenSpotifyFromCover(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "spotify:",
                UseShellExecute = true
            });
            e.Handled = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OpenSpotifyFromCover error: {ex.Message}");
        }
    }

    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        if (_suspendPositionTracking || !IsVisible) return;
        // Update target position when window is manually moved
        _targetLeft = Left;
        _targetTop = Top;
        SaveWindowPosition();
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        if (!IsVisible) return;
        _lastOutsideHideUtc = DateTime.UtcNow;
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
