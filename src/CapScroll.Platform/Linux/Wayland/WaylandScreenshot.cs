using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using CapScroll.Core.Interfaces;
using CapScroll.Core.Models;
using CapScroll.Platform.Linux.Portals;

namespace CapScroll.Platform.Linux.Wayland;

/// <summary>
/// capture capabilities using the standard XDG Desktop Portal over D-Bus.
/// </summary>
public sealed class WaylandScreenshot : ICaptureBackend
{
    public string Name => "XDG Desktop Portal";

    public bool IsAvailable =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")) ||
        XdgPortalService.IsAvailable();

    public Task<CaptureResult> CaptureScreenAsync(CancellationToken cancellationToken = default)
    {
        return XdgPortalService.CaptureAsync(
            interactive: false,
            cropRegion: null,
            cancellationToken: cancellationToken);
    }

    public Task<CaptureResult> CaptureRegionAsync(PixelRect region, CancellationToken cancellationToken = default)
    {
        return XdgPortalService.CaptureAsync(
            interactive: false,
            cropRegion: region,
            cancellationToken: cancellationToken);
    }

    public Task<CaptureResult> CaptureInteractiveAsync(CancellationToken cancellationToken = default)
    {
        return XdgPortalService.CaptureAsync(
            interactive: true,
            cropRegion: null,
            cancellationToken: cancellationToken);
    }
}
