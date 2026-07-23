using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;

namespace MiniDeck.Services
{
    public enum ImageStorageKind
    {
        Button,
        Background,
        ShellIcon
    }

    public static class ImageStorageService
    {
        public static string ImportImage(
            string sourceFilePath,
            ImageStorageKind kind,
            string storageRoot = null)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath))
            {
                throw new ArgumentException("画像ファイルを指定してください。", nameof(sourceFilePath));
            }

            string sourcePath = Path.GetFullPath(sourceFilePath);
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("指定された画像ファイルが見つかりません。", sourcePath);
            }

            string destinationDirectory = GetCategoryDirectory(kind, storageRoot);
            Directory.CreateDirectory(destinationDirectory);

            string extension = Path.GetExtension(sourcePath);
            string hash = ComputeFileHash(sourcePath);

            // 保存カテゴリと旧版の名前形式を問わず、内容が同じ画像は1ファイルを共有する。
            string existingPath = new[]
                {
                    GetCategoryDirectory(ImageStorageKind.Button, storageRoot),
                    GetCategoryDirectory(ImageStorageKind.Background, storageRoot),
                    GetCategoryDirectory(ImageStorageKind.ShellIcon, storageRoot)
                }
                .Where(Directory.Exists)
                .SelectMany(directory => Directory.EnumerateFiles(directory))
                .FirstOrDefault(file => FilesHaveSameHash(file, hash));
            if (!string.IsNullOrWhiteSpace(existingPath))
            {
                return existingPath;
            }

            string destinationPath = Path.Combine(destinationDirectory, $"{hash}{extension.ToLowerInvariant()}");

            if (!string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(sourcePath, destinationPath, false);
            }

            return destinationPath;
        }

        public static bool TryResolveImagePath(
            string storedPath,
            out string resolvedPath,
            string applicationBaseDirectory = null)
        {
            resolvedPath = null;
            if (string.IsNullOrWhiteSpace(storedPath))
            {
                return false;
            }

            try
            {
                string value = storedPath.Trim();
                string baseDirectory = string.IsNullOrWhiteSpace(applicationBaseDirectory)
                    ? AppDomain.CurrentDomain.BaseDirectory
                    : Path.GetFullPath(applicationBaseDirectory);

                if (IsApplicationRelativePath(value))
                {
                    string relativePath = value.TrimStart('/', '\\')
                        .Replace('/', Path.DirectorySeparatorChar)
                        .Replace('\\', Path.DirectorySeparatorChar);
                    resolvedPath = Path.GetFullPath(Path.Combine(baseDirectory, relativePath));
                }
                else if (Uri.TryCreate(value, UriKind.Absolute, out Uri uri) && uri.IsFile)
                {
                    resolvedPath = Path.GetFullPath(uri.LocalPath);
                }
                else if (Path.IsPathRooted(value))
                {
                    resolvedPath = Path.GetFullPath(value);
                }
                else
                {
                    resolvedPath = Path.GetFullPath(Path.Combine(baseDirectory, value));
                }

                return File.Exists(resolvedPath);
            }
            catch
            {
                resolvedPath = null;
                return false;
            }
        }

        public static string SaveGeneratedIcon(
            BitmapSource bitmap,
            string displayName,
            string sourceKey,
            string storageRoot = null)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }

            string destinationDirectory = GetCategoryDirectory(ImageStorageKind.ShellIcon, storageRoot);
            Directory.CreateDirectory(destinationDirectory);

            string hash = ComputeTextHash(sourceKey ?? displayName ?? "icon");
            if (TryGetGeneratedIconPath(sourceKey ?? displayName ?? "icon", out string cachedPath, storageRoot))
            {
                return cachedPath;
            }

            string destinationPath = Path.Combine(destinationDirectory, $"{hash}.png");

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var stream = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                encoder.Save(stream);
            }

            return destinationPath;
        }

        public static bool TryGetGeneratedIconPath(
            string sourceKey,
            out string iconPath,
            string storageRoot = null)
        {
            iconPath = null;
            if (string.IsNullOrWhiteSpace(sourceKey))
            {
                return false;
            }

            string directory = GetCategoryDirectory(ImageStorageKind.ShellIcon, storageRoot);
            if (!Directory.Exists(directory))
            {
                return false;
            }

            string hash = ComputeTextHash(sourceKey);
            string currentPath = Path.Combine(directory, $"{hash}.png");
            if (File.Exists(currentPath))
            {
                iconPath = currentPath;
                return true;
            }

            // 旧版の「表示名-先頭12文字のハッシュ.png」もキャッシュとして利用する。
            string legacySuffix = $"-{hash.Substring(0, 12)}.png";
            iconPath = Directory.EnumerateFiles(directory, "*.png")
                .FirstOrDefault(path => path.EndsWith(legacySuffix, StringComparison.OrdinalIgnoreCase));
            return !string.IsNullOrWhiteSpace(iconPath);
        }

        public static string GetStorageRoot(string storageRoot = null)
        {
            return string.IsNullOrWhiteSpace(storageRoot)
                ? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MiniDeck",
                    "Images")
                : Path.GetFullPath(storageRoot);
        }

        public static string GetCategoryDirectory(ImageStorageKind kind, string storageRoot = null)
        {
            string category;
            switch (kind)
            {
                case ImageStorageKind.Background:
                    category = "Backgrounds";
                    break;
                case ImageStorageKind.ShellIcon:
                    category = "GeneratedIcons";
                    break;
                default:
                    category = "Buttons";
                    break;
            }

            return Path.Combine(GetStorageRoot(storageRoot), category);
        }

        private static bool IsApplicationRelativePath(string path)
        {
            string normalized = path.Replace('\\', '/');
            return normalized.StartsWith("/Resources/", StringComparison.OrdinalIgnoreCase);
        }

        private static string ComputeFileHash(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            using (SHA256 algorithm = SHA256.Create())
            {
                return string.Concat(algorithm.ComputeHash(stream).Select(value => value.ToString("x2")));
            }
        }

        private static bool FilesHaveSameHash(string filePath, string expectedHash)
        {
            try
            {
                return string.Equals(ComputeFileHash(filePath), expectedHash, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string ComputeTextHash(string value)
        {
            using (SHA256 algorithm = SHA256.Create())
            {
                byte[] hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""));
                return string.Concat(hash.Select(item => item.ToString("x2")));
            }
        }

        private static string SanitizeFileName(string value)
        {
            char[] invalidCharacters = Path.GetInvalidFileNameChars();
            string sanitized = new string((value ?? "image")
                .Select(character => invalidCharacters.Contains(character) ? '_' : character)
                .ToArray())
                .Trim();
            return string.IsNullOrWhiteSpace(sanitized) ? "image" : sanitized;
        }
    }
}
