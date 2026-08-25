namespace CapScroll.Core.Models;

/// <summary>
/// for holding the result state and buffer output of a screen capture.
/// </summary>
public sealed class CaptureResult
{
    /// <summary>
    /// initializes a new instance
    /// </summary>
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

    /// <summary>
    /// whether the capture succeeded.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// output pixel buffer
    /// </summary>
    public byte[]? Pixels { get; }

    /// <summary>
    /// width of the captured image in pixels
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// height of the captured image in pixels
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// stride (byte length per row) of the captured pixel buffer
    /// </summary>
    public int Stride { get; }

    /// <summary>
    /// error message for why the capture operation failed
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// method for creating a capture result containing raw pixel data
    /// </summary>
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

    /// <summary>
    /// method for creating a failed capture result with an error message
    /// </summary>
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
