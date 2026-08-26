using Avalonia;
using CapScroll.Core.Interfaces;
using CapScroll.Core.Models;

namespace CapScroll.Platform.Linux.Wayland;

public sealed class WaylandScreenshot : ICaptureBackend
{
    public string Name => "Wayland";

    public bool IsAvailable => true;

    public Task<CaptureResult> CaptureRegionAsync(PixelRect region, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<CaptureResult> CaptureScreenAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
