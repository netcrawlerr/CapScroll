using CapScroll.Core.Models;

namespace CapScroll.Platform.Stitching;

/// <summary>
/// Image processing utility for stitching sequential frame captures into
/// a single continuous vertical image buffer.
/// </summary>
public sealed class Stitcher
{
    /// <summary>
    /// Stitches a sequence of captured frames into one by calculating vertical pixel overlaps.
    /// </summary>
    /// <param name="frames">ordered collection of sequential screen capture frames.</param>
    /// <param name="expectedOverlapHint">Estimated overlap threshold in pixels to guide search limits.</param>
    /// <param name="progress">callback reporting percentage progress (0.0 to 100.0)</param>
    /// <returns>a new <see cref="CaptureFrame"/> containing the complete stitched image composite</returns>
    public CaptureFrame Stitch(
        IReadOnlyList<CaptureFrame> frames,
        int expectedOverlapHint = 100,
        Action<double>? progress = null)
    {
        if (frames.Count == 0)
        {
            throw new ArgumentException(
                "At least one frame is required.",
                nameof(frames));
        }

        if (frames.Count == 1)
        {
            progress?.Invoke(100);
            return frames[0].Clone();
        }

        var width = frames[0].Width;

        var calculatedOffsets =
            new List<int>();

        /*
         * frame after the first one needs to be
         * aligned against the previous frm
         */
        for (var i = 1; i < frames.Count; i++)
        {
            var previous =
                frames[i - 1];

            var current =
                frames[i];

            var bestOverlap =
                DetectOverlap(
                    previous,
                    current,
                    expectedOverlapHint,
                    progress,
                    i,
                    frames.Count);

            var newPixels =
                current.Height - bestOverlap;

            Console.WriteLine(
                $"[DEBUG STITCH] " +
                $"Frame {i - 1} -> Frame {i}: " +
                $"Overlap = {bestOverlap}px | " +
                $"New Content = {newPixels}px");

            calculatedOffsets.Add(
                newPixels);
        }


        // final image height.

        var totalHeight =
            frames[0].Height;

        foreach (var newPixels in calculatedOffsets)
        {
            totalHeight += newPixels;
        }

        var stride =
            width * 4;

        var output =
            new byte[stride * totalHeight];


        // cp 1st frm

        CopyRows(
            frames[0].Pixels,
            frames[0].Stride,
            0,
            output,
            stride,
            0,
            frames[0].Height,
            width);

        var currentDestY =
            frames[0].Height;

        // append new cntnt
        for (var i = 1; i < frames.Count; i++)
        {
            var frame =
                frames[i];

            var newPixels =
                calculatedOffsets[i - 1];

            var sourceStartY =
                frame.Height - newPixels;

            CopyRows(
                frame.Pixels,
                frame.Stride,
                sourceStartY,
                output,
                stride,
                currentDestY,
                newPixels,
                width);

            currentDestY +=
                newPixels;
        }

        progress?.Invoke(100);

        return new CaptureFrame(
            output,
            width,
            totalHeight,
            stride,
            DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// estimates the best vertical overlap pixel count between two consecutive frames
    /// </summary>
    private static int DetectOverlap(
        CaptureFrame previous,
        CaptureFrame current,
        int expectedOverlapHint,
        Action<double>? progress,
        int frameIndex,
        int frameCount)
    {


        // this aint the best way to do it but happens to work well enough
        var minOverlap =
            (int)(previous.Height * 0.20);

        var maxOverlap =
            (int)(previous.Height * 0.90);

        if (expectedOverlapHint > 0)
        {
            var hint =
                Math.Clamp(
                    expectedOverlapHint,
                    minOverlap,
                    maxOverlap);

            Console.WriteLine(
                $"[DEBUG STITCH] " +
                $"Expected overlap hint: {hint}px");
        }

        var bestOverlap =
            minOverlap;

        var minAverageDiff =
            double.MaxValue;

        const int rowStep = 2;
        const int colStep = 4;

        var overlapRange =
            maxOverlap - minOverlap;

        for (
            var candidateOverlap = minOverlap;
            candidateOverlap <= maxOverlap;
            candidateOverlap++)
        {
            long currentDifference = 0;
            long samples = 0;

            var previousStartY =
                previous.Height -
                candidateOverlap;

            for (
                var y = 0;
                y < candidateOverlap;
                y += rowStep)
            {
                var previousY =
                    previousStartY + y;

                var currentY =
                    y;

                var previousRowOffset =
                    previousY * previous.Stride;

                var currentRowOffset =
                    currentY * current.Stride;

                for (
                    var x = 0;
                    x < previous.Width;
                    x += colStep)
                {
                    var pixelOffset =
                        x * 4;

                    var diffR =
                        Math.Abs(
                            previous.Pixels[
                                previousRowOffset +
                                pixelOffset] -
                            current.Pixels[
                                currentRowOffset +
                                pixelOffset]);

                    var diffG =
                        Math.Abs(
                            previous.Pixels[
                                previousRowOffset +
                                pixelOffset +
                                1] -
                            current.Pixels[
                                currentRowOffset +
                                pixelOffset +
                                1]);

                    var diffB =
                        Math.Abs(
                            previous.Pixels[
                                previousRowOffset +
                                pixelOffset +
                                2] -
                            current.Pixels[
                                currentRowOffset +
                                pixelOffset +
                                2]);

                    currentDifference +=
                        diffR +
                        diffG +
                        diffB;

                    samples++;
                }
            }

            var averageDiff =
                samples > 0
                    ? (double)currentDifference / samples
                    : double.MaxValue;

            if (averageDiff < minAverageDiff)
            {
                minAverageDiff =
                    averageDiff;

                bestOverlap =
                    candidateOverlap;
            }

            // progress
            var overlapProgress =
                overlapRange <= 0
                    ? 1.0
                    : (double)(
                        candidateOverlap -
                        minOverlap) /
                      overlapRange;

            var completedFrames =
                frameIndex - 1;

            var progressValue =
                (
                    completedFrames +
                    overlapProgress
                ) /
                (frameCount - 1) *
                100.0;

            progress?.Invoke(
                progressValue);
        }

        return bestOverlap;
    }

    /// <summary>
    /// performs row-by-row memory block copies from source frame pixel buffer to target buffer.
    /// </summary>
    private static void CopyRows(
        byte[] source,
        int sourceStride,
        int sourceY,
        byte[] destination,
        int destinationStride,
        int destinationY,
        int height,
        int width)
    {
        var rowBytes =
            width * 4;

        for (
            var row = 0;
            row < height;
            row++)
        {
            Buffer.BlockCopy(
                source,
                (sourceY + row) *
                    sourceStride,
                destination,
                (destinationY + row) *
                    destinationStride,
                rowBytes);
        }
    }
}
