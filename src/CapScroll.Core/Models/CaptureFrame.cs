namespace CapScroll.Core.Models;

public sealed class CaptureFrame
{
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

    public byte[] Pixels { get; }

    public int Width { get; }

    public int Height { get; }

    public int Stride { get; }

    public DateTimeOffset Timestamp { get; }

    public int BytesPerPixel => 4;

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
