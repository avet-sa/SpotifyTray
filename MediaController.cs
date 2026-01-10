using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Control;
using Windows.Storage.Streams;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

public class MediaController
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;
    private Image? _currentCover;
    
    // Cache for album artwork: key is track identifier, value is cached image
    private readonly Dictionary<string, Image> _artworkCache = new();
    private const int MaxCacheSize = 50; // Limit cache to 50 images

    public event Action? MediaChanged;
    public Image? CurrentCover => _currentCover;
    
    public bool IsPlaying()
    {
        if (_session == null) return false;
        var info = _session.GetPlaybackInfo();
        return info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
    }

    public bool IsSpotifyActive()
    {
        if (_session == null) return false;
        try
        {
            var sourceAppId = _session.SourceAppUserModelId;
            return sourceAppId.Contains("Spotify", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"IsSpotifyActive error: {ex.Message}");
            return false;
        }
    }

    public async Task InitializeAsync()
    {
        _manager =
            await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();

        _manager.CurrentSessionChanged += (_, __) => AttachSession();
        AttachSession();
    }

    private void AttachSession()
    {
        if (_manager == null) return;
        
        _session = _manager.GetCurrentSession();
        if (_session == null) return;

        _session.MediaPropertiesChanged += (_, __) => MediaChanged?.Invoke();
        _session.PlaybackInfoChanged += (_, __) => MediaChanged?.Invoke();
    }

    public async Task<(string title, string artist, string album, Image? cover)>
        GetNowPlayingAsync()
    {
        if (_session == null)
            return ("", "", "", null);

        var media = await _session.TryGetMediaPropertiesAsync();
        
        // Create a cache key from track metadata
        var cacheKey = GenerateCacheKey(media.Title, media.Artist, media.AlbumTitle);
        
        // Check if we have this artwork cached
        if (_artworkCache.TryGetValue(cacheKey, out var cachedImage))
        {
            System.Diagnostics.Debug.WriteLine($"Using cached artwork for: {media.Title}");
            _currentCover = new Bitmap(cachedImage); // Clone to avoid disposal issues
        }
        else
        {
            // Not cached, fetch and cache it
            _currentCover = await GetCoverAsync(media.Thumbnail);
            
            if (_currentCover != null)
            {
                // Add to cache (clone for cache storage)
                _artworkCache[cacheKey] = new Bitmap(_currentCover);
                
                // Enforce cache size limit
                if (_artworkCache.Count > MaxCacheSize)
                {
                    var oldestKey = _artworkCache.Keys.First();
                    _artworkCache[oldestKey]?.Dispose();
                    _artworkCache.Remove(oldestKey);
                    System.Diagnostics.Debug.WriteLine($"Cache limit reached, removed oldest entry");
                }
                
                System.Diagnostics.Debug.WriteLine($"Cached new artwork for: {media.Title}");
            }
        }

        return (
            media.Title,
            media.Artist,
            media.AlbumTitle,
            _currentCover
        );
    }
    
    private string GenerateCacheKey(string title, string artist, string album)
    {
        // Create a unique key based on track metadata
        var keyString = $"{title}|{artist}|{album}".ToLowerInvariant();
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(keyString));
        return Convert.ToHexString(hash);
    }

    private async Task<Image?> GetCoverAsync(IRandomAccessStreamReference? thumbnail)
    {
        if (thumbnail == null) return null;

        using var stream = await thumbnail.OpenReadAsync();
        using var netStream = stream.AsStream();
        using var tempImage = Image.FromStream(netStream);
        // Clone the image to avoid disposal issues
        return new Bitmap(tempImage);
    }

    public Task? PlayPause() => _session?.TryTogglePlayPauseAsync().AsTask();
    public Task? Next() => _session?.TrySkipNextAsync().AsTask();
    public Task? Previous() => _session?.TrySkipPreviousAsync().AsTask();

    /// <summary>
    /// Calculates the median color from an image.
    /// Samples pixels to find the median R, G, and B values.
    /// </summary>
    public Color GetMedianColor(Image? image)
    {
        if (image == null)
            return Color.FromArgb(32, 32, 32); // Default dark color

        try
        {
            var bitmap = image as Bitmap ?? new Bitmap(image);
            int width = bitmap.Width;
            int height = bitmap.Height;

            // Sample pixels (every nth pixel to improve performance for large images)
            int step = Math.Max(1, Math.Max(width, height) / 100); // Sample ~100x100 pixels
            var reds = new List<int>();
            var greens = new List<int>();
            var blues = new List<int>();

            for (int y = 0; y < height; y += step)
            {
                for (int x = 0; x < width; x += step)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    reds.Add(pixel.R);
                    greens.Add(pixel.G);
                    blues.Add(pixel.B);
                }
            }

            // Calculate median values
            reds.Sort();
            greens.Sort();
            blues.Sort();

            int medianRed = reds[reds.Count / 2];
            int medianGreen = greens[greens.Count / 2];
            int medianBlue = blues[blues.Count / 2];

            return Color.FromArgb(medianRed, medianGreen, medianBlue);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetMedianColor error: {ex.Message}");
            return Color.FromArgb(32, 32, 32);
        }
    }

    /// <summary>
    /// Extracts dominant colors from the edges of the image.
    /// Returns colors from top, right, bottom, and left edges.
    /// </summary>
    public (Color top, Color right, Color bottom, Color left) GetEdgeColors(Image? image)
    {
        var defaultColor = Color.FromArgb(32, 32, 32);
        
        if (image == null)
            return (defaultColor, defaultColor, defaultColor, defaultColor);

        try
        {
            var bitmap = image as Bitmap ?? new Bitmap(image);
            int width = bitmap.Width;
            int height = bitmap.Height;

            // Define edge sampling regions (10% of each edge)
            int edgeThickness = Math.Max(1, Math.Min(width, height) / 10);

            // Helper function to get median color from a region
            Color GetRegionColor(int startX, int startY, int endX, int endY)
            {
                var reds = new List<int>();
                var greens = new List<int>();
                var blues = new List<int>();

                int step = Math.Max(1, (endX - startX) / 10); // Sample ~10 pixels per edge
                
                for (int y = startY; y < endY; y += step)
                {
                    for (int x = startX; x < endX; x += step)
                    {
                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            var pixel = bitmap.GetPixel(x, y);
                            reds.Add(pixel.R);
                            greens.Add(pixel.G);
                            blues.Add(pixel.B);
                        }
                    }
                }

                if (reds.Count == 0)
                    return defaultColor;

                reds.Sort();
                greens.Sort();
                blues.Sort();

                return Color.FromArgb(
                    reds[reds.Count / 2],
                    greens[greens.Count / 2],
                    blues[blues.Count / 2]
                );
            }

            // Extract colors from each edge
            var topColor = GetRegionColor(0, 0, width, edgeThickness);
            var bottomColor = GetRegionColor(0, height - edgeThickness, width, height);
            var leftColor = GetRegionColor(0, 0, edgeThickness, height);
            var rightColor = GetRegionColor(width - edgeThickness, 0, width, height);

            return (topColor, rightColor, bottomColor, leftColor);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetEdgeColors error: {ex.Message}");
            return (defaultColor, defaultColor, defaultColor, defaultColor);
        }
    }
}
