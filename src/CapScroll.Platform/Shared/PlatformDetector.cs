using System;
using CapScroll.Core.Interfaces;
using CapScroll.Platform.Linux;
using CapScroll.Platform.Linux.Wayland;
using CapScroll.Platform.Linux.X11;

namespace CapScroll.Platform.Shared;

/// <summary>
/// for detecting the host display platform and instantiating appropriate capture backends.
/// </summary>
public static class PlatformDetector
{
    /// <summary>
    /// detects current OS environment variables, session types, and display server availability.
    /// </summary>
    /// <returns>A populated <see cref="PlatformInfo"/> containing host environment details.</returns>
    public static PlatformInfo Detect()
    {
        if (!OperatingSystem.IsLinux())
        {
            return new PlatformInfo
            {
                SessionType = LinuxSessionType.Unknown
            };
        }

        var session =
            Environment.GetEnvironmentVariable(
                    "XDG_SESSION_TYPE")
                ?.Trim()
                .ToLowerInvariant();

        var desktop =
            Environment.GetEnvironmentVariable(
                "XDG_CURRENT_DESKTOP")
            ?? "Unknown";

        var display =
            Environment.GetEnvironmentVariable("DISPLAY");

        var waylandDisplay =
            Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");

        var hasX11 =
            !string.IsNullOrWhiteSpace(display) &&
            X11Interop.IsAvailable();

        var hasWayland =
            !string.IsNullOrWhiteSpace(waylandDisplay);



        var sessionType = session switch
        {
            "x11" => LinuxSessionType.X11,
            "wayland" => LinuxSessionType.Wayland,
            _ => LinuxSessionType.Unknown
        };

        return new PlatformInfo
        {
            SessionType = sessionType,
            DesktopEnvironment = desktop,
            HasX11 = hasX11,
            HasWayland = hasWayland,
            Display = display,
            WaylandDisplay = waylandDisplay,
        };
    }

    /// <summary>
    /// constructs and returns the matching <see cref="ICaptureBackend"/> implementation.
    /// </summary>
    public static ICaptureBackend CreateCaptureBackend()
    {
        var platform = Detect();

        return platform.SessionType switch
        {
            LinuxSessionType.X11 =>
                new X11Screenshot(),
            LinuxSessionType.Wayland =>
                new WaylandScreenshot(),
            _ => throw new NotSupportedException()
        };
    }
}
