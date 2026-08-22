using System.Runtime.InteropServices;

namespace CapScroll.Platform.Linux.Native.X11;

internal static class X11Input
{
    private const string X11 = "libX11.so.6";
    private const string Xtst = "libXtst.so.6";

    private const uint ButtonPress = 4;
    private const uint ButtonRelease = 5;

    private const uint Button4 = 4;
    private const uint Button5 = 5;

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

    [DllImport(X11)]
    private static extern int XFlush(
        IntPtr display);

    [DllImport(Xtst)]
    private static extern int XTestFakeButtonEvent(
        IntPtr display,
        uint button,
        bool isPress,
        ulong delay);

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
