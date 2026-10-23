using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using CapScroll.Core.Models;
using SkiaSharp;
using Tmds.DBus;

namespace CapScroll.Platform.Linux.Portals;

[DBusInterface("org.freedesktop.portal.Screenshot")]
public interface IScreenshotPortalDBus : IDBusObject
{
    Task<ObjectPath> ScreenshotAsync(string parentWindow, IDictionary<string, object> options);
}

[DBusInterface("org.freedesktop.portal.RemoteDesktop")]
public interface IRemoteDesktopPortalDBus : IDBusObject
{
    Task<ObjectPath> CreateSessionAsync(IDictionary<string, object> options);
    Task<ObjectPath> SelectDevicesAsync(ObjectPath sessionHandle, IDictionary<string, object> options);
    Task<ObjectPath> StartAsync(ObjectPath sessionHandle, string parentWindow, IDictionary<string, object> options);
    Task NotifyPointerAxisDiscreteAsync(ObjectPath sessionHandle, IDictionary<string, object> options, uint axis, int steps);
}

[DBusInterface("org.freedesktop.portal.Request")]
public interface IPortalRequestDBus : IDBusObject
{
    Task<IDisposable> WatchResponseAsync(
        Action<(uint response, IDictionary<string, object> results)> handler,
        Action<Exception>? onError = null);

    Task CloseAsync();
}

/// <summary>
/// D-Bus service for XDG Desktop Portals
/// </summary>
public static class XdgPortalService
{
    private const string PortalBusName = "org.freedesktop.portal.Desktop";
    private static readonly ObjectPath PortalObjectPath = new("/org/freedesktop/portal/desktop");

    private static readonly SemaphoreSlim ConnectionLock = new(1, 1);
    private static Connection? _connection;
    private static string? _senderName;
    private static IScreenshotPortalDBus? _screenshotProxy;
    private static IRemoteDesktopPortalDBus? _remoteDesktopProxy;
    private static ObjectPath? _remoteDesktopSession;

