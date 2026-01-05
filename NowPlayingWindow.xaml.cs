using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Shapes;
using System.IO;

namespace SpotifyTray;

public partial class NowPlayingWindow : Window
{
    private readonly MediaController _media;

    public NowPlayingWindow(MediaController media)
    {
        InitializeComponent();
        _media = media;
        _media.MediaChanged += UpdateDisplay;
        
        // Position near system tray (higher up)
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 10;
        Top = workArea.Bottom - Height - 60; // Moved 50 pixels higher
        
        UpdateDisplay();
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
                        System.Diagnostics.Debug.WriteLine($"Cover error: {ex.Message}");
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

    private async void PlayPause(object _, RoutedEventArgs __)
    {
        var task = _media.PlayPause();
        if (task != null) await task;
    }

    private async void Next(object _, RoutedEventArgs __)
    {
        var task = _media.Next();
        if (task != null) await task;
    }

    private async void Prev(object _, RoutedEventArgs __)
    {
        var task = _media.Previous();
        if (task != null) await task;
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        Hide();
    }
}
