using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CapScroll.Core.Interfaces;
using CapScroll.Core.Models;
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

    private sealed record Palette(
        string Background,
        string Surface,
        string Border,
        string BorderSubtle,
        string Accent,
        string ButtonBackground,
        string ButtonHover,
        string ButtonPressed,
        string PrimaryText,
        string SecondaryText);

    private static readonly Palette MidnightPalette =
        new(
            Background: "#0B0F14",
            Surface: "#111820",
            Border: "#293340",
            BorderSubtle: "#202832",
            Accent: "#4F8CFF",
            ButtonBackground: "#182230",
            ButtonHover: "#223149",
            ButtonPressed: "#2B4161",
            PrimaryText: "#F4F7FB",
            SecondaryText: "#9BA8B8");

    private static readonly Palette OceanPalette =
        new(
            Background: "#07151C",
            Surface: "#0D2029",
            Border: "#1D3C4A",
            BorderSubtle: "#16313D",
            Accent: "#19A7CE",
            ButtonBackground: "#102B36",
            ButtonHover: "#164252",
            ButtonPressed: "#1C5365",
            PrimaryText: "#F1FAFD",
            SecondaryText: "#8EAFBA");

    private static readonly Palette SlatePalette =
        new(
            Background: "#111316",
            Surface: "#191C20",
            Border: "#343940",
            BorderSubtle: "#282C31",
            Accent: "#8B95A5",
            ButtonBackground: "#22262C",
            ButtonHover: "#2D333B",
            ButtonPressed: "#383F49",
            PrimaryText: "#F1F2F4",
            SecondaryText: "#A3A8B0");

    private static readonly Palette PurplePalette =
        new(
            Background: "#100B18",
            Surface: "#181021",
            Border: "#38264A",
            BorderSubtle: "#2A1D36",
            Accent: "#A970FF",
            ButtonBackground: "#21152E",
            ButtonHover: "#302043",
            ButtonPressed: "#3C2855",
            PrimaryText: "#F8F3FF",
            SecondaryText: "#B7A9C5");

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

        _isInitialized = true;

        ApplyPalette(
            MidnightPalette);

        UpdatePlatformInformation();
    }


    private void PaletteComboBox_OnSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (!_isInitialized)
        {
            return;
        }


        if (PaletteComboBox is null)
        {
            return;
        }

        var palette =
            PaletteComboBox.SelectedIndex switch
            {
                0 => MidnightPalette,
                1 => OceanPalette,
                2 => SlatePalette,
                3 => PurplePalette,
                _ => MidnightPalette
            };


        ApplyPalette(palette);
    }


    private void ApplyPalette(
        Palette palette)
    {
        if (Application.Current is null)
        {
            return;
        }


        var resources =
            Application.Current.Resources;


        resources["BackgroundBrush"] =
            CreateBrush(
                palette.Background);

        resources["SurfaceBrush"] =
            CreateBrush(
                palette.Surface);

        resources["BorderBrush"] =
            CreateBrush(
                palette.Border);

        resources["BorderSubtleBrush"] =
            CreateBrush(
                palette.BorderSubtle);

        resources["AccentBrush"] =
            CreateBrush(
                palette.Accent);

        resources["ButtonBackgroundBrush"] =
            CreateBrush(
                palette.ButtonBackground);

        resources["ButtonHoverBackgroundBrush"] =
            CreateBrush(
                palette.ButtonHover);

        resources["ButtonPressedBackgroundBrush"] =
            CreateBrush(
                palette.ButtonPressed);

        resources["PrimaryTextBrush"] =
            CreateBrush(
                palette.PrimaryText);

        resources["SecondaryTextBrush"] =
            CreateBrush(
                palette.SecondaryText);
    }


    private static IBrush CreateBrush(
        string color)
    {
        return new SolidColorBrush(
            Color.Parse(color));
    }


    // update info
    private void UpdatePlatformInformation()
    {
        PlatformText.Text =
            $"Session: {_platformInfo.SessionType} | " +
            $"Desktop: {_platformInfo.DesktopEnvironment}";

        BackendText.Text =
            $"Backend: {_captureBackend.Name} | " +
            $"Available: {_captureBackend.IsAvailable}";

        X11Text.Text =
            $"X11: {_platformInfo.HasX11}";
    }


    // normal capture

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



    // scrolling capture

    private async void CapScrollButton_OnClick(
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

            // hide the window fore starting scrollin cap
            Hide();

            await Task.Delay(150);


            _scrollCaptureCancellation =
                new CancellationTokenSource();


            // capture and stitching in different thread
            var result =
                await RunScrollingCaptureAsync(
                    region);

            UpdateProcessingProgress(
                100,
                "Capture complete",
                "Saving screenshot...");


            var file =
                await SaveCaptureAsync(
                    result.Pixels,
                    result.Width,
                    result.Height,
                    result.Stride);


            HideProcessingOverlay();

            Show();

            var resultWindow =
                new CaptureResultWindow(
                    file);

            await resultWindow.ShowDialog(this);
        }
        catch (OperationCanceledException)
        {
            HideProcessingOverlay();

            Show();

            await ShowMessage(
                "Scrolling capture stopped",
                "The scrolling capture was stopped.");
        }
        catch (Exception ex)
        {
            HideProcessingOverlay();

            Show();

            await ShowMessage(
                "Scrolling capture failed",
                ex.ToString());
        }
        finally
        {
            _scrollCaptureCancellation?.Dispose();

            _scrollCaptureCancellation = null;


            if (!IsVisible)
            {
                Show();
            }

            HideProcessingOverlay();

            CaptureButton.IsEnabled = true;

            CapScrollButton.IsEnabled = true;
        }
    }

    // gallery

    private async void ViewCapturesButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        try
        {
            var resultWindow =
                new CaptureResultWindow();

            await resultWindow.ShowDialog(this);
        }
        catch (Exception ex)
        {
            await ShowMessage(
                "Unable to open captures",
                ex.ToString());
        }
        finally
        {
            GC.Collect(
                GC.MaxGeneration,
                GCCollectionMode.Forced,
                blocking: true,
                compacting: true);

            GC.WaitForPendingFinalizers();

            GC.Collect(
                GC.MaxGeneration,
                GCCollectionMode.Forced,
                blocking: true,
                compacting: true);
        }
    }


    // save
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

    private void UpdateProcessingProgress(
        double progress,
        string status,
        string detail)
    {
        ProcessingProgress.Value =
            Math.Clamp(progress, 0, 100);

        ProcessingPercentage.Text =
            $"{ProcessingProgress.Value:0}%";

        ProcessingStatus.Text =
            status;

        ProcessingDetail.Text =
            detail;
    }


    private void HideProcessingOverlay()
    {
        ProcessingOverlay.IsVisible = false;
    }

    private Task<CaptureFrame> RunScrollingCaptureAsync(
        PixelRect region)
    {
        return Task.Run(
            () =>
                _captureEngine
                    .CaptureScrollingAsync(
                        region,
                        scrollClicks: 3,
                        scrollDelayMilliseconds: 700,
                        overlap: 100,
                        cancellationToken:
                            _scrollCaptureCancellation!.Token,
                        stitchingStarted:
                            OnStitchingStarted,
                        stitchingProgress:
                            OnStitchingProgress));
    }

    private void ShowProcessingOverlay(
        string title,
        string status,
        string detail)
    {
        ProcessingTitle.Text = title;
        ProcessingStatus.Text = status;
        ProcessingDetail.Text = detail;

        ProcessingProgress.Value = 0;
        ProcessingPercentage.Text = "0%";

        ProcessingOverlay.IsVisible = true;
    }


    private void OnStitchingStarted()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(
            () =>
            {
                Show();

                Activate();

                ShowProcessingOverlay(
                    "Processing capture",
                    "Stitching screenshot...",
                    "Combining captured frames");
            });
    }


    private void OnStitchingProgress(
        double progress)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(
            () =>
            {
                UpdateProcessingProgress(
                    progress,
                    "Stitching screenshot...",
                    "Finding the best alignment between captured frames");
            });
    }
}
