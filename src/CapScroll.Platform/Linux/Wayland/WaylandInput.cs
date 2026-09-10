using System.Diagnostics;
using Avalonia;

namespace CapScroll.Platform.Linux.Wayland;

public static class WaylandInput
{
    public static void MovePointerToRegionCenter(PixelRect region)
    {
        var centerX = region.X + (region.Width / 2);
        var centerY = region.Y + (region.Height / 2);

        RunTool("ydotool", $"mousemove -a {centerX} {centerY}");
    }

    public static void ScrollDown(int clicks)
    {
        RunTool("ydotool", $"click 0x151 --repeat {clicks}");
    }

    private static void RunTool(string fileName, string args)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            proc?.WaitForExit(200);
        }
        catch
        {
            // fb
        }
    }
}
