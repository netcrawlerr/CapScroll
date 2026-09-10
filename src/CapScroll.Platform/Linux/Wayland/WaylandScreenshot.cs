using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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
            bool capturedByGrim = false;

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

            try
            {
                using var process = Process.Start(startInfo);
                if (process is not null)
                {
                    await process.WaitForExitAsync(cancellationToken);
                    if (process.ExitCode == 0 && File.Exists(tempFile))
                    {
                        capturedByGrim = true;
                    }
                }
            }
            catch
            {
                // ig
            }

            // 2. fallback to gnome-screenshot
            if (!capturedByGrim)
            {
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
                return CaptureResult.Failed("Wayland screenshot utility produced no output file.");
            }

            using var loadedBitmap = new Bitmap(tempFile);

            int fullWidth = loadedBitmap.PixelSize.Width;
            int fullHeight = loadedBitmap.PixelSize.Height;

            PixelRect cropRect = new PixelRect(0, 0, fullWidth, fullHeight);

            if (!capturedByGrim && region.HasValue)
            {
                var r = region.Value;
                int clampX = Math.Clamp(r.X, 0, fullWidth);
                int clampY = Math.Clamp(r.Y, 0, fullHeight);
                int clampW = Math.Min(r.Width, fullWidth - clampX);
                int clampH = Math.Min(r.Height, fullHeight - clampY);

                if (clampW > 0 && clampH > 0)
                {
                    cropRect = new PixelRect(clampX, clampY, clampW, clampH);
                }
            }

            int targetWidth = cropRect.Width;
            int targetHeight = cropRect.Height;
            int stride = targetWidth * 4;
            var pixels = new byte[targetHeight * stride];

            using var stream = File.OpenRead(tempFile);
            using var sourceBitmap = WriteableBitmap.Decode(stream);

            using var writeable = new WriteableBitmap(
                new PixelSize(targetWidth, targetHeight),
                new Vector(96, 96),
                Avalonia.Platform.PixelFormat.Bgra8888,
                Avalonia.Platform.AlphaFormat.Premul);

            using (var fb = writeable.Lock())
            {
                sourceBitmap.CopyPixels(cropRect, fb.Address, targetHeight * stride, fb.RowBytes);
                Marshal.Copy(fb.Address, pixels, 0, pixels.Length);
            }

            // color swapping
            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte temp = pixels[i];
                pixels[i] = pixels[i + 2]; // Swap
                pixels[i + 2] = temp;
            }

            return CaptureResult.FromPixels(pixels, targetWidth, targetHeight, stride);
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
