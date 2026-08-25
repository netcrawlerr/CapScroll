namespace CapScroll.Core.Models;

/// <summary>
/// represents a raw pixel frame captured from a display buffer.
/// </summary>
public sealed class CaptureFrame
{
    /// <summary>
    /// initializes a new instance of the raw frame data model.
    /// </summary>
    public CaptureFrame(
        byte[] pixels,
        int width,
        int height,
        int stride,
        DateTimeOffset timestamp)
    {
        Pixels = pixels;
        Width = width;
        Height = height;
        Stride = stride;
        Timestamp = timestamp;
    }

    /// <summary>
    /// raw image bytes
    /// </summary>
    public byte[] Pixels { get; }

    /// <summary>
    /// frame width in pixels
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// frame height in pixels
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// byte length per row in the memory buffer
    /// </summary>
    public int Stride { get; }

    /// <summary>
    /// time when the frame was captured
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// color depth per pixel (32-bit color / 4 bytes)
    /// </summary>
    public int BytesPerPixel => 4;

    /// <summary>
    /// creates a deep copy of the capture frame memory array
    /// </summary>
    public CaptureFrame Clone()
    {
        return new CaptureFrame(
            (byte[])Pixels.Clone(),
            Width,
            Height,
            Stride,
            Timestamp);
    }
}
