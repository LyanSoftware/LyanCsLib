namespace Lytec.Image
{
    [Flags]
    public enum FontOptions
    {
        None = 0,
        Underline = 1 << 0,
        Strikeout = 1 << 1,
        Overline = 1 << 2,
        Baseline = 1 << 3,
        Antialias = 1 << 4
    }
}
