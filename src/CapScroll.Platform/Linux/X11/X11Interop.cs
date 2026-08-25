using System.Runtime.InteropServices;

namespace CapScroll.Platform.Linux.X11;

/// <summary>
/// Native P/Invoke interface for core X11 library calls (libX11.so.6).
/// </summary>
internal static class X11Interop
{
    private const string X11 = "libX11.so.6";

    /// <summary>
    /// opens a connection to the X server specified by display_name.
    /// </summary>
    [DllImport(X11)]
    private static extern IntPtr XOpenDisplay(
        string? display_name);

    /// <summary>
    /// closes the connection to the specified X display server.
    /// </summary>
    [DllImport(X11)]
    private static extern int XCloseDisplay(
        IntPtr display);

    /// <summary>
    /// returns the default root window ID for the default screen.
    /// </summary>
    [DllImport(X11)]
    private static extern IntPtr XDefaultRootWindow(
        IntPtr display);

    /// <summary>
    /// returns the default screen number for the display.
    /// </summary>
    [DllImport(X11)]
    private static extern int XDefaultScreen(
        IntPtr display);

    /// <summary>
    /// returns the root window ID for a given screen number.
    /// </summary>
    [DllImport(X11)]
    private static extern IntPtr XRootWindow(
        IntPtr display,
        int screen_number);

    /// <summary>
    /// returns the width of the screen in pixels.
    /// </summary>
    [DllImport(X11)]
    private static extern int XDisplayWidth(
        IntPtr display,
        int screen_number);

    /// <summary>
    /// returns the height of the screen in pixels.
    /// </summary>
    [DllImport(X11)]
    private static extern int XDisplayHeight(
        IntPtr display,
        int screen_number);

    /// <summary>
    /// captures a sub-image buffer from a drawable (window/screen).
    /// </summary>
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

    /// <summary>
    /// deallocates memory associated with an XImage structure.
    /// </summary>
    [DllImport(X11)]
    private static extern IntPtr XDestroyImage(
        IntPtr image);

    /// <summary>
    /// reads pixel data at a specific coordinate from an XImage handle.
    /// </summary>
    [DllImport(X11)]
    private static extern IntPtr XGetPixel(
        IntPtr image,
        int x,
        int y);

    /// <summary>
    /// flushes output commands to the X server.
    /// </summary>
    [DllImport(X11)]
    private static extern int XFlush(
        IntPtr display);

    /// <summary>
    /// flushes output commands and waits for all events to be processed.
    /// </summary>
    [DllImport(X11)]
    private static extern int XSync(
        IntPtr display,
        bool discard);

    /// <summary>
    /// frees memory allocated by X11 library functions.
    /// </summary>
    [DllImport(X11)]
    private static extern int XFree(
        IntPtr data);

    /// <summary>
    /// returns the atom identifier associated with a property string name.
    /// </summary>
    [DllImport(X11)]
    private static extern IntPtr XInternAtom(
        IntPtr display,
        string atom_name,
        bool only_if_exists);

    /// <summary>
    /// returns property information for a specified window.
    /// </summary>
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

    /// <summary>
    /// checks whether an X11 server connection can be established on the host.
    /// </summary>
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



    /// <summary>
    /// opens the primary X display server connection handle.
    /// </summary>
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

    /// <summary>
    /// closes an open X display connection handle.
    /// </summary>
    public static void CloseDisplay(IntPtr display)
    {
        if (display != IntPtr.Zero)
            XCloseDisplay(display);
    }

    /// <summary>
    /// gets the root window handle for the default screen.
    /// </summary>
    public static IntPtr GetRootWindow(
        IntPtr display)
    {
        return XDefaultRootWindow(display);
    }

    /// <summary>
    /// gets the index of the default screen for the display connection.
    /// </summary>
    public static int GetDefaultScreen(
        IntPtr display)
    {
        return XDefaultScreen(display);
    }

    /// <summary>
    /// gets the total width of the default screen in pixels.
    /// </summary>
    public static int GetScreenWidth(
        IntPtr display)
    {
        return XDisplayWidth(
            display,
            GetDefaultScreen(display));
    }

    /// <summary>
    /// gets the total height of the default screen in pixels.
    /// </summary>
    public static int GetScreenHeight(
        IntPtr display)
    {
        return XDisplayHeight(
            display,
            GetDefaultScreen(display));
    }

    /// <summary>
    /// reads a specified rectangular pixel region into an XImage handle.
    /// </summary>
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

    /// <summary>
    /// gets a single pixel value at (x, y) coordinates from an XImage handle.
    /// </summary>
    public static IntPtr GetPixel(
        IntPtr image,
        int x,
        int y)
    {
        return XGetPixel(image, x, y);
    }

    /// <summary>
    /// releases and frees the memory associated with an XImage handle.
    /// </summary>
    public static void DestroyImage(
        IntPtr image)
    {
        if (image != IntPtr.Zero)
            XDestroyImage(image);
    }

    /// <summary>
    /// flushes pending output requests to the X server.
    /// </summary>
    public static void Flush(
        IntPtr display)
    {
        XFlush(display);
    }

    /// <summary>
    /// sync X server request queues.
    /// </summary>
    public static void Sync(
        IntPtr display)
    {
        XSync(display, false);
    }
}
