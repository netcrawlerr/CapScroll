using Avalonia;
using CapScroll.Core.Interfaces;
using CapScroll.Core.Models;

namespace CapScroll.Platform.Linux.Native.X11;

public sealed class X11Screenshot : ICaptureBackend
{
    public string Name => "X11";

    public bool IsAvailable =>
        OperatingSystem.IsLinux() &&
        X11Interop.IsAvailable();

    public Task<CaptureResult> CaptureScreenAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () => CaptureScreen(cancellationToken),
            cancellationToken);
    }

    public Task<CaptureResult> CaptureRegionAsync(
        PixelRect region,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () => CaptureRegion(region, cancellationToken),
            cancellationToken);
    }

    private CaptureResult CaptureScreen(
        CancellationToken cancellationToken)
    {
        var display = IntPtr.Zero;

        try
        {
            display = X11Interop.OpenDisplay();

            var width =
                X11Interop.GetScreenWidth(display);

            var height =
                X11Interop.GetScreenHeight(display);

            return Capture(
                display,
                X11Interop.GetRootWindow(display),
                0,
                0,
                width,
                height,
                cancellationToken);
        }
        catch (Exception ex)
        {
            return CaptureResult.Failed(
                $"X11 screen capture failed: {ex.Message}");
        }
        finally
        {
            X11Interop.CloseDisplay(display);
        }
    }

    private CaptureResult CaptureRegion(
        PixelRect region,
        CancellationToken cancellationToken)
    {
        var display = IntPtr.Zero;

        try
        {
            if (region.Width <= 0 || region.Height <= 0)
            {
                return CaptureResult.Failed(
                    "Capture region has invalid dimensions.");
            }

            display =
                X11Interop.OpenDisplay();

            return Capture(
                display,
                X11Interop.GetRootWindow(display),
                region.X,
                region.Y,
                region.Width,
                region.Height,
                cancellationToken);
        }
        catch (Exception ex)
        {
            return CaptureResult.Failed(
                $"X11 region capture failed: {ex.Message}");
        }
        finally
        {
            X11Interop.CloseDisplay(display);
        }
    }

    private static CaptureResult Capture(
        IntPtr display,
        IntPtr window,
        int x,
        int y,
        int width,
        int height,
        CancellationToken cancellationToken)
    {
        if (width <= 0 || height <= 0)
        {
            return CaptureResult.Failed(
                "Invalid capture dimensions.");
        }

        X11Interop.Sync(display);

        var image =
            X11Interop.CaptureImage(
                display,
                window,
                x,
                y,
                width,
                height);

        if (image == IntPtr.Zero)
        {
            return CaptureResult.Failed(
                "XGetImage returned a null image.");
        }

        try
        {
            var pixels =
                new byte[width * height * 4];

            var index = 0;

            for (var py = 0; py < height; py++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                for (var px = 0; px < width; px++)
                {
                    var pixel =
                        X11Interop.GetPixel(
                            image,
                            px,
                            py).ToInt64();

                    /*
                     * 
                     * red   = 0x00FF0000
                     * green = 0x0000FF00
                     * blue  = 0x000000FF
                     *
                     * ion i might be lost here
                     */

                    var red =
                        (byte)((pixel >> 16) & 0xFF);

                    var green =
                        (byte)((pixel >> 8) & 0xFF);

                    var blue =
                        (byte)(pixel & 0xFF);

                    pixels[index++] = red;
                    pixels[index++] = green;
                    pixels[index++] = blue;
                    pixels[index++] = 255;
                }
            }

            return CaptureResult.FromPixels(
                pixels,
                width,
                height,
                width * 4);
        }
        finally
        {
            X11Interop.DestroyImage(image);
        }
    }
}