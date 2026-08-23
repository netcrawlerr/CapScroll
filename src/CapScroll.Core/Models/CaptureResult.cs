namespace CapScroll.Core.Models;

public sealed class CaptureResult
{
    private CaptureResult(
        bool success,
        byte[]? pixels,
        int width,
        int height,
        int stride,
        string? error)
    {
        Success = success;
        Pixels = pixels;
        Width = width;
        Height = height;
        Stride = stride;
        Error = error;
    }

    public bool Success { get; }

    public byte[]? Pixels { get; }

    public int Width { get; }

    public int Height { get; }

    public int Stride { get; }

    public string? Error { get; }

    public static CaptureResult FromPixels(
        byte[] pixels,
        int width,
        int height,
        int stride)
    {
        return new CaptureResult(
            true,
            pixels,
            width,
            height,
            stride,
            null);
    }

    public static CaptureResult Failed(string error)
    {
        return new CaptureResult(
            false,
            null,
            0,
            0,
            0,
            error);
    }
}
