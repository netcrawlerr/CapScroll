using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace CapScroll.Desktop.Views;

public partial class OverlayWindow : Window
{
    private Point _startPoint;
    private bool _selecting;

    public PixelRect? SelectedRegion { get; private set; }

    public OverlayWindow()
    {
        InitializeComponent();

        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;

        KeyDown += OnKeyDown;

        Opened += OnOpened;
    }

    private void OnOpened(
        object? sender,
        EventArgs e)
    {
        ConfigureFullScreen();
    }

    private void ConfigureFullScreen()
    {
        var screen = Screens.Primary;

        if (screen is null)
            return;

        WindowState = WindowState.Normal;

        Position = screen.Bounds.Position;

        Width = screen.Bounds.Width;
        Height = screen.Bounds.Height;

        Topmost = true;
    }

    private void OnPointerPressed(
        object? sender,
        PointerPressedEventArgs e)
    {
        var point =
            e.GetCurrentPoint(this);

        if (!point.Properties.IsLeftButtonPressed)
            return;

        _startPoint =
            e.GetPosition(this);

        _selecting = true;

        SelectionBorder.IsVisible = true;

        UpdateSelection(
            _startPoint,
            _startPoint);

        e.Handled = true;
    }

    private void OnPointerMoved(
        object? sender,
        PointerEventArgs e)
    {
        if (!_selecting)
            return;

        var current =
            e.GetPosition(this);

        UpdateSelection(
            _startPoint,
            current);
    }

    private void OnPointerReleased(
        object? sender,
        PointerReleasedEventArgs e)
    {
        if (!_selecting)
            return;

        var current =
            e.GetPosition(this);

        var rect =
            CreateRect(
                _startPoint,
                current);

        _selecting = false;

        if (rect.Width < 2 ||
            rect.Height < 2)
        {
            SelectedRegion = null;
            Close();
            return;
        }
        
        var screenPosition =
            Position;

        SelectedRegion =
            new PixelRect(
                rect.X + screenPosition.X,
                rect.Y + screenPosition.Y,
                rect.Width,
                rect.Height);

        Close();
    }

    private void OnKeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SelectedRegion = null;
            Close();
        }
    }

    private void UpdateSelection(
        Point start,
        Point end)
    {
        var rect =
            CreateRect(start, end);

        SelectionBorder.Width =
            rect.Width;

        SelectionBorder.Height =
            rect.Height;

        SelectionBorder.Margin =
            new Thickness(
                rect.X,
                rect.Y,
                0,
                0);

        SelectionBorder.HorizontalAlignment =
            Avalonia.Layout.HorizontalAlignment.Left;

        SelectionBorder.VerticalAlignment =
            Avalonia.Layout.VerticalAlignment.Top;
    }

    private static PixelRect CreateRect(
        Point start,
        Point end)
    {
        var x =
            Math.Min(start.X, end.X);

        var y =
            Math.Min(start.Y, end.Y);

        var right =
            Math.Max(start.X, end.X);

        var bottom =
            Math.Max(start.Y, end.Y);

        return new PixelRect(
            (int)x,
            (int)y,
            Math.Max(0, (int)(right - x)),
            Math.Max(0, (int)(bottom - y)));
    }
}
