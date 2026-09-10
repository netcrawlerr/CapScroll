using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using CapScroll.Core.Interfaces;
using CapScroll.Core.Models;

namespace CapScroll.Platform.Linux.Wayland;

public sealed class WaylandScreenshot : ICaptureBackend
{
    public string Name => "Wayland";

    public bool IsAvailable =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));

    public async Task<CaptureResult> CaptureScreenAsync(CancellationToken cancellationToken = default)
    {
        return await CaptureWaylandRegionAsync(null, cancellationToken);
    }

    public async Task<CaptureResult> CaptureRegionAsync(PixelRect region, CancellationToken cancellationToken = default)
    {
        return await CaptureWaylandRegionAsync(region, cancellationToken);
    }

    private static async Task<CaptureResult> CaptureWaylandRegionAsync(
        PixelRect? region,
        CancellationToken cancellationToken)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"capscroll_wayland_{Guid.NewGuid():N}.png");

        try
        {
            // grim
            var startInfo = new ProcessStartInfo
            {
                FileName = "grim",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            if (region.HasValue)
            {
                var r = region.Value;
                startInfo.Arguments = $"-g \"{r.X},{r.Y} {r.Width}x{r.Height}\" \"{tempFile}\"";
            }
            else
            {
                startInfo.Arguments = $"\"{tempFile}\"";
            }

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return CaptureResult.Failed("Failed to spawn process for Wayland screen capture.");
            }

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0 || !File.Exists(tempFile))
            {
                // fallback to gnome-screenshot
                var gnomeProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = "gnome-screenshot",
                    Arguments = $"-f \"{tempFile}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (gnomeProcess is not null)
                {
                    await gnomeProcess.WaitForExitAsync(cancellationToken);
                }
            }

            if (!File.Exists(tempFile))
            {
                return CaptureResult.Failed("Wayland screenshot utility (grim/gnome-screenshot) produced no output file.");
            }

            using var fileBitmap = new Bitmap(tempFile);

            var width = fileBitmap.PixelSize.Width;
            var height = fileBitmap.PixelSize.Height;
            var stride = width * 4;
            var pixels = new byte[height * stride];

            using (var stream = new MemoryStream())
            {
                fileBitmap.Save(stream);
                stream.Position = 0;

                using var writeable = WriteableBitmap.Decode(stream);

                unsafe
                {
                    fixed (byte* pPixels = pixels)
                    {
                        writeable.CopyPixels(new PixelRect(0, 0, width, height), (nint)pPixels, pixels.Length, stride);
                    }
                }
            }

            return CaptureResult.FromPixels(pixels, width, height, stride);
        }
        catch (Exception ex)
        {
            return CaptureResult.Failed($"Wayland capture failed: {ex.Message}");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                try { File.Delete(tempFile); } catch { }
            }
        }
    }
}
