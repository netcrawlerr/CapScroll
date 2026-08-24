using System.Runtime.InteropServices;

namespace CapScroll.Platform.Linux.X11;

internal static class X11Interop
{
    private const string X11 = "libX11.so.6";

    [DllImport(X11)]
    private static extern IntPtr XOpenDisplay(
        string? display_name);

    [DllImport(X11)]
    private static extern int XCloseDisplay(
        IntPtr display);

    [DllImport(X11)]
    private static extern IntPtr XDefaultRootWindow(
        IntPtr display);

    [DllImport(X11)]
    private static extern int XDefaultScreen(
        IntPtr display);

    [DllImport(X11)]
    private static extern IntPtr XRootWindow(
        IntPtr display,
        int screen_number);

    [DllImport(X11)]
    private static extern int XDisplayWidth(
        IntPtr display,
        int screen_number);

    [DllImport(X11)]
    private static extern int XDisplayHeight(
        IntPtr display,
        int screen_number);

    [DllImport(X11)]
    private static extern IntPtr XGetImage(
        IntPtr display,
        IntPtr drawable,
        int x,
        int y,
        uint width,
        uint height,
        ulong plane_mask,
        int format);

    [DllImport(X11)]
    private static extern IntPtr XDestroyImage(
        IntPtr image);

    [DllImport(X11)]
    private static extern IntPtr XGetPixel(
        IntPtr image,
        int x,
        int y);

    [DllImport(X11)]
    private static extern int XFlush(
        IntPtr display);

    [DllImport(X11)]
    private static extern int XSync(
        IntPtr display,
        bool discard);

    [DllImport(X11)]
    private static extern int XFree(
        IntPtr data);

    [DllImport(X11)]
    private static extern IntPtr XInternAtom(
        IntPtr display,
        string atom_name,
        bool only_if_exists);

    [DllImport(X11)]
    private static extern int XGetWindowProperty(
        IntPtr display,
        IntPtr window,
        IntPtr property,
        IntPtr long_offset,
        IntPtr long_length,
        bool delete,
        IntPtr req_type,
        out IntPtr actual_type_return,
        out int actual_format_return,
        out IntPtr nitems_return,
        out IntPtr bytes_after_return,
        out IntPtr prop_return);

    private const int ZPixmap = 2;

    private static readonly ulong AllPlanes = ulong.MaxValue;

    public static bool IsAvailable()
    {
        if (!OperatingSystem.IsLinux())
            return false;

        try
        {
            var display = XOpenDisplay(null);

            if (display == IntPtr.Zero)
                return false;

            XCloseDisplay(display);
            return true;
        }
        catch
        {
            return false;
        }
    }



    public static IntPtr OpenDisplay()
    {
        var display = XOpenDisplay(null);

        if (display == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Unable to open the X11 display.");
        }

        return display;
    }

    public static void CloseDisplay(IntPtr display)
    {
        if (display != IntPtr.Zero)
            XCloseDisplay(display);
    }

    public static IntPtr GetRootWindow(
        IntPtr display)
    {
        return XDefaultRootWindow(display);
    }

    public static int GetDefaultScreen(
        IntPtr display)
    {
        return XDefaultScreen(display);
    }

    public static int GetScreenWidth(
        IntPtr display)
    {
        return XDisplayWidth(
            display,
            GetDefaultScreen(display));
    }

    public static int GetScreenHeight(
        IntPtr display)
    {
        return XDisplayHeight(
            display,
            GetDefaultScreen(display));
    }

    public static IntPtr CaptureImage(
        IntPtr display,
        IntPtr window,
        int x,
        int y,
        int width,
        int height)
    {
        return XGetImage(
            display,
            window,
            x,
            y,
            (uint)width,
            (uint)height,
            AllPlanes,
            ZPixmap);
    }

    public static IntPtr GetPixel(
        IntPtr image,
        int x,
        int y)
    {
        return XGetPixel(image, x, y);
    }

    public static void DestroyImage(
        IntPtr image)
    {
        if (image != IntPtr.Zero)
            XDestroyImage(image);
    }

    public static void Flush(
        IntPtr display)
    {
        XFlush(display);
    }

    public static void Sync(
        IntPtr display)
    {
        XSync(display, false);
    }
}
