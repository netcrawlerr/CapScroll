using Avalonia;
using CapScroll.Core.Interfaces;
using CapScroll.Core.Models;
using CapScroll.Platform.Linux.X11;
using CapScroll.Platform.Stitching;

namespace CapScroll.Platform.Engine;

/// <summary>
/// all about the capturings: regional or scrolling.
/// </summary>
public sealed class CapScrollEngine
{
    private readonly ICaptureBackend _captureBackend;
    private readonly Stitcher _stitcher;

    /// <summary>
    /// initializes a new instance of the <see cref="CapScrollEngine"/> class with a specified capture provider.
    /// </summary>
    /// <param name="captureBackend">The screen capture backend instance (e.g., X11 or platform-native implementation).</param>
    public CapScrollEngine(
        ICaptureBackend captureBackend)
    {
        _captureBackend =
            captureBackend;

        _stitcher =
            new Stitcher();
    }

    /// <summary>
    /// gets the display name of the active capture backend.
    /// </summary>
    public string BackendName =>
        _captureBackend.Name;

    /// <summary>
    /// gets a value indicating whether the configured capture backend is supported and available on the current host.
    /// </summary>
    public bool IsAvailable =>
        _captureBackend.IsAvailable;

    /// <summary>
    /// Asynchronously captures a single static screen region.
    /// </summary>
    /// <param name="region">The target bounding box pixel coordinates and dimensions.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task containing the captured region pixel data or failure details.</returns>
    public Task<CaptureResult> CaptureRegionAsync(
        PixelRect region,
        CancellationToken cancellationToken = default)
    {
        return _captureBackend.CaptureRegionAsync(
            region,
            cancellationToken);
    }

    /// <summary>
    /// incremental mouse scrolling, consecutive frame acquisition, end-of-scroll boundary detection,
    /// and vertical image stitching into a single combined <see cref="CaptureFrame"/>.
    /// </summary>
    /// <param name="region">The target rectangular area on the screen to scroll and capture.</param>
    /// <param name="scrollClicks">The number of scroll steps/notches to send via synthesized input per iteration.</param>
    /// <param name="scrollDelayMilliseconds">The delay in milliseconds to wait after a scroll event to allow UI re-rendering.</param>
    /// <param name="overlap">The estimated pixel overlap target between consecutive captured frames.</param>
    /// <param name="cancellationToken">A token to abort the scrolling and capture loop.</param>
    /// <param name="stitchingStarted">An optional callback invoked when frame acquisition finishes and stitching begins.</param>
    /// <param name="stitchingProgress">An optional callback reporting image stitching progress as a percentage ratio (0.0 to 1.0).</param>
    /// <returns>A task containing the fully stitched composite <see cref="CaptureFrame"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when inputs for scroll clicks, delays, or overlap are negative or invalid.</exception>
    /// <exception cref="ArgumentException">Thrown when the selected capture region height is too small to perform scrolling.</exception>
    /// <exception cref="NotSupportedException">Thrown if the current underlying capture backend is not supported for scroll automation.</exception>
    /// <exception cref="InvalidOperationException">Thrown if initial capture fails or no valid frames are acquired.</exception>
    public async Task<CaptureFrame> CaptureScrollingAsync(
        PixelRect region,
        int scrollClicks = 8,
        int scrollDelayMilliseconds = 700,
        int overlap = 100,
        CancellationToken cancellationToken = default,
        Action? stitchingStarted = null,
        Action<double>? stitchingProgress = null)
    {
        if (scrollClicks <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(scrollClicks));
        }

