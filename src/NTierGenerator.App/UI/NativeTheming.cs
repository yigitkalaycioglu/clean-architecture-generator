using System.Runtime.InteropServices;

namespace NTierGenerator.App.UI;

/// <summary>
/// Windows'un yerel görünüm ayarları. Desteklenmeyen Windows sürümlerinde çağrılar sessizce etkisiz kalır.
/// </summary>
internal static class NativeTheming
{
    private const int DwmUseImmersiveDarkMode = 20;
    private const int DwmCaptionColor = 35;
    private const int DwmTextColor = 36;

    /// <summary>Başlık çubuğunu verilen renge boyar (Windows 11).</summary>
    public static void SetCaptionColor(IntPtr windowHandle, Color background, Color text)
    {
        var darkMode = 1;
        var backgroundRef = ToColorRef(background);
        var textRef = ToColorRef(text);
        DwmSetWindowAttribute(windowHandle, DwmUseImmersiveDarkMode, ref darkMode, sizeof(int));
        DwmSetWindowAttribute(windowHandle, DwmCaptionColor, ref backgroundRef, sizeof(int));
        DwmSetWindowAttribute(windowHandle, DwmTextColor, ref textRef, sizeof(int));
    }

    /// <summary>TreeView'a Gezgin'deki gibi modern ok işaretleri ve fareyle üzerine gelme vurgusu verir.</summary>
    public static void UseExplorerStyle(Control control) => SetWindowTheme(control.Handle, "Explorer", null);

    /// <summary>Koyu zeminli kontrollerde kaydırma çubuklarını koyu yapar.</summary>
    public static void UseDarkScrollBars(Control control) => SetWindowTheme(control.Handle, "DarkMode_Explorer", null);

    private static int ToColorRef(Color color) => color.R | (color.G << 8) | (color.B << 16);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr windowHandle, int attribute, ref int value, int size);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr windowHandle, string? subAppName, string? subIdList);
}
