namespace CapScroll.Platform.Linux;

public sealed class PlatformInfo
{
    public LinuxSessionType SessionType { get; init; }
    public string DesktopEnvironment { get; init; } = "Unknown";
    public bool HasX11 { get; init; }
    public string? Display { get; init; }

    public override string ToString()
    {
        return $"Session={SessionType}, " +
               $"Desktop={DesktopEnvironment}, " +
               $"X11={HasX11}";
    }
}