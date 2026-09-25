using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

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

    private void OnOpened(object? sender, EventArgs e)
    {
        ConfigureFullScreen();
    }

    private void ConfigureFullScreen()
    {
        var screens = Screens.All;
        if (screens is null || screens.Count == 0)
            return;

        // total virtual bounding box across all monitors
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        foreach (var screen in screens)
        {
            var bounds = screen.Bounds;
            minX = Math.Min(minX, bounds.X);
            minY = Math.Min(minY, bounds.Y);
            maxX = Math.Max(maxX, bounds.X + bounds.Width);
            maxY = Math.Max(maxY, bounds.Y + bounds.Height);
        }

        var totalWidth = maxX - minX;
        var totalHeight = maxY - minY;

        WindowState = WindowState.Normal;
        Position = new PixelPoint(minX, minY);
        Width = totalWidth;
        Height = totalHeight;

        Topmost = true;

        PositionInstructionOnPrimaryScreen(minX, minY);
    }

    private void PositionInstructionOnPrimaryScreen(int minX, int minY)
    {
        var primaryScreen = Screens.Primary ?? Screens.All[0];
        var primaryBounds = primaryScreen.Bounds;


        double localPrimaryLeft = primaryBounds.X - minX;
        double localPrimaryTop = primaryBounds.Y - minY;

        double primaryWidth = primaryBounds.Width;
        double instructionWidth = InstructionContainer.Bounds.Width;


        if (instructionWidth <= 0)
        {
            InstructionContainer.Measure(Size.Infinity);
            instructionWidth = InstructionContainer.DesiredSize.Width;
        }

        double leftMargin = localPrimaryLeft + (primaryWidth - instructionWidth) / 2.0;
        double topMargin = localPrimaryTop + 25.0;

        InstructionContainer.Margin = new Thickness(leftMargin, topMargin, 0, 0);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);

        if (!point.Properties.IsLeftButtonPressed)
            return;

        _startPoint = e.GetPosition(this);
        _selecting = true;

        SelectionBorder.IsVisible = true;

        UpdateSelection(_startPoint, _startPoint);

        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_selecting)
            return;

        var current = e.GetPosition(this);
        UpdateSelection(_startPoint, current);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_selecting)
            return;

        var current = e.GetPosition(this);
        var rect = CreateRect(_startPoint, current);

        _selecting = false;

        if (rect.Width < 2 || rect.Height < 2)
        {
            SelectedRegion = null;
            Close();
            return;
        }

        var absoluteX = rect.X + Position.X;
        var absoluteY = rect.Y + Position.Y;

        var targetPoint = new PixelPoint(absoluteX + rect.Width / 2, absoluteY + rect.Height / 2);
        var targetScreen = Screens.ScreenFromPoint(targetPoint) ?? Screens.Primary;
        double scale = targetScreen?.Scaling ?? DesktopScaling;

        SelectedRegion = new PixelRect(
            (int)(absoluteX * scale),
            (int)(absoluteY * scale),
            (int)(rect.Width * scale),
            (int)(rect.Height * scale));

        Close();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SelectedRegion = null;
            Close();
        }
    }

    private void UpdateSelection(Point start, Point end)
    {
        var rect = CreateRect(start, end);

        SelectionBorder.Width = rect.Width;
        SelectionBorder.Height = rect.Height;

        SelectionBorder.Margin = new Thickness(
            rect.X,
            rect.Y,
            0,
            0);

        SelectionBorder.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        SelectionBorder.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
    }

    private static PixelRect CreateRect(Point start, Point end)
    {
        var x = Math.Min(start.X, end.X);
        var y = Math.Min(start.Y, end.Y);

        var right = Math.Max(start.X, end.X);
        var bottom = Math.Max(start.Y, end.Y);

        return new PixelRect(
            (int)x,
            (int)y,
            Math.Max(0, (int)(right - x)),
            Math.Max(0, (int)(bottom - y)));
    }
}
