using System.Runtime.InteropServices;

namespace CapScroll.Platform.Linux.X11;

/// <summary>
/// interop wrapper for X11 user input events via libX11 and libXtst.
/// </summary>
internal static class X11Input
{
    private const string X11 = "libX11.so.6";
    private const string Xtst = "libXtst.so.6";

    private const uint ButtonPress = 4;
    private const uint ButtonRelease = 5;

    private const uint Button4 = 4;
    private const uint Button5 = 5;

    /// <summary>
    /// moves the pointer to the specified coordinates relative to the destination window.
    /// </summary>
    [DllImport(X11)]
    private static extern int XWarpPointer(
        IntPtr display,
        IntPtr srcWindow,
        IntPtr destWindow,
        int srcX,
        int srcY,
        uint srcWidth,
        uint srcHeight,
        int destX,
        int destY);

    /// <summary>
    /// flushes the output buffer to ensure all pending requests are sent to the X server.
    /// </summary>
    [DllImport(X11)]
    private static extern int XFlush(
        IntPtr display);

    /// <summary>
    /// sends a mouse button press or release event using the XTest extension.
    /// </summary>
    [DllImport(Xtst)]
    private static extern int XTestFakeButtonEvent(
        IntPtr display,
        uint button,
        bool isPress,
        ulong delay);

    /// <summary>
    /// moves the X11 mouse pointer directly to absolute root screen coordinates (x, y).
    /// </summary>
    public static void MovePointer(
        IntPtr display,
        int x,
        int y)
    {
        if (display == IntPtr.Zero)
        {
            throw new ArgumentException(
                "Invalid X11 display.",
                nameof(display));
        }

        var root =
            X11Interop.GetRootWindow(display);

        var result =
            XWarpPointer(
                display,
                IntPtr.Zero,
                root,
                0,
                0,
                0,
                0,
                x,
                y);

        if (result == 0)
        {
            throw new InvalidOperationException(
                "Unable to move the X11 pointer.");
        }

        XFlush(display);
    }

    /// <summary>
    /// mouse scroll down events.
    /// </summary>
    public static void ScrollDown(
        IntPtr display,
        int clicks = 5)
    {
        if (clicks <= 0)
            return;

        for (var i = 0; i < clicks; i++)
        {
            SendButton(
                display,
                Button5);
        }

        XFlush(display);
    }

    /// <summary>
    /// mouse scroll up events.
    /// </summary>
    public static void ScrollUp(
        IntPtr display,
        int clicks = 5)
    {
        if (clicks <= 0)
            return;

        for (var i = 0; i < clicks; i++)
        {
            SendButton(
                display,
                Button4);
        }

        XFlush(display);
    }

    /// <summary>
    /// sends a full press-and-release button cycle.
    /// </summary>
    private static void SendButton(
        IntPtr display,
        uint button)
    {
        var pressResult =
            XTestFakeButtonEvent(
                display,
                button,
                true,
                0);

        if (pressResult == 0)
        {
            throw new InvalidOperationException(
                "Unable to send X11 mouse button press.");
        }

        var releaseResult =
            XTestFakeButtonEvent(
                display,
                button,
                false,
                0);

        if (releaseResult == 0)
        {
            throw new InvalidOperationException(
                "Unable to send X11 mouse button release.");
        }
    }
}
