using Microsoft.Win32;
using MiniDeck.Models;
using MiniDeck.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace MiniDeck
{
    public sealed class ImagePathReplacement
    {
        public ImageStorageKind Kind { get; set; }
        public string OldPath { get; set; }
        public string NewPath { get; set; }
    }

    public partial class ImageManagementWindow : Window
    {
        private readonly List<PageSetting> _pages;
        private readonly List<PageSetting> _protectedPages;
        private readonly string _storageRoot;
        private readonly string _applicationBaseDirectory;
        private readonly List<ImagePathReplacement> _replacements = new List<ImagePathReplacement>();
        private string _backgroundImagePath;
        private readonly string _protectedBackgroundImagePath;

        public ObservableCollection<ImageAssetInfo> Images { get; } = new ObservableCollection<ImageAssetInfo>();
        public IReadOnlyList<ImagePathReplacement> Replacements => _replacements;

        public ImageManagementWindow(
            IEnumerable<PageSetting> pages,
            string backgroundImagePath,
            string storageRoot = null,
            string applicationBaseDirectory = null,
            IEnumerable<PageSetting> protectedPages = null,
            string protectedBackgroundImagePath = null)
        {
            InitializeComponent();
            _pages = (pages ?? Enumerable.Empty<PageSetting>())
                .Where(page => page != null)
                .Select(page => page.Clone())
                .ToList();
            _protectedPages = (protectedPages ?? Enumerable.Empty<PageSetting>())
                .Where(page => page != null)
                .Select(page => page.Clone())
                .ToList();
            _backgroundImagePath = backgroundImagePath ?? "";
            _protectedBackgroundImagePath = protectedBackgroundImagePath ?? "";
            _storageRoot = storageRoot;
            _applicationBaseDirectory = applicationBaseDirectory;
            ImageGrid.ItemsSource = Images;
            RefreshCatalog();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshCatalog();
        }

        private void Repair_Click(object sender, RoutedEventArgs e)
        {
            RepairSelectedImage();
        }

        private void ImageGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((ImageGrid.SelectedItem as ImageAssetInfo)?.CanRepair == true)
            {
                RepairSelectedImage();
            }
        }

        private void RepairSelectedImage()
        {
            ImageAssetInfo item = ImageGrid.SelectedItem as ImageAssetInfo;
            if (item == null || !item.CanRepair)
            {
                MessageBox.Show(
                    "状態が「見つからない」の画像を選択してください。",
                    "画像を選択し直す",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var dialog = new OpenFileDialog
            {
                Filter = "画像ファイル (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp|すべてのファイル (*.*)|*.*",
                Title = $"{item.ReferenceSummary} の画像を選択し直す"
            };
            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                ImageStorageKind referenceKind = item.ReferenceKind == ImageStorageKind.Background
                    ? ImageStorageKind.Background
                    : ImageStorageKind.Button;
                string replacementPath = ImageStorageService.ImportImage(
                    dialog.FileName,
                    referenceKind,
                    _storageRoot);
                ReplaceReferences(referenceKind, item.StoredPath, replacementPath);
                RefreshCatalog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"画像を追加できませんでした。\n{ex.Message}",
                    "画像の再選択に失敗しました",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ReplaceReferences(ImageStorageKind kind, string oldPath, string newPath)
        {
            if (kind == ImageStorageKind.Background)
            {
                if (PathsMatch(_backgroundImagePath, oldPath))
                {
                    _backgroundImagePath = newPath;
                }
            }
            else
            {
                foreach (ButtonSetting button in _pages
                    .SelectMany(page => page.Buttons ?? new List<ButtonSetting>())
                    .Where(button => button != null))
                {
                    if (PathsMatch(button.ImagePath, oldPath))
                    {
                        button.ImagePath = newPath;
                    }

                    if (PathsMatch(button.StateActiveImagePath, oldPath))
                    {
                        button.StateActiveImagePath = newPath;
                    }
                }
            }

            ImagePathReplacement existing = _replacements.FirstOrDefault(replacement =>
                replacement.Kind == kind && PathsMatch(replacement.OldPath, oldPath));
            if (existing == null)
            {
                _replacements.Add(new ImagePathReplacement
                {
                    Kind = kind,
                    OldPath = oldPath,
                    NewPath = newPath
                });
            }
            else
            {
                existing.NewPath = newPath;
            }
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            ImageAssetInfo item = ImageGrid.SelectedItem as ImageAssetInfo;
            if (item == null || !item.CanDelete)
            {
                MessageBox.Show(
                    "状態が「未使用」のMiniDeck管理画像を選択してください。使用中の画像は削除できません。",
                    "削除できません",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            MessageBoxResult answer = MessageBox.Show(
                $"未使用の画像「{item.FileName}」を削除しますか？\n\nこの削除は設定画面をキャンセルしても元に戻りません。",
                "未使用画像の削除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            if (!ImageAssetService.TryDeleteUnusedImage(
                item.FullPath,
                GetPagesForDeletionCheck(),
                _backgroundImagePath,
                out string error,
                _storageRoot,
                _applicationBaseDirectory))
            {
                MessageBox.Show(error, "削除できません", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            RefreshCatalog();
        }

        private void DeleteAllUnused_Click(object sender, RoutedEventArgs e)
        {
            int unusedCount = Images.Count(item => item.CanDelete);
            if (unusedCount == 0)
            {
                MessageBox.Show(
                    "削除できる未使用画像はありません。",
                    "未使用画像の削除",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            MessageBoxResult answer = MessageBox.Show(
                $"未使用画像 {unusedCount} 件をすべて削除しますか？\n\n使用中の画像は削除されません。この削除は設定画面をキャンセルしても元に戻りません。",
                "未使用画像をすべて削除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            int deletedCount = ImageAssetService.DeleteAllUnusedImages(
                GetPagesForDeletionCheck(),
                _backgroundImagePath,
                out IReadOnlyList<string> errors,
                _storageRoot,
                _applicationBaseDirectory);
            RefreshCatalog();
            string message = $"未使用画像を {deletedCount} 件削除しました。";
            if (errors.Count > 0)
            {
                message += $"\n\n削除できなかった画像:\n{string.Join("\n", errors)}";
            }

            MessageBox.Show(
                message,
                "未使用画像の削除",
                MessageBoxButton.OK,
                errors.Count == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }

        private void RefreshCatalog()
        {
            Images.Clear();
            foreach (ImageAssetInfo item in ImageAssetService.BuildCatalog(
                GetPagesForDisplayCatalog(),
                _backgroundImagePath,
                _storageRoot,
                _applicationBaseDirectory))
            {
                Images.Add(item);
            }

            int usedCount = Images.Count(item => item.ReferenceCount > 0 && item.Exists);
            int unusedCount = Images.Count(item => item.CanDelete);
            int missingCount = Images.Count(item => item.IsMissing);
            SummaryText.Text = $"使用中 {usedCount} 件 / 未使用 {unusedCount} 件 / 見つからない画像 {missingCount} 件";
        }

        private IEnumerable<PageSetting> GetPagesForDisplayCatalog()
        {
            foreach (PageSetting page in _pages)
            {
                yield return page;
            }

            foreach (PageSetting page in _protectedPages)
            {
                List<ButtonSetting> existingReferences = (page.Buttons ?? new List<ButtonSetting>())
                    .Select(button => button?.Clone() ?? new ButtonSetting())
                    .ToList();
                bool hasExistingReference = false;
                foreach (ButtonSetting button in existingReferences)
                {
                    bool baseImageExists = ImageStorageService.TryResolveImagePath(
                        button.ImagePath,
                        out string _,
                        _applicationBaseDirectory);
                    bool stateImageExists = ImageStorageService.TryResolveImagePath(
                        button.StateActiveImagePath,
                        out string _,
                        _applicationBaseDirectory);
                    if (baseImageExists || stateImageExists)
                    {
                        hasExistingReference = true;
                    }

                    if (!baseImageExists)
                    {
                        button.ImagePath = "";
                    }

                    if (!stateImageExists)
                    {
                        button.StateActiveImagePath = "";
                    }
                }

                if (hasExistingReference)
                {
                    yield return new PageSetting
                    {
                        Name = page.Name,
                        Buttons = existingReferences
                    };
                }
            }

            if (!string.IsNullOrWhiteSpace(_protectedBackgroundImagePath) &&
                !PathsMatch(_protectedBackgroundImagePath, _backgroundImagePath) &&
                ImageStorageService.TryResolveImagePath(
                    _protectedBackgroundImagePath,
                    out string _,
                    _applicationBaseDirectory))
            {
                yield return CreateProtectedBackgroundReferencePage();
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private IEnumerable<PageSetting> GetPagesForDeletionCheck()
        {
            foreach (PageSetting page in _pages.Concat(_protectedPages))
            {
                yield return page;
            }

            if (!string.IsNullOrWhiteSpace(_protectedBackgroundImagePath) &&
                !PathsMatch(_protectedBackgroundImagePath, _backgroundImagePath))
            {
                yield return CreateProtectedBackgroundReferencePage();
            }
        }

        private PageSetting CreateProtectedBackgroundReferencePage()
        {
            return new PageSetting
            {
                Name = "適用済み設定",
                Buttons = new List<ButtonSetting>
                {
                    new ButtonSetting
                    {
                        DisplayText = "適用済みの背景画像",
                        ImagePath = _protectedBackgroundImagePath
                    }
                }
            };
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private static bool PathsMatch(string first, string second)
        {
            return string.Equals(
                first?.Trim() ?? "",
                second?.Trim() ?? "",
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
