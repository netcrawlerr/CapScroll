using Avalonia;
using CapScroll.Core.Models;

namespace CapScroll.Core.Interfaces;

/// <summary>
/// interface for platform screen capture backends (X11, Wayland, etc...)
/// </summary>
public interface ICaptureBackend
{
    /// <summary>
    /// identifier name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Whether this backend is supported on the current environment.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Captures the full screen asynchronously.
    /// </summary>
    Task<CaptureResult> CaptureScreenAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures a specific pixel region asynchronously.
    /// </summary>
    Task<CaptureResult> CaptureRegionAsync(
        PixelRect region,
        CancellationToken cancellationToken = default);
}
