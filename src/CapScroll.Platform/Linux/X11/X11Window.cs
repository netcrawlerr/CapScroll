namespace CapScroll.Platform.Linux.X11;

/// <summary>
/// Helper for window and region interaction within the X11 display environment.
/// </summary>
internal static class X11Window
{
    /// <summary>
    /// calculates the midpoint of a specified rectangular region and moves the X11 mouse pointer to that coordinate.
    /// </summary>
    /// <param name="display">The active X display server connection handle.</param>
    /// <param name="region">The target bounding box region in screen pixel coordinates.</param>
    public static void MovePointerToRegionCenter(
        IntPtr display,
        Avalonia.PixelRect region)
    {
        var centerX =
            region.X + region.Width / 2;

        var centerY =
            region.Y + region.Height / 2;

        X11Input.MovePointer(
            display,
            centerX,
            centerY);
    }
}
