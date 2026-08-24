namespace CapScroll.Platform.Linux.X11;

internal static class X11Window
{
    public static void MovePointerToRegionCenter(
        IntPtr display,
        Avalonia.PixelRect region)
    {
        var centerX =
            region.X + region.Width / 2;

        var centerY =
            region.Y + region.Height / 2;

        X11Input.MovePointer(  // mv ptr to center
            display,
            centerX,
            centerY);
    }
}
