using System.Runtime.InteropServices;

namespace NTierGenerator.App.UI;

/// <summary>
/// Dosya türü ikonlarını Windows Gezgini'ndeki gibi kabuktan (shell32) alır.
/// Dosyanın diskte var olması gerekmez; uzantı yeterlidir.
/// </summary>
internal static class ShellIcons
{
    private const uint FileAttributeDirectory = 0x10;
    private const uint FileAttributeNormal = 0x80;
    private const uint ShgfiIcon = 0x100;
    private const uint ShgfiSmallIcon = 0x1;
    private const uint ShgfiOpenIcon = 0x2;
    private const uint ShgfiUseFileAttributes = 0x10;

    public static Bitmap? GetFolderIcon(bool open) =>
        GetIcon("folder", FileAttributeDirectory, open ? ShgfiOpenIcon : 0);

    public static Bitmap? GetFileIcon(string fileName) =>
        GetIcon(fileName, FileAttributeNormal, 0);

    private static Bitmap? GetIcon(string path, uint attributes, uint extraFlags)
    {
        var info = new ShFileInfo();
        var flags = ShgfiIcon | ShgfiSmallIcon | ShgfiUseFileAttributes | extraFlags;
        if (SHGetFileInfo(path, attributes, ref info, (uint)Marshal.SizeOf<ShFileInfo>(), flags) == IntPtr.Zero || info.Icon == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            using var icon = Icon.FromHandle(info.Icon);
            return icon.ToBitmap();
        }
        finally
        {
            DestroyIcon(info.Icon);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShFileInfo
    {
        public IntPtr Icon;
        public int IconIndex;
        public uint Attributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string DisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string TypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(string path, uint fileAttributes, ref ShFileInfo fileInfo, uint fileInfoSize, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr icon);
}
