using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CapScroll.Core.Interfaces;
using CapScroll.Desktop.Views;
using CapScroll.Platform.Engine;
using CapScroll.Platform.Linux;
using CapScroll.Platform.Shared;

namespace CapScroll.Desktop;

public partial class MainWindow : Window
{
    private readonly PlatformInfo _platformInfo;

    private readonly ICaptureBackend _captureBackend;

    private readonly CapScrollEngine _captureEngine;

    private CancellationTokenSource? _scrollCaptureCancellation;

    private bool _isInitialized;
    
    public MainWindow()
    {
        InitializeComponent();
        _platformInfo =
            PlatformDetector.Detect();

        _captureBackend =
            PlatformDetector.CreateCaptureBackend();

        _captureEngine =
            new CapScrollEngine(
                _captureBackend);
    }
    
    
   // save cap

    private static async Task<string> SaveCaptureAsync(
        byte[] pixels,
        int width,
        int height,
        int stride)
    {
        var directory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.MyPictures),
                "CapScroll");


        Directory.CreateDirectory(
            directory);


        var filename =
            $"capture-{DateTime.Now:yyyyMMdd-HHmmss}.png";


        var path =
            Path.Combine(
                directory,
                filename);


        using var bitmap =
            new WriteableBitmap(
                new PixelSize(
                    width,
                    height),

                new Vector(
                    96,
                    96),

                PixelFormat.Rgba8888,

                AlphaFormat.Opaque);


        using (var framebuffer =
               bitmap.Lock())
        {
            for (var y = 0;
                 y < height;
                 y++)
            {
                var sourceOffset =
                    y * stride;

                var destinationOffset =
                    y * framebuffer.RowBytes;

                var bytes =
                    Math.Min(
                        stride,
                        framebuffer.RowBytes);


                System.Runtime.InteropServices.Marshal.Copy(
                    pixels,
                    sourceOffset,
                    framebuffer.Address +
                    destinationOffset,
                    bytes);
            }
        }
        
        using var stream =
            File.Create(path);

        bitmap.Save(stream);


        await stream.FlushAsync();


        return path;
    }


   // msg window

    private async Task ShowMessage(
        string title,
        string message)
    {
        var window =
            new Window
            {
                Width = 600,
                Height = 250,
                Title = title,
                WindowStartupLocation =
                    WindowStartupLocation.CenterOwner
            };


        var text =
            new TextBlock
            {
                Text = message,

                TextWrapping =
                    Avalonia.Media.TextWrapping.Wrap,

                Margin =
                    new Thickness(20)
            };


        window.Content = text;


        await window.ShowDialog(this);
    }

    private void ViewCapturesButton_OnClick(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private async void CaptureButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        try
        {
            if (!_captureBackend.IsAvailable)
            {
                await ShowMessage(
                    "Capture unavailable",
                    $"The {_captureBackend.Name} capture backend " +
                    "is not available on this system.");

                return;
            }


            var overlay =
                new OverlayWindow();

            await overlay.ShowDialog(this);


            if (overlay.SelectedRegion
                is not PixelRect region)
            {
                return;
            }


            CaptureButton.IsEnabled = false;

            CapScrollButton.IsEnabled = false;


            var result =
                await _captureEngine
                    .CaptureRegionAsync(
                        region);


            if (!result.Success ||
                result.Pixels is null)
            {
                await ShowMessage(
                    "Capture failed",
                    result.Error ??
                    "Unknown capture error.");

                return;
            }


            var file =
                await SaveCaptureAsync(
                    result.Pixels,
                    result.Width,
                    result.Height,
                    result.Stride);


            var resultWindow =
                new CaptureResultWindow(
                    file);

            await resultWindow.ShowDialog(this);
        }
        catch (Exception ex)
        {
            await ShowMessage(
                "Error",
                ex.ToString());
        }
        finally
        {
            CaptureButton.IsEnabled = true;

            CapScrollButton.IsEnabled = true;
        }
    }

    private void CapScrollButton_OnClick(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }
}
