using MiniDeck.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MiniDeck.Services
{
    public sealed class ImageAssetInfo
    {
        private readonly List<string> _references = new List<string>();
        private readonly HashSet<ImageStorageKind> _referenceKinds = new HashSet<ImageStorageKind>();

        public ImageStorageKind Kind { get; internal set; }
        public ImageStorageKind ReferenceKind { get; internal set; }
        public string StoredPath { get; internal set; } = "";
        public string FullPath { get; internal set; } = "";
        public bool Exists { get; internal set; }
        public bool IsManaged { get; internal set; }
        public bool IsBundled { get; internal set; }

        public string FileName => string.IsNullOrWhiteSpace(FullPath)
            ? Path.GetFileName(StoredPath)
            : Path.GetFileName(FullPath);

        public string KindLabel
        {
            get
            {
                if (Kind != ImageStorageKind.ShellIcon &&
                    _referenceKinds.Contains(ImageStorageKind.Button) &&
                    _referenceKinds.Contains(ImageStorageKind.Background))
                {
                    return "共有画像";
                }

                if (Kind != ImageStorageKind.ShellIcon &&
                    _referenceKinds.Contains(ImageStorageKind.Background))
                {
                    return "背景画像";
                }

                switch (Kind)
                {
                    case ImageStorageKind.Background:
                        return "背景画像";
                    case ImageStorageKind.ShellIcon:
                        return "シェルアイコン";
                    default:
                        return "ボタン画像";
                }
            }
        }

        public int ReferenceCount => _references.Count;
        public IReadOnlyList<string> References => _references;
        public bool IsMissing => !Exists && ReferenceCount > 0;
        public bool CanRepair => IsMissing;
        public bool CanDelete => Exists && IsManaged && ReferenceCount == 0;
        public string PreviewPath => Exists ? FullPath : null;

        public string Status
        {
            get
            {
                if (IsMissing)
                {
                    return "見つからない";
                }

                if (ReferenceCount > 0)
                {
                    if (IsBundled)
                    {
                        return "使用中（同梱）";
                    }

                    return IsManaged ? "使用中" : "使用中（外部）";
                }

                return IsManaged ? "未使用" : "参照なし";
            }
        }

        public string ReferenceSummary
        {
            get
            {
                if (_references.Count == 0)
                {
                    return "参照なし";
                }

                const int shownCount = 3;
                string summary = string.Join("、", _references.Take(shownCount));
                return _references.Count <= shownCount
                    ? summary
                    : $"{summary} ほか {_references.Count - shownCount} 件";
            }
        }

        internal void AddReference(string location, ImageStorageKind referenceKind)
        {
            _referenceKinds.Add(referenceKind);
            if (!string.IsNullOrWhiteSpace(location) && !_references.Contains(location))
            {
                _references.Add(location);
            }
        }
    }

    public static class ImageAssetService
    {
        public static IReadOnlyList<ImageAssetInfo> BuildCatalog(
            IEnumerable<PageSetting> pages,
            string backgroundImagePath,
            string storageRoot = null,
            string applicationBaseDirectory = null)
        {
            string root = ImageStorageService.GetStorageRoot(storageRoot);
            var catalogByPath = new Dictionary<string, ImageAssetInfo>(StringComparer.OrdinalIgnoreCase);

            AddManagedFiles(catalogByPath, ImageStorageKind.Button, storageRoot);
            AddManagedFiles(catalogByPath, ImageStorageKind.Background, storageRoot);
            AddManagedFiles(catalogByPath, ImageStorageKind.ShellIcon, storageRoot);

            foreach (ImageReference reference in CreateReferences(pages, backgroundImagePath, applicationBaseDirectory))
            {
                if (reference.Exists && !string.IsNullOrWhiteSpace(reference.FullPath))
                {
                    string key = NormalizePath(reference.FullPath);
                    if (!catalogByPath.TryGetValue(key, out ImageAssetInfo item))
                    {
                        item = new ImageAssetInfo
                        {
                            Kind = reference.Kind,
                            ReferenceKind = reference.Kind,
                            StoredPath = reference.StoredPath,
                            FullPath = reference.FullPath,
                            Exists = true,
                            IsManaged = IsPathWithinRoot(reference.FullPath, root),
                            IsBundled = reference.IsBundled
                        };
                        catalogByPath[key] = item;
                    }

                    item.AddReference(reference.Location, reference.Kind);
                    continue;
                }

                string missingKey = $"missing|{reference.Kind}|{reference.StoredPath}";
                if (!catalogByPath.TryGetValue(missingKey, out ImageAssetInfo missingItem))
                {
                    missingItem = new ImageAssetInfo
                    {
                        Kind = DetermineKindFromPath(reference.FullPath, reference.Kind, storageRoot),
                        ReferenceKind = reference.Kind,
                        StoredPath = reference.StoredPath,
                        FullPath = reference.FullPath ?? "",
                        Exists = false,
                        IsManaged = !string.IsNullOrWhiteSpace(reference.FullPath) &&
                                    IsPathWithinRoot(reference.FullPath, root),
                        IsBundled = reference.IsBundled
                    };
                    catalogByPath[missingKey] = missingItem;
                }

                missingItem.AddReference(reference.Location, reference.Kind);
            }

            return catalogByPath.Values
                .OrderByDescending(item => item.IsMissing)
                .ThenBy(item => item.CanDelete ? 1 : 0)
                .ThenBy(item => item.KindLabel)
                .ThenBy(item => item.FileName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        public static bool TryDeleteUnusedImage(
            string fullPath,
            IEnumerable<PageSetting> pages,
            string backgroundImagePath,
            out string errorMessage,
            string storageRoot = null,
            string applicationBaseDirectory = null)
        {
            errorMessage = "";
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                errorMessage = "削除する画像を選択してください。";
                return false;
            }

            string normalizedPath;
            try
            {
                normalizedPath = NormalizePath(fullPath);
            }
            catch (Exception ex)
            {
                errorMessage = $"画像パスを確認できません。\n{ex.Message}";
                return false;
            }

            string root = ImageStorageService.GetStorageRoot(storageRoot);
            if (!IsPathWithinRoot(normalizedPath, root))
            {
                errorMessage = "MiniDeckの画像保存フォルダー外にあるファイルは削除できません。";
                return false;
            }

            ImageAssetInfo item = BuildCatalog(
                    pages,
                    backgroundImagePath,
                    storageRoot,
                    applicationBaseDirectory)
                .FirstOrDefault(asset => PathsEqual(asset.FullPath, normalizedPath));
            if (item == null || !item.Exists)
            {
                errorMessage = "画像ファイルが見つかりません。";
                return false;
            }

            if (!item.IsManaged)
            {
                errorMessage = "MiniDeckが管理していない画像は削除できません。";
                return false;
            }

            if (item.ReferenceCount > 0)
            {
                errorMessage = $"この画像は {item.ReferenceCount} か所で使用中のため削除できません。";
                return false;
            }

            try
            {
                File.Delete(normalizedPath);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"画像を削除できませんでした。\n{ex.Message}";
                return false;
            }
        }

        public static int DeleteAllUnusedImages(
            IEnumerable<PageSetting> pages,
            string backgroundImagePath,
            out IReadOnlyList<string> errors,
            string storageRoot = null,
            string applicationBaseDirectory = null)
        {
            List<PageSetting> pageSnapshot = (pages ?? Enumerable.Empty<PageSetting>()).ToList();
            List<ImageAssetInfo> unused = BuildCatalog(
                    pageSnapshot,
                    backgroundImagePath,
                    storageRoot,
                    applicationBaseDirectory)
                .Where(item => item.CanDelete)
                .ToList();
            var deleteErrors = new List<string>();
            int deletedCount = 0;

            foreach (ImageAssetInfo item in unused)
            {
                if (TryDeleteUnusedImage(
                    item.FullPath,
                    pageSnapshot,
                    backgroundImagePath,
                    out string error,
                    storageRoot,
                    applicationBaseDirectory))
                {
                    deletedCount++;
                }
                else
                {
                    deleteErrors.Add($"{item.FileName}: {error}");
                }
            }

            errors = deleteErrors;
            return deletedCount;
        }

        private static IEnumerable<ImageReference> CreateReferences(
            IEnumerable<PageSetting> pages,
            string backgroundImagePath,
            string applicationBaseDirectory)
        {
            if (!string.IsNullOrWhiteSpace(backgroundImagePath))
            {
                yield return CreateReference(
                    ImageStorageKind.Background,
                    backgroundImagePath,
                    "背景",
                    applicationBaseDirectory);
            }

            int pageIndex = 0;
            foreach (PageSetting page in pages ?? Enumerable.Empty<PageSetting>())
            {
                int buttonIndex = 0;
                foreach (ButtonSetting button in page?.Buttons ?? new List<ButtonSetting>())
                {
                    if (!string.IsNullOrWhiteSpace(button?.ImagePath))
                    {
                        string pageName = string.IsNullOrWhiteSpace(page?.Name)
                            ? $"ページ {pageIndex + 1}"
                            : page.Name;
                        string buttonName = string.IsNullOrWhiteSpace(button.DisplayText)
                            ? $"ボタン {buttonIndex + 1}"
                            : button.DisplayText;
                        yield return CreateReference(
                            ImageStorageKind.Button,
                            button.ImagePath,
                            $"「{pageName}」の {buttonIndex + 1} 番「{buttonName}」",
                            applicationBaseDirectory);
                    }

                    if (!string.IsNullOrWhiteSpace(button?.StateActiveImagePath))
                    {
                        string pageName = string.IsNullOrWhiteSpace(page?.Name)
                            ? $"ページ {pageIndex + 1}"
                            : page.Name;
                        string buttonName = string.IsNullOrWhiteSpace(button.DisplayText)
                            ? $"ボタン {buttonIndex + 1}"
                            : button.DisplayText;
                        yield return CreateReference(
                            ImageStorageKind.Button,
                            button.StateActiveImagePath,
                            $"「{pageName}」の {buttonIndex + 1} 番「{buttonName}」（状態ON画像）",
                            applicationBaseDirectory);
                    }

                    buttonIndex++;
                }

                pageIndex++;
            }
        }

        private static ImageReference CreateReference(
            ImageStorageKind kind,
            string storedPath,
            string location,
            string applicationBaseDirectory)
        {
            bool exists = ImageStorageService.TryResolveImagePath(
                storedPath,
                out string fullPath,
                applicationBaseDirectory);
            string normalized = storedPath.Replace('\\', '/');
            return new ImageReference
            {
                Kind = kind,
                StoredPath = storedPath,
                FullPath = fullPath,
                Exists = exists,
                Location = location,
                IsBundled = normalized.StartsWith("/Resources/", StringComparison.OrdinalIgnoreCase)
            };
        }

        private static void AddManagedFiles(
            IDictionary<string, ImageAssetInfo> catalog,
            ImageStorageKind kind,
            string storageRoot)
        {
            string directory = ImageStorageService.GetCategoryDirectory(kind, storageRoot);
            if (!Directory.Exists(directory))
            {
                return;
            }

            foreach (string file in Directory.EnumerateFiles(directory))
            {
                string fullPath;
                try
                {
                    fullPath = NormalizePath(file);
                }
                catch
                {
                    continue;
                }

                catalog[fullPath] = new ImageAssetInfo
                {
                    Kind = kind,
                    ReferenceKind = kind == ImageStorageKind.Background
                        ? ImageStorageKind.Background
                        : ImageStorageKind.Button,
                    StoredPath = fullPath,
                    FullPath = fullPath,
                    Exists = File.Exists(fullPath),
                    IsManaged = true
                };
            }
        }

        private static ImageStorageKind DetermineKindFromPath(
            string fullPath,
            ImageStorageKind fallback,
            string storageRoot)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return fallback;
            }

            foreach (ImageStorageKind kind in new[]
            {
                ImageStorageKind.Background,
                ImageStorageKind.ShellIcon,
                ImageStorageKind.Button
            })
            {
                if (IsPathWithinRoot(fullPath, ImageStorageService.GetCategoryDirectory(kind, storageRoot)))
                {
                    return kind;
                }
            }

            return fallback;
        }

        private static bool IsPathWithinRoot(string path, string root)
        {
            try
            {
                string normalizedPath = NormalizePath(path);
                string normalizedRoot = NormalizePath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static bool PathsEqual(string first, string second)
        {
            if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second))
            {
                return false;
            }

            try
            {
                return string.Equals(NormalizePath(first), NormalizePath(second), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private sealed class ImageReference
        {
            public ImageStorageKind Kind { get; set; }
            public string StoredPath { get; set; }
            public string FullPath { get; set; }
            public string Location { get; set; }
            public bool Exists { get; set; }
            public bool IsBundled { get; set; }
        }
    }
}
