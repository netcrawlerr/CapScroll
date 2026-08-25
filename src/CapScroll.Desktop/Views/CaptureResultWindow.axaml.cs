using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace CapScroll.Desktop.Views;

public partial class CaptureResultWindow : Window
{
    private readonly string? _initialFilePath;

    private readonly string _captureDirectory;

    private Bitmap? _currentBitmap;

    private readonly List<Bitmap> _thumbnailBitmaps = new();

    private string? _selectedFilePath;

    private bool _isClosed;


    public CaptureResultWindow(
        string? filePath = null)
    {
        InitializeComponent();

        _initialFilePath = filePath;

        _captureDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.MyPictures),
                "CapScroll");

        Closed += OnWindowClosed;

        LoadCaptures();
    }

    private void LoadCaptures()
    {
        CapturesPanel.Children.Clear();

        DisposeThumbnails();

        if (!Directory.Exists(_captureDirectory))
        {
            Directory.CreateDirectory(
                _captureDirectory);

            CaptureCountText.Text =
                "0 captures";

            FileNameText.Text =
                "No captures yet.";

            DimensionsText.Text =
                string.Empty;

            LocationText.Text =
                _captureDirectory;

            return;
        }


        var files =
            Directory
                .EnumerateFiles(
                    _captureDirectory,
                    "*.png",
                    SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.CreationTime)
                .ToList();


        CaptureCountText.Text =
            files.Count == 1
                ? "1 capture"
                : $"{files.Count} captures";


        if (files.Count == 0)
        {
            FileNameText.Text =
                "No captures yet.";

            DimensionsText.Text =
                string.Empty;

            LocationText.Text =
                _captureDirectory;

            return;
        }


        /*
         *
         * creates the gallery entries only.
         *
         * does NOT load the full PNGs.
         */
        foreach (var file in files)
        {
            AddCaptureItem(file);
        }


        /*
         * load only the selected one
         */
        var selectedFile =
            !string.IsNullOrWhiteSpace(_initialFilePath) &&
            File.Exists(_initialFilePath)
                ? _initialFilePath
                : files[0].FullName;


        LoadCapture(selectedFile);
    }


    private void AddCaptureItem(
        FileInfo file)
    {
        var container =
            new Border
            {
                Padding =
                    new Thickness(8),

                CornerRadius =
                    new CornerRadius(6),

                Background =
                    new SolidColorBrush(
                        Color.Parse("#191919")),

                BorderThickness =
                    new Thickness(1),

                BorderBrush =
                    new SolidColorBrush(
                        Color.Parse("#292929")),

                Cursor =
                    new Avalonia.Input.Cursor(
                        Avalonia.Input.StandardCursorType.Hand)
            };


        var grid =
            new Grid
            {
                ColumnDefinitions =
                    new ColumnDefinitions(
                        "75,*"),

                ColumnSpacing = 10
            };


        var thumbnail =
            new Image
            {
                Width = 75,
                Height = 55,
                Stretch = Stretch.UniformToFill
            };



        try
        {
            using var stream =
                File.OpenRead(
                    file.FullName);

            var bitmap =
                Bitmap.DecodeToWidth(
                    stream,
                    35); // its gonna be super blurry but meh idk what else to do

            _thumbnailBitmaps.Add(bitmap);

            thumbnail.Source =
                bitmap;
        }
        catch
        {
            //
        }


        Grid.SetColumn(
            thumbnail,
            0);


        var name =
            new TextBlock
            {
                Text =
                    file.Name,

                FontWeight =
                    FontWeight.SemiBold,

                TextWrapping =
                    TextWrapping.Wrap,

                MaxHeight = 40
            };


        var date =
            new TextBlock
            {
                Text =
                    file.CreationTime.ToString(
                        "yyyy-MM-dd HH:mm"),

                FontSize = 12,

                Opacity = 0.55
            };


        var information =
            new StackPanel
            {
                Spacing = 4,

                VerticalAlignment =
                    Avalonia.Layout.VerticalAlignment.Center
            };


        information.Children.Add(name);
        information.Children.Add(date);

        Grid.SetColumn(
            information,
            1);


        grid.Children.Add(
            thumbnail);

        grid.Children.Add(
            information);

        container.Child =
            grid;


        container.PointerPressed +=
            (_, _) =>
            {
                LoadCapture(
                    file.FullName);
            };


        CapturesPanel.Children.Add(
            container);
    }

    private void LoadCapture(
        string filePath)
    {
        if (_isClosed)
        {
            return;
        }


        if (!File.Exists(filePath))
        {
            FileNameText.Text =
                "Capture file not found.";

            DimensionsText.Text =
                string.Empty;

            LocationText.Text =
                filePath;

            return;
        }


        try
        {
            ReleaseCurrentBitmap();


            var fileInfo =
                new FileInfo(filePath);


            _selectedFilePath =
                fileInfo.FullName;


            FileNameText.Text =
                fileInfo.Name;


            LocationText.Text =
                $"Location: {fileInfo.DirectoryName}";


            using var stream =
                File.OpenRead(
                    fileInfo.FullName);


            _currentBitmap =
                new Bitmap(stream);


            PreviewImage.Source =
                _currentBitmap;


            DimensionsText.Text =
                $"Dimensions: " +
                $"{_currentBitmap.PixelSize.Width} × " +
                $"{_currentBitmap.PixelSize.Height} px";
        }
        catch (Exception ex)
        {
            ReleaseCurrentBitmap();

            DimensionsText.Text =
                "Unable to load image preview.";

            LocationText.Text =
                $"Location: {filePath}\n" +
                $"Error: {ex.Message}";
        }
    }


    private void ReleaseCurrentBitmap()
    {

        PreviewImage.Source = null;

        _currentBitmap?.Dispose();

        _currentBitmap = null;
    }


    private void OpenImageButton_OnClick(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(
                _selectedFilePath))
        {
            return;
        }


        if (!File.Exists(
                _selectedFilePath))
        {
            return;
        }


        try
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName =
                        _selectedFilePath,

                    UseShellExecute = true
                });
        }
        catch (Exception ex)
        {
            _ = ShowErrorAsync(
                "Unable to open image",
                ex.Message);
        }
    }


    private void OpenFolderButton_OnClick(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(
                _selectedFilePath))
        {
            return;
        }


        if (!File.Exists(
                _selectedFilePath))
        {
            return;
        }


        try
        {
            var directory =
                Path.GetDirectoryName(
                    _selectedFilePath);


            if (string.IsNullOrWhiteSpace(
                    directory))
            {
                return;
            }


            Process.Start(
                new ProcessStartInfo
                {
                    FileName =
                        directory,

                    UseShellExecute = true
                });
        }
        catch (Exception ex)
        {
            _ = ShowErrorAsync(
                "Unable to open folder",
                ex.Message);
        }
    }

    private void CloseButton_OnClick(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }


    private void OnWindowClosed(
        object? sender,
        EventArgs e)
    {
        ReleaseAllGalleryMemory();

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


    private void DisposeThumbnails()
    {
        foreach (var bitmap in
                 _thumbnailBitmaps)
        {
            bitmap.Dispose();
        }

        _thumbnailBitmaps.Clear();
    }


    private async Task ShowErrorAsync(
        string title,
        string message)
    {
        var window =
            new Window
            {
                Width = 500,

                Height = 200,

                Title = title,

                WindowStartupLocation =
                    WindowStartupLocation.CenterOwner
            };


        var text =
            new TextBlock
            {
                Text =
                    message,

                TextWrapping =
                    TextWrapping.Wrap,

                Margin =
                    new Thickness(20)
            };


        window.Content =
            text;


        await window.ShowDialog(this);
    }

    private void ReleaseAllGalleryMemory()
    {
        PreviewImage.Source = null;

        if (_currentBitmap is not null)
        {
            _currentBitmap.Dispose();
            _currentBitmap = null;
        }

        foreach (var child in CapturesPanel.Children)
        {
            if (child is Border border &&
                border.Child is Grid grid)
            {
                foreach (var gridChild in grid.Children)
                {
                    if (gridChild is Image image)
                    {
                        image.Source = null;
                    }
                }
            }
        }

        CapturesPanel.Children.Clear();

        foreach (var bitmap in _thumbnailBitmaps)
        {
            bitmap.Dispose();
        }

        _thumbnailBitmaps.Clear();

        _selectedFilePath = null;
    }
}
