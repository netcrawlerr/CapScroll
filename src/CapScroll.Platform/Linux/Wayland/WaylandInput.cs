using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using CapScroll.Platform.Linux.Portals;

namespace CapScroll.Platform.Linux.Wayland;

public static class WaylandInput
{
    private static readonly string SocketPath =
        $"/run/user/{(Environment.UserName == "root" ? "0" : "1000")}/.ydotool_socket";

    public static void ScrollDown(int clicks = 8)
    {
        ScrollDownAsync(clicks).GetAwaiter().GetResult();
    }

    public static async Task ScrollDownAsync(int clicks = 8)
    {
        try
        {
            if (XdgPortalService.IsAvailable())
            {
                bool success = await XdgPortalService.ScrollAsync(clicks);
                if (success)
                {
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WAYLAND INPUT] Portal scroll failed: {ex.Message}");
        }

        ScrollDownWithYdotool(clicks);
    }

    public static void MovePointerToRegionCenter(PixelRect region)
    {
        EnsureDaemonRunning();

        int centerX = region.X + (region.Width / 2);
        int centerY = region.Y + (region.Height / 2);

        ExecuteProcess("ydotool", $"mousemove -a {centerX} {centerY}");
    }

    private static void ScrollDownWithYdotool(int clicks)
    {
        EnsureDaemonRunning();

        for (int i = 0; i < clicks; i++)
        {
            ExecuteProcess("ydotool", "mousemove --wheel -y -1");
        }
    }

    private static void EnsureDaemonRunning()
    {
        if (File.Exists(SocketPath))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ydotoold",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            Task.Delay(300).Wait();
        }
        catch
        {
            Console.WriteLine("[WARN WAYLAND] Could not automatically spawn ydotoold daemon.");
        }
    }

    private static void ExecuteProcess(string fileName, string args)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            });
            proc?.WaitForExit(500);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR WAYLAND INPUT] Execution failed: {ex.Message}");
        }
    }
}