        if (scrollDelayMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(scrollDelayMilliseconds));
        }

        if (overlap < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(overlap),
                "Overlap cannot be negative.");
        }

        if (region.Height <= 10)
        {
            throw new ArgumentException(
                "Selected region height is too small for scrolling capture.",
                nameof(region));
        }

        var effectiveOverlap =
            Math.Min(
                overlap,
                region.Height / 2);

        if (_captureBackend is not X11Screenshot)
        {
            throw new NotSupportedException(
                "Scrolling capture is currently implemented for X11 only.");
        }

        Console.WriteLine(
            $"\n[DEBUG CAPTURE START] " +
            $"Region: {region.Width}x{region.Height} " +
            $"at ({region.X},{region.Y}) | " +
            $"Scroll Clicks: {scrollClicks} | " +
            $"Delay: {scrollDelayMilliseconds}ms");

        var frames =
            new List<CaptureFrame>();

        var display =
            X11Interop.OpenDisplay();

        try
        {
            X11Window.MovePointerToRegionCenter(
                display,
                region);

            await Task.Delay(150);

            // Capture initial frame (Frame 0)
            var firstResult =
                await _captureBackend
                    .CaptureRegionAsync(region);

            if (!firstResult.Success ||
                firstResult.Pixels is null)
            {
                throw new InvalidOperationException(
                    firstResult.Error ??
                    "Initial capture failed.");
            }

            var previousFrame =
                new CaptureFrame(
                    firstResult.Pixels,
                    firstResult.Width,
                    firstResult.Height,
                    firstResult.Stride,
                    DateTimeOffset.UtcNow);

            frames.Add(
                previousFrame);

            Console.WriteLine(
                $"[DEBUG CAPTURE] " +
                $"Frame 0 captured successfully " +
                $"({previousFrame.Width}x{previousFrame.Height}).");

            // Execute automated scrolling acquisition loop
            var frameIndex = 1;

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine(
                        "[DEBUG CAPTURE] " +
                        "Cancellation requested before scroll.");

                    break;
                }

                X11Input.ScrollDown(
                    display,
                    scrollClicks);

                await Task.Delay(
                    scrollDelayMilliseconds);

                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine(
                        "[DEBUG CAPTURE] " +
                        "Cancellation requested after delay.");

                    break;
                }

                var result =
                    await _captureBackend
                        .CaptureRegionAsync(region);

                if (!result.Success ||
                    result.Pixels is null)
                {
                    throw new InvalidOperationException(
                        result.Error ??
                        "Capture failed.");
                }

                var currentFrame =
                    new CaptureFrame(
                        result.Pixels,
                        result.Width,
                        result.Height,
                        result.Stride,
                        DateTimeOffset.UtcNow);

                var isSimilar =
                    AreFramesSimilar(
                        previousFrame,
                        currentFrame,
                        out var diffRatio);

                Console.WriteLine(
                    $"[DEBUG CAPTURE] " +
                    $"Frame {frameIndex} captured. " +
                    $"Change Ratio: {diffRatio:P2} | " +
                    $"End of page reached? {isSimilar}");

                if (isSimilar)
                {
                    Console.WriteLine(
                        $"[DEBUG CAPTURE] " +
                        $"Reached bottom of content at " +
                        $"frame {frameIndex}. " +
                        $"Stopping capture.");

                    break;
                }

                frames.Add(
                    currentFrame);

                previousFrame =
                    currentFrame;

                frameIndex++;
            }
        }
        finally
        {
            X11Interop.CloseDisplay(
                display);
        }

        if (frames.Count == 0)
        {
            throw new InvalidOperationException(
                "No frames were captured.");
        }

        stitchingStarted?.Invoke();

        Console.WriteLine(
            $"[DEBUG STITCH] " +
            $"Total frames captured: {frames.Count}. " +
            $"Starting dynamic alignment...");

        return _stitcher.Stitch(
            frames,
            effectiveOverlap,
            stitchingProgress);
    }

    /// <summary>
    /// pixel grids between two consecutive frames to determine if content stopped scrolling (bottom of page reached).
    /// </summary>
    /// <param name="first">The baseline image frame to compare.</param>
    /// <param name="second">The newly acquired image frame to compare against.</param>
    /// <param name="changedRatio">When this method returns, contains the ratio of sampled pixels that exceeded tolerance (0.0 to 1.0).</param>
    /// <returns>True if the frames are substantially identical (indicating no new scroll content); otherwise, false.</returns>
    private static bool AreFramesSimilar(
        CaptureFrame first,
        CaptureFrame second,
        out double changedRatio)
    {
        changedRatio = 0.0;

        if (first.Width != second.Width ||
            first.Height != second.Height ||
            first.Stride != second.Stride)
        {
            return false;
        }

        const int sampleStep = 10;
        const int pixelTolerance = 30;

        var totalSamples = 0;
        var differentSamples = 0;

        for (
            var y = 0;
            y < first.Height;
            y += sampleStep)
        {
            for (
                var x = 0;
                x < first.Width;
                x += sampleStep)
            {
                var firstIndex =
                    y * first.Stride +
                    x * 4;

                var secondIndex =
                    y * second.Stride +
                    x * 4;

                var redDifference =
                    Math.Abs(
                        first.Pixels[firstIndex] -
                        second.Pixels[secondIndex]);

                var greenDifference =
                    Math.Abs(
                        first.Pixels[firstIndex + 1] -
                        second.Pixels[secondIndex + 1]);

                var blueDifference =
                    Math.Abs(
                        first.Pixels[firstIndex + 2] -
                        second.Pixels[secondIndex + 2]);

                var difference =
                    redDifference +
                    greenDifference +
                    blueDifference;

                if (difference >
                    pixelTolerance)
                {
                    differentSamples++;
                }

                totalSamples++;
            }
        }

        if (totalSamples == 0)
        {
            return true;
        }

        changedRatio =
            (double)differentSamples /
            totalSamples;

        return changedRatio < 0.01;
    }
}