    public static bool IsAvailable()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DBUS_SESSION_BUS_ADDRESS"));
    }

    public static async Task<CaptureResult> CaptureAsync(
        bool interactive,
        PixelRect? cropRegion = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (connection, senderName, screenshotProxy, _) = await EnsureConnectedAsync(cancellationToken);

            string token = $"capscroll_{Guid.NewGuid():N}";
            var expectedRequestPath = new ObjectPath($"/org/freedesktop/portal/desktop/request/{senderName}/{token}");

            var tcs = new TaskCompletionSource<(uint response, IDictionary<string, object> results)>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            using var reg = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

            var requestProxy = connection.CreateProxy<IPortalRequestDBus>(PortalBusName, expectedRequestPath);
            using var signalWatcher = await requestProxy.WatchResponseAsync(
                data => tcs.TrySetResult(data),
                ex => tcs.TrySetException(ex)
            );

            var dbusOptions = new Dictionary<string, object>
            {
                ["handle_token"] = token,
                ["interactive"] = interactive,
                ["modal"] = false
            };

            ObjectPath actualPath = await screenshotProxy.ScreenshotAsync(string.Empty, dbusOptions);

            IDisposable? fallbackWatcher = null;
            if (actualPath != expectedRequestPath)
            {
                var actualProxy = connection.CreateProxy<IPortalRequestDBus>(PortalBusName, actualPath);
                fallbackWatcher = await actualProxy.WatchResponseAsync(
                    data => tcs.TrySetResult(data),
                    ex => tcs.TrySetException(ex)
                );
            }

            try
            {
                var (responseCode, results) = await tcs.Task;

                if (responseCode != 0)
                {
                    return CaptureResult.Failed(responseCode == 1
                        ? "Screenshot capture cancelled by user."
                        : $"XDG Screenshot Portal failed with code {responseCode}.");
                }

                if (!results.TryGetValue("uri", out object? uriObj) || uriObj is not string uriString)
                {
                    return CaptureResult.Failed("XDG Portal succeeded, but returned no 'uri'.");
                }

                // string localPath = Uri.UnescapeDataString(new Uri(uriString).LocalPath);

                string localPath;
                if (uriString.StartsWith("file://"))
                {
                    var uri = new Uri(uriString);
                    localPath = Uri.UnescapeDataString(uri.LocalPath);
                }
                else
                {
                    localPath = uriString;
                }


                localPath = localPath.Replace('\\', '/');
                if (localPath.StartsWith("//"))
                {
                    localPath = localPath.TrimStart('/');
                    localPath = "/" + localPath;
                }

                if (!File.Exists(localPath))
                {
                    return CaptureResult.Failed($"Portal screenshot file missing at '{localPath}'.");
                }

                return DecodeToRgbaAndCleanup(localPath, cropRegion);
            }
            finally
            {
                fallbackWatcher?.Dispose();
            }
        }
        catch (OperationCanceledException)
        {
            return CaptureResult.Failed("Screenshot capture was cancelled.");
        }
        catch (Exception ex)
        {
            return CaptureResult.Failed($"XDG Portal capture error: {ex.Message}");
        }
    }

    public static async Task<bool> ScrollAsync(int clicks, CancellationToken cancellationToken = default)
        {
            try
            {
                var (connection, senderName, _, remoteDesktop) = await EnsureConnectedAsync(cancellationToken);

                if (_remoteDesktopSession is null)
                {
                    _remoteDesktopSession = await InitializeRemoteDesktopSessionAsync(connection, senderName, remoteDesktop, cancellationToken);
                    if (_remoteDesktopSession is null)
                    {
                        return false;
                    }
                }

                // if gnome 1 kde 8x
                string desktopEnv = (Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "").ToUpperInvariant();
                bool isKde = desktopEnv.Contains("KDE") || desktopEnv.Contains("PLASMA");
                int stepsPerClick = isKde ? 8 : 1;

                var emptyOptions = new Dictionary<string, object>();
                for (int i = 0; i < clicks; i++)
                {
                    await remoteDesktop.NotifyPointerAxisDiscreteAsync(_remoteDesktopSession.Value, emptyOptions, 0, stepsPerClick);
                }

                return true;
            }
            catch
            {
                _remoteDesktopSession = null;
                return false;
            }
        }

    private static async Task<ObjectPath?> InitializeRemoteDesktopSessionAsync(
        Connection connection,
        string senderName,
        IRemoteDesktopPortalDBus remoteDesktop,
        CancellationToken ct)
    {
        string sessionToken = $"capscroll_rd_sess_{Guid.NewGuid():N}";

        var (createCode, createResults) = await SendPortalRequestAsync(
            connection,
            senderName,
            token => remoteDesktop.CreateSessionAsync(new Dictionary<string, object>
            {
                ["handle_token"] = token,
                ["session_handle_token"] = sessionToken,
                ["persist_mode"] = (uint)2
            }),
            ct);

        if (createCode != 0 || !createResults.TryGetValue("session_handle", out object? handleObj))
        {
            return null;
        }

        var sessionPath = handleObj is ObjectPath op ? op : new ObjectPath(handleObj.ToString()!);

        var (selectCode, _) = await SendPortalRequestAsync(
            connection,
            senderName,
            token => remoteDesktop.SelectDevicesAsync(sessionPath, new Dictionary<string, object>
            {
                ["handle_token"] = token,
                ["types"] = (uint)2
            }),
            ct);

        if (selectCode != 0) return null;

        var (startCode, _) = await SendPortalRequestAsync(
            connection,
            senderName,
            token => remoteDesktop.StartAsync(sessionPath, string.Empty, new Dictionary<string, object>
            {
                ["handle_token"] = token
            }),
            ct);

        return startCode == 0 ? sessionPath : null;
    }

    private static async Task<(uint response, IDictionary<string, object> results)> SendPortalRequestAsync(
        Connection connection,
        string senderName,
        Func<string, Task<ObjectPath>> portalCall,
        CancellationToken ct)
    {
        string token = $"capscroll_req_{Guid.NewGuid():N}";
        var expectedPath = new ObjectPath($"/org/freedesktop/portal/desktop/request/{senderName}/{token}");

        var tcs = new TaskCompletionSource<(uint, IDictionary<string, object>)>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        using var reg = ct.Register(() => tcs.TrySetCanceled(ct));

        var requestProxy = connection.CreateProxy<IPortalRequestDBus>(PortalBusName, expectedPath);
        using var watcher = await requestProxy.WatchResponseAsync(
            data => tcs.TrySetResult(data),
            ex => tcs.TrySetException(ex));

        await portalCall(token);
        return await tcs.Task;
    }

    private static async Task<(Connection, string, IScreenshotPortalDBus, IRemoteDesktopPortalDBus)> EnsureConnectedAsync(CancellationToken ct)
    {
        if (_connection is not null && _senderName is not null && _screenshotProxy is not null && _remoteDesktopProxy is not null)
        {
            return (_connection, _senderName, _screenshotProxy, _remoteDesktopProxy);
        }

        await ConnectionLock.WaitAsync(ct);
        try
        {
            if (_connection is null)
            {
                var conn = new Connection(Address.Session);
                var info = await conn.ConnectAsync();
                _senderName = info.LocalName.TrimStart(':').Replace('.', '_');
                _screenshotProxy = conn.CreateProxy<IScreenshotPortalDBus>(PortalBusName, PortalObjectPath);
                _remoteDesktopProxy = conn.CreateProxy<IRemoteDesktopPortalDBus>(PortalBusName, PortalObjectPath);
                _connection = conn;
            }

            return (_connection, _senderName!, _screenshotProxy!, _remoteDesktopProxy!);
        }
        finally
        {
            ConnectionLock.Release();
        }
    }

    private static CaptureResult DecodeToRgbaAndCleanup(string filePath, PixelRect? cropRegion)
    {
        try
        {
            using var bitmap = SKBitmap.Decode(filePath);
            if (bitmap is null)
            {
                return CaptureResult.Failed("Failed to decode XDG Portal image with SkiaSharp.");
            }

            SKBitmap targetBitmap = bitmap;
            bool disposeTarget = false;

            try
            {
                if (cropRegion.HasValue)
                {
                    var r = cropRegion.Value;
                    var fullRect = new SKRectI(0, 0, bitmap.Width, bitmap.Height);
                    var clamped = new SKRectI(r.X, r.Y, r.X + r.Width, r.Y + r.Height);
                    clamped.Intersect(fullRect);

                    if (clamped.Width <= 0 || clamped.Height <= 0)
                    {
                        return CaptureResult.Failed("Selected region is outside screen bounds.");
                    }

                    targetBitmap = new SKBitmap();
                    if (!bitmap.ExtractSubset(targetBitmap, clamped))
                    {
                        targetBitmap.Dispose();
                        return CaptureResult.Failed("Failed to crop region from full screenshot.");
                    }
                    disposeTarget = true;
                }

                var info = new SKImageInfo(
                    targetBitmap.Width,
                    targetBitmap.Height,
                    SKColorType.Rgba8888,
                    SKAlphaType.Premul);

                int stride = info.RowBytes;
                byte[] pixels = new byte[info.BytesSize];

                GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
                try
                {
                    using var srcPixmap = targetBitmap.PeekPixels();
                    if (srcPixmap is null || !srcPixmap.ReadPixels(info, handle.AddrOfPinnedObject(), stride, 0, 0))
                    {
                        return CaptureResult.Failed("Failed to convert pixel buffer to RGBA8888.");
                    }
                }
                finally
                {
                    handle.Free();
                }

                return CaptureResult.FromPixels(pixels, info.Width, info.Height, stride);
            }
            finally
            {
                if (disposeTarget)
                {
                    targetBitmap.Dispose();
                }
            }
        }
        finally
        {
            try { File.Delete(filePath); } catch { }
        }
    }
}
