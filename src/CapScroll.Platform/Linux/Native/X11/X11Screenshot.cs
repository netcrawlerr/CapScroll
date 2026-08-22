using Avalonia;
using CapScroll.Core.Interfaces;
using CapScroll.Core.Models;

namespace CapScroll.Platform.Linux.Native.X11;

public class X11Screenshot : ICaptureBackend
{
    public string Name => "X11";

    public bool IsAvailable => OperatingSystem.IsLinux() &&
                               X11Interop.IsAvailable();
    public Task<CaptureResult> CaptureScreenAsync(CancellationToken cancellationToken = default)
    {
       // to be imp after creating the engine
    }

    public Task<CaptureResult> CaptureRegionAsync(PixelRect region, CancellationToken cancellationToken = default)
    {
       // to be imp after creating the engine
    }
    
    // private methods to cap screen
    
    private CaptureResult CaptureScren(CancellationToken cancellationToken){}
    
    private CaptureResult CaptureRegion(PixelRect region, CancellationToken cancellationToken){}
    
    private static CaptureResult Capture(IntPtr display, IntPtr window, int x, int y , int width, int height, CancellationToken cancellationToken){}
}