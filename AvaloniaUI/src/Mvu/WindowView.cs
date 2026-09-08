using Avalonia.Controls;

namespace Lytec.AvaloniaUI.Mvu;

public record WindowInfo
{
    public string Title { get; init; } = "Window";
    public double Width { get; init; } = 800;
    public double Height { get; init; } = 600;
    public double MinWidth { get; init; } = 400;
    public double MinHeight { get; init; } = 300;
    public bool CanResize { get; init; } = true;
    public SizeToContent SizeToContent { get; init; } = SizeToContent.WidthAndHeight;
}

public interface IWindowView
{
    WindowInfo WindowOptions { get; }
}
