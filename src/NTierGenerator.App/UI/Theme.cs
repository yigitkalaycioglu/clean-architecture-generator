using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace NTierGenerator.App.UI;

/// <summary>Uygulamanın renkleri, yazı tipleri ve çizimle üretilen logosu.</summary>
internal static class Theme
{
    public static readonly Color Background = ColorTranslator.FromHtml("#F3F4F8");
    public static readonly Color Surface = Color.White;
    public static readonly Color Border = ColorTranslator.FromHtml("#E1E4EB");
    public static readonly Color Header = ColorTranslator.FromHtml("#16122E");
    public static readonly Color HeaderText = Color.White;
    public static readonly Color HeaderMuted = ColorTranslator.FromHtml("#ABA4DA");
    public static readonly Color Accent = ColorTranslator.FromHtml("#512BD4");
    public static readonly Color AccentHover = ColorTranslator.FromHtml("#4422B6");
    public static readonly Color AccentPressed = ColorTranslator.FromHtml("#371B97");
    public static readonly Color AccentDisabled = ColorTranslator.FromHtml("#BDB0EC");
    public static readonly Color TextPrimary = ColorTranslator.FromHtml("#1B1F2A");
    public static readonly Color TextMuted = ColorTranslator.FromHtml("#6B7280");
    public static readonly Color Success = ColorTranslator.FromHtml("#15803D");
    public static readonly Color Warning = ColorTranslator.FromHtml("#B45309");
    public static readonly Color Error = ColorTranslator.FromHtml("#B91C1C");
    public static readonly Color CodeBackground = ColorTranslator.FromHtml("#1E1E2E");
    public static readonly Color CodeText = ColorTranslator.FromHtml("#DCE3F0");

    public static Font SectionFont { get; } = new("Segoe UI Semibold", 8.25F);

    public static Font TitleFont { get; } = new("Segoe UI Semibold", 15F);

    public static Font ButtonFont { get; } = new("Segoe UI Semibold", 11F);

    public static Font CodeFont { get; } = new("Consolas", 9.75F);

    /// <summary>
    /// Mor zemin üzerinde üst üste binen üç katman: üstte dolu bir eşkenar dörtgen, altında iki "V".
    /// Uygulama ikonu (Resources/app.ico) aynı çizimle üretildi.
    /// </summary>
    public static Bitmap CreateLogo(int size)
    {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var scale = size / 256F;
        PointF Point(float x, float y) => new(x * scale, y * scale);

        using (var background = CreateRoundedRectangle(new RectangleF(8 * scale, 8 * scale, 240 * scale, 240 * scale), 56 * scale))
        using (var gradient = new LinearGradientBrush(new PointF(0, 0), new PointF(size, size), ColorTranslator.FromHtml("#7356FF"), ColorTranslator.FromHtml("#3A1D9E")))
        {
            graphics.FillPath(gradient, background);
        }

        using var lowerLayer = CreateLayerPen(Color.FromArgb(150, 255, 255, 255), 16 * scale);
        using var middleLayer = CreateLayerPen(Color.FromArgb(205, 255, 255, 255), 16 * scale);
        graphics.DrawLines(lowerLayer, [Point(56, 166), Point(128, 202), Point(200, 166)]);
        graphics.DrawLines(middleLayer, [Point(56, 130), Point(128, 166), Point(200, 130)]);

        using var topLayer = new SolidBrush(Color.White);
        graphics.FillPolygon(topLayer, [Point(128, 58), Point(200, 94), Point(128, 130), Point(56, 94)]);

        return bitmap;
    }

    private static Pen CreateLayerPen(Color color, float width) => new(color, width)
    {
        LineJoin = LineJoin.Round,
        StartCap = LineCap.Round,
        EndCap = LineCap.Round
    };

    private static GraphicsPath CreateRoundedRectangle(RectangleF bounds, float radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

/// <summary>İnce kenarlıklı beyaz kart.</summary>
internal sealed class CardPanel : Panel
{
    public CardPanel()
    {
        BackColor = Theme.Surface;
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(Theme.Border);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
}
