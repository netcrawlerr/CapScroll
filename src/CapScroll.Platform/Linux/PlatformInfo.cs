namespace CapScroll.Platform.Linux;

/// <summary>
/// runtime system details about the host Linux display.
/// </summary>
public sealed class PlatformInfo
{
    /// <summary>
    /// the active Linux session protocol type (e.g., X11, Wayland ...).
    /// </summary>
    public LinuxSessionType SessionType { get; init; }

    /// <summary>
    /// the desktop environment name (GNOME, KDE, XFCE).
    /// </summary>
    public string DesktopEnvironment { get; init; } = "Unknown";

    /// <summary>
    /// whether an active X11 display server connection is available.
    /// </summary>
    public bool HasX11 { get; init; }

    /// <summary>
    /// the current DISPLAY environment variable value
    /// </summary>
    public string? Display { get; init; }

    /// <summary>
    /// returns a formatted string summary of the detected platform configuration.
    /// </summary>
    public override string ToString()
    {
        return $"Session={SessionType}, " +
               $"Desktop={DesktopEnvironment}, " +
               $"X11={HasX11}";
    }
}
