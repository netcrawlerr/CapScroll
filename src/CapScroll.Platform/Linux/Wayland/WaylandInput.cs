using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;

namespace CapScroll.Platform.Linux.Wayland;

public static class WaylandInput
{
    private static readonly string SocketPath =
        $"/run/user/{(Environment.UserName == "root" ? "0" : "1000")}/.ydotool_socket";

    public static void MovePointerToRegionCenter(PixelRect region)
    {
        EnsureDaemonRunning();

        int centerX = region.X + (region.Width / 2);
        int centerY = region.Y + (region.Height / 2);

        ExecuteProcess("ydotool", $"mousemove -a {centerX} {centerY}");
    }

    // experiemental
    public static void ScrollDown(int clicks = 3)
    {
        EnsureDaemonRunning();

        for (int i = 0; i < clicks; i++)
        {
            ExecuteProcess("ydotool", "click 0xC5");
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
            var startInfo = new ProcessStartInfo
            {
                FileName = "ydotoold",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(startInfo);
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
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            using var proc = Process.Start(psi);
            proc?.WaitForExit(500);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR WAYLAND INPUT] Execution failed: {ex.Message}");
        }
    }
}
