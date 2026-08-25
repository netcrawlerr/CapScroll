using Avalonia;
using CapScroll.Core.Interfaces;
using CapScroll.Core.Models;

namespace CapScroll.Platform.Linux.X11;

/// <summary>
/// provides screen and region capture capabilities for Linux using the native X11 display protocol.
/// </summary>
public sealed class X11Screenshot : ICaptureBackend
{
    /// <summary>
    /// gets the unique identifier name of the capture backend provider.
    /// </summary>
    public string Name => "X11";

    /// <summary>
    /// gets a value indicating whether the current operating system is Linux and an active X11 server connection is available.
    /// </summary>
    public bool IsAvailable =>
        OperatingSystem.IsLinux() &&
        X11Interop.IsAvailable();

    /// <summary>
    /// Asynchronously captures the entire primary display screen area.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests during execution.</param>
    /// <returns>A task containing the captured screen pixel data or failure details.</returns>
    public Task<CaptureResult> CaptureScreenAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () => CaptureScreen(cancellationToken),
            cancellationToken);
    }

    /// <summary>
    /// Asynchronously captures a specified rectangular pixel region on the screen.
    /// </summary>
    /// <param name="region">The target bounding box pixel coordinates and dimensions.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests during execution.</param>
    /// <returns>A task containing the captured region pixel data or failure details.</returns>
    public Task<CaptureResult> CaptureRegionAsync(
        PixelRect region,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () => CaptureRegion(region, cancellationToken),
            cancellationToken);
    }

    /// <summary>
    /// executes the full screen capture.
    /// </summary>
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

    /// <summary>
    /// regional screen capture.
    /// </summary>
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

    /// <summary>
    /// interacts with libX11 to extract raw pixel data from the window surface, converting color channels into RGBA format.
    /// </summary>
    /// <param name="display">The active X display server handle.</param>
    /// <param name="window">The root window handle target.</param>
    /// <param name="x">The starting X-coordinate origin relative to the root window.</param>
    /// <param name="y">The starting Y-coordinate origin relative to the root window.</param>
    /// <param name="width">The total width of the captured boundary in pixels.</param>
    /// <param name="height">The total height of the captured boundary in pixels.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A structured <see cref="CaptureResult"/> containing unmanaged pixel byte buffers or error state.</returns>
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
                     * Extract RGB components from bitmasked integer:
                     * Red: Bits 16..23 (0x00FF0000)
                     * Green: Bits 8..15 (0x0000FF00)
                     * Blue: Bits 0..7 (0x000000FF)
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
