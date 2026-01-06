        using System.Windows;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using Application = System.Windows.Application;

namespace SpotifyTray;

public partial class App : Application
{
    // Constants
    private const int TrayIconSize = 32;
    private const int TooltipMaxLength = 63;
    private const int TooltipTruncateLength = 60;
    private const int MediaChangeDebounceMs = 300;
    
    private NotifyIcon? _notifyIcon;
    private MediaController? _media;
    private NowPlayingWindow? _window;
    private System.Threading.Timer? _debounceTimer;
    private readonly object _debounceLock = new object();
    private CancellationTokenSource? _cancellationTokenSource;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Enforce single instance
        var appName = "SpotifyTray_SingleInstance";
        bool createdNew;
        var mutex = new System.Threading.Mutex(true, appName, out createdNew);
        
        if (!createdNew)
        {
            System.Windows.MessageBox.Show("SpotifyTray is already running.", "Already Running", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();

        _media = new MediaController();
        _media.MediaChanged += OnMediaChanged;
        Task.Run(async () =>
        {
            try
            {
                await _media.InitializeAsync();
                // Load initial media info to get the cover
                await _media.GetNowPlayingAsync();
                // Update tray icon on UI thread
                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Dispatcher.Invoke(() => OnMediaChanged());
                }
            }
            catch (OperationCanceledException)
            {
                // App is shutting down
            }
        }, _cancellationTokenSource.Token);

        var contextMenu = new ContextMenuStrip();
        contextMenu.Renderer = new ModernContextMenuRenderer();
        contextMenu.BackColor = ColorTranslator.FromHtml("#202020");
        contextMenu.ForeColor = Color.White;
        contextMenu.ShowImageMargin = false;
        // contextMenu.Padding = new Padding(8, 6, 8, 6);
        
        var openSpotifyItem = new ToolStripMenuItem("Open Spotify");
        openSpotifyItem.Click += OpenSpotify;
        openSpotifyItem.ForeColor = Color.White;
        
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += ExitApp;
        exitItem.ForeColor = Color.White;
        
        contextMenu.Items.Add(openSpotifyItem);
        contextMenu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "Spotify Tray",
            ContextMenuStrip = contextMenu
        };

        _notifyIcon.Click += OnTrayIconClick;
    }

    private void OnTrayIconClick(object? sender, EventArgs e)
    {
        var mouseEvent = e as MouseEventArgs;
        if (mouseEvent?.Button != MouseButtons.Left) return;

        if (_window == null || !_window.IsVisible)
        {
            if (_media == null) return;
            
            _window = new NowPlayingWindow(_media);
            _window.Show();
            _window.Activate();
        }
        else
        {
            _window.Hide();
        }
    }

    private void OnMediaChanged()
    {
        if (_media == null || _notifyIcon == null) return;

        // Debounce rapid media change events
        lock (_debounceLock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = new System.Threading.Timer(_ =>
            {
                // Show tray icon only when Spotify is active
                var isSpotifyActive = _media.IsSpotifyActive();
                
                Dispatcher.Invoke(() =>
                {
                    _notifyIcon.Visible = isSpotifyActive;
                });

                if (isSpotifyActive)
                {
                    UpdateTrayIcon();
                }
                else
                {
                    // Hide window if Spotify stops
                    Dispatcher.Invoke(() =>
                    {
                        if (_window != null && _window.IsVisible)
                        {
                            _window.Hide();
                        }
                        UpdateTrayIconTooltip("", "");
                    });
                }
            }, null, MediaChangeDebounceMs, Timeout.Infinite);
        }
    }

    private async void UpdateTrayIcon()
    {
        if (_media == null || _notifyIcon == null) return;

        try
        {
            // Get fresh media info
            var (title, artist, album, cover) = await _media.GetNowPlayingAsync();
            
            // Update tooltip with current song info
            Dispatcher.Invoke(() => UpdateTrayIconTooltip(title, artist));
            
            if (cover == null) return;

            // Create a high-quality icon from the album cover
            using var resized = new Bitmap(TrayIconSize, TrayIconSize);
            using (var g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.DrawImage(cover, 0, 0, TrayIconSize, TrayIconSize);
            }
            
            var iconHandle = resized.GetHicon();
            var icon = Icon.FromHandle(iconHandle);
            
            // Update icon on UI thread
            Dispatcher.Invoke(() =>
            {
                var oldIcon = _notifyIcon.Icon;
                _notifyIcon.Icon = icon;
                
                if (oldIcon != null && oldIcon != SystemIcons.Application)
                {
                    DestroyIcon(oldIcon.Handle);
                    oldIcon.Dispose();
                }
            });
            
            // Clean up the icon handle
            DestroyIcon(iconHandle);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateTrayIcon error: {ex.Message}");
            // Fallback to default icon on error
        }
    }

    private void UpdateTrayIconTooltip(string songTitle, string artist)
    {
        if (_notifyIcon == null) return;

        // Handle empty states
        if (string.IsNullOrEmpty(songTitle) || string.IsNullOrEmpty(artist))
        {
            _notifyIcon.Text = "Spotify Tray";
            return;
        }

        // Windows tray icons have a maximum tooltip length
        string tooltip = $"{songTitle} - {artist}";
        
        // Truncate if necessary
        if (tooltip.Length > TooltipMaxLength)
        {
            tooltip = tooltip.Substring(0, TooltipTruncateLength) + "...";
        }
        
        _notifyIcon.Text = tooltip;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    private void OpenSpotify(object? sender, EventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "spotify:",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OpenSpotify error: {ex.Message}");
        }
    }

    private void ExitApp(object? sender, EventArgs e)
    {
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _cancellationTokenSource?.Cancel();
        _debounceTimer?.Dispose();
        _cancellationTokenSource?.Dispose();
        _notifyIcon?.Dispose();
        base.OnExit(e);
    }
}

public class ModernContextMenuRenderer : ToolStripProfessionalRenderer
{
    public ModernContextMenuRenderer() : base(new ModernColorTable()) 
    { 
        RoundedEdges = true;
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        // Draw rounded border
        using (var pen = new Pen(ColorTranslator.FromHtml("#3a3a3a"), 1))
        using (var path = GetRoundedRect(new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1), 8))
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.DrawPath(pen, path);
        }
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.IsOnDropDown) return;

        var rc = new Rectangle(2, 0, e.Item.Width - 4, e.Item.Height);
        var hoverColor = ColorTranslator.FromHtml("#3a3a3a");

        using (var path = GetRoundedRect(rc, 4))
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            if (e.Item.Selected)
            {
                using (var brush = new SolidBrush(hoverColor))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }
        }
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = Color.White;
        base.OnRenderItemText(e);
    }

    private System.Drawing.Drawing2D.GraphicsPath GetRoundedRect(Rectangle bounds, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        var diameter = radius * 2;
        var arc = new Rectangle(bounds.Location, new System.Drawing.Size(diameter, diameter));

        // Top left
        path.AddArc(arc, 180, 90);
        // Top right
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        // Bottom right
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        // Bottom left
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();
        return path;
    }
}

public class ModernColorTable : ProfessionalColorTable
{
    public override Color MenuBorder => ColorTranslator.FromHtml("#3a3a3a");
    public override Color MenuItemBorder => ColorTranslator.FromHtml("#3a3a3a");
    public override Color MenuItemSelected => ColorTranslator.FromHtml("#3a3a3a");
    public override Color MenuItemSelectedGradientBegin => ColorTranslator.FromHtml("#3a3a3a");
    public override Color MenuItemSelectedGradientEnd => ColorTranslator.FromHtml("#3a3a3a");
    public override Color MenuItemPressedGradientBegin => ColorTranslator.FromHtml("#4a4a4a");
    public override Color MenuItemPressedGradientEnd => ColorTranslator.FromHtml("#4a4a4a");
}
