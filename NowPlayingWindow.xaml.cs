using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.IO;
using System.Drawing;
using System.Windows.Media.Animation;
using DrawingImage = System.Drawing.Image;
using WpfPoint = System.Windows.Point;
using WpfColor = System.Windows.Media.Color;

namespace SpotifyTray;

public partial class NowPlayingWindow : Window
{
    // Constants
    private const int WindowOffsetX = 10;
    private const int WindowOffsetY = 60;
    private const string PositionSettingsKey = "WindowPosition";
    private const int AnimationDurationMs = 100;
    
    private readonly MediaController _media;
    private Border? _backgroundBorder; // Reference to the main background border
    private System.Windows.Controls.Image? _coverImage; // Reference to the main cover image
    private System.Windows.Controls.Image? _coverBlurred; // Reference to the blurred cover image
    private double _targetLeft; // Store the target position for animation
    private double _targetTop;

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
        
        // Store reference to background border
        _backgroundBorder = this.FindName("BackgroundBorder") as Border;
        
        // Find the cover images
        _coverImage = this.FindName("Cover") as System.Windows.Controls.Image;
        _coverBlurred = this.FindName("CoverBlurred") as System.Windows.Controls.Image;
        
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
        Opacity = 1;
        base.Show();
        
        // Animate slide in from bottom
        var workArea = SystemParameters.WorkArea;
        var slideAnimation = new DoubleAnimation
        {
            From = workArea.Bottom,
            To = _targetTop,
            Duration = TimeSpan.FromMilliseconds(AnimationDurationMs),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        
        BeginAnimation(Window.TopProperty, slideAnimation);
    }
    
    public new void Hide()
    {
        // Animate slide out to the bottom
        var workArea = SystemParameters.WorkArea;
        var slideAnimation = new DoubleAnimation
        {
            From = Top,
            To = workArea.Bottom,
            Duration = TimeSpan.FromMilliseconds(AnimationDurationMs),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        
        slideAnimation.Completed += (s, e) => base.Hide();
        
        BeginAnimation(Window.TopProperty, slideAnimation);
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

                        // Set both the sharp and blurred cover images
                        if (_coverImage != null)
                            _coverImage.Source = img;
                        
                        if (_coverBlurred != null)
                        {
                            // Create a fresh BitmapImage for the blurred version
                            ms.Position = 0;
                            var blurredImg = new BitmapImage();
                            blurredImg.BeginInit();
                            blurredImg.CacheOption = BitmapCacheOption.OnLoad;
                            blurredImg.StreamSource = ms;
                            blurredImg.EndInit();
                            _coverBlurred.Source = blurredImg;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Cover conversion error: {ex.Message}");
                        if (_coverImage != null)
                            _coverImage.Source = null;
                        if (_coverBlurred != null)
                            _coverBlurred.Source = null;
                        ResetBackgroundGradient();
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Cover is null");
                    ResetBackgroundGradient();
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateDisplay error: {ex.Message}");
        }
    }

    private void ResetBackgroundGradient()
    {
        if (_backgroundBorder == null) return;
        _backgroundBorder.Background = new SolidColorBrush(WpfColor.FromRgb(32, 32, 32));
        
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

    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        // Update target position when window is manually moved
        _targetLeft = Left;
        _targetTop = Top;
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
