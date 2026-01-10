using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.IO;
using System.Drawing;
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
    
    private readonly MediaController _media;
    private Border? _backgroundBorder; // Reference to the main background border
    private Border? _coverBorder; // Reference to the cover border for applying effects

    public NowPlayingWindow(MediaController media)
    {
        InitializeComponent();
        _media = media;
        _media.MediaChanged += UpdateDisplay;
        
        // Add keyboard shortcuts
        KeyDown += OnKeyDown;
        
        // Load saved position or use default
        LoadWindowPosition();
        
        // Store reference to background border
        _backgroundBorder = this.FindName("BackgroundBorder") as Border;
        
        // Find the cover border (parent of the Cover image)
        var cover = this.FindName("Cover") as System.Windows.Controls.Image;
        if (cover?.Parent is Border coverBorder)
        {
            _coverBorder = coverBorder;
        }
        
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

                        // Calculate median color and apply drop shadow and gradient
                        ApplyMedianColorEffects(cover);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Cover conversion error: {ex.Message}");
                        Cover.Source = null;
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

    private void ApplyMedianColorEffects(DrawingImage cover)
    {
        if (_coverBorder == null)
        {
            System.Diagnostics.Debug.WriteLine("Cover border not found");
            return;
        }

        try
        {
            // Get the median color from the cover
            var medianColor = _media.GetMedianColor(cover);
            System.Diagnostics.Debug.WriteLine($"Median color: R={medianColor.R}, G={medianColor.G}, B={medianColor.B}");
            
            // Convert to WPF Color
            var wpfMedianColor = WpfColor.FromArgb(
                255,
                medianColor.R,
                medianColor.G,
                medianColor.B
            );
            
            // Apply drop shadow to the cover border with the median color
            var dropShadow = new DropShadowEffect
            {
                Color = wpfMedianColor,
                BlurRadius = 13,
                Opacity = 0.6,
                ShadowDepth = 0,
                Direction = 0,
                RenderingBias = RenderingBias.Performance,

            };
            
            System.Diagnostics.Debug.WriteLine("Applying drop shadow effect");
            _coverBorder.Effect = dropShadow;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ApplyMedianColorEffects error: {ex.Message}");
        }
    }

    private void ResetBackgroundGradient()
    {
        if (_backgroundBorder == null) return;
        _backgroundBorder.Background = new SolidColorBrush(WpfColor.FromRgb(32, 32, 32));
        
        if (_coverBorder != null)
        {
            _coverBorder.Effect = null;
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
