using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Control;
using Windows.Storage.Streams;
using System.Drawing;

public class MediaController
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;
    private Image? _currentCover;

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
        catch { return false; }
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
        _currentCover = await GetCoverAsync(media.Thumbnail);

        return (
            media.Title,
            media.Artist,
            media.AlbumTitle,
            _currentCover
        );
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
