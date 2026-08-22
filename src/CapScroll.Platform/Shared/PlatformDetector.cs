using CapScroll.Platform.Linux;

namespace CapScroll.Platform.Shared;

public static class PlatformDetector
{
    public static PlatformInfo Detect()
    {
        if (OperatingSystem.IsLinux())
        {
            return new PlatformInfo()
            {
                SessionType = LinuxSessionType.Unknown
            };
        }

        var session = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE")
            ?.Trim()
            .ToLowerInvariant();
        
        var desktop =
            Environment.GetEnvironmentVariable(
                "XDG_CURRENT_DESKTOP")
            ?? "Unknown";

        var display =
            Environment.GetEnvironmentVariable("DISPLAY");
        
        var hasX11 =
            !string.IsNullOrWhiteSpace(display) &&
            X11Interop.IsAvailable(); // i will create this interop later
        
        var sessionType = session switch
        {
            "x11" => LinuxSessionType.X11,

            _ => LinuxSessionType.Unknown
        };

        return new PlatformInfo
        {
            SessionType = sessionType,
            DesktopEnvironment = desktop,
            HasX11 = hasX11,
            Display = display,

        };
    }

}
