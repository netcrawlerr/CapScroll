using Avalonia;
using CapScroll.Core.Models;

namespace CapScroll.Core.Interfaces;

public interface ICaptureBackend
{
    string Name { get; }

    bool IsAvailable { get; }

    Task<CaptureResult> CaptureScreenAsync(
        CancellationToken cancellationToken = default);

    Task<CaptureResult> CaptureRegionAsync(
        PixelRect region,
        CancellationToken cancellationToken = default);
}