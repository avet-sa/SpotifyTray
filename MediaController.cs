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
    
    // Cache for album artwork: key is track identifier, value is cached image
    private readonly Dictionary<string, Image> _artworkCache = new();
    private const int MaxCacheSize = 50; // Limit cache to 50 images

    public event Action? MediaChanged;
    
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
        Image? cover = null;
        
        // Create a cache key from track metadata
        var cacheKey = GenerateCacheKey(media.Title, media.Artist, media.AlbumTitle);
        
        // Check if we have this artwork cached
        if (_artworkCache.TryGetValue(cacheKey, out var cachedImage))
        {
            System.Diagnostics.Debug.WriteLine($"Using cached artwork for: {media.Title}");
            cover = new Bitmap(cachedImage); // Clone so caller can dispose safely
        }
        else
        {
            // Not cached, fetch and cache it
            cover = await GetCoverAsync(media.Thumbnail);
            
            if (cover != null)
            {
                // Add to cache (clone for cache storage)
                _artworkCache[cacheKey] = new Bitmap(cover);
                
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
            cover
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
}
