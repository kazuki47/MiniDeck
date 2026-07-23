using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace MiniDeck.Services
{
    public static class ShellIconService
    {
        private const uint FileAttributeDirectory = 0x00000010;
        private const uint FileAttributeNormal = 0x00000080;
        private const uint ShgfiIcon = 0x00000100;
        private const uint ShgfiLargeIcon = 0x00000000;
        private const uint ShgfiUseFileAttributes = 0x00000010;

        public static string TryExtractAndStoreIcon(
            string shellPath,
            string displayName,
            string sourceKey = null,
            bool useFileAttributes = false,
            string storageRoot = null)
        {
            if (string.IsNullOrWhiteSpace(shellPath))
            {
                return "";
            }

            string cacheKey = CreateCacheKey(shellPath, sourceKey, useFileAttributes);
            if (ImageStorageService.TryGetGeneratedIconPath(cacheKey, out string cachedPath, storageRoot))
            {
                return cachedPath;
            }

            IntPtr iconHandle = IntPtr.Zero;
            try
            {
                uint attributes = Directory.Exists(shellPath)
                    ? FileAttributeDirectory
                    : FileAttributeNormal;
                uint flags = ShgfiIcon | ShgfiLargeIcon;
                if (useFileAttributes)
                {
                    flags |= ShgfiUseFileAttributes;
                }

                var fileInfo = new ShellFileInfo();
                IntPtr result = SHGetFileInfo(
                    shellPath,
                    attributes,
                    ref fileInfo,
                    (uint)Marshal.SizeOf(fileInfo),
                    flags);
                iconHandle = fileInfo.IconHandle;
                if (result == IntPtr.Zero || iconHandle == IntPtr.Zero)
                {
                    return "";
                }

                BitmapSource bitmap = Imaging.CreateBitmapSourceFromHIcon(
                    iconHandle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(64, 64));
                bitmap.Freeze();
                return ImageStorageService.SaveGeneratedIcon(
                    bitmap,
                    string.IsNullOrWhiteSpace(displayName) ? "icon" : displayName,
                    cacheKey,
                    storageRoot);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"シェルアイコンの取得に失敗しました: {shellPath}\n{ex.Message}");
                return "";
            }
            finally
            {
                if (iconHandle != IntPtr.Zero)
                {
                    DestroyIcon(iconHandle);
                }
            }
        }

        private static string CreateCacheKey(string shellPath, string sourceKey, bool useFileAttributes)
        {
            string normalizedPath = shellPath.Trim().ToUpperInvariant();
            string fileVersion = "";
            try
            {
                if (!useFileAttributes && File.Exists(shellPath))
                {
                    var info = new FileInfo(shellPath);
                    fileVersion = $"|{info.Length}|{info.LastWriteTimeUtc.Ticks}";
                }
                else if (!useFileAttributes && Directory.Exists(shellPath))
                {
                    fileVersion = $"|{Directory.GetLastWriteTimeUtc(shellPath).Ticks}";
                }
            }
            catch
            {
                // 更新時刻を取得できない場合もパス単位のキャッシュは利用できる。
            }

            return $"{sourceKey ?? normalizedPath}|{normalizedPath}|{useFileAttributes}{fileVersion}";
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct ShellFileInfo
        {
            public IntPtr IconHandle;
            public int IconIndex;
            public uint Attributes;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string DisplayName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string TypeName;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(
            string path,
            uint fileAttributes,
            ref ShellFileInfo fileInfo,
            uint fileInfoSize,
            uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr iconHandle);
    }
}
