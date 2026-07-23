using MiniDeck.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace MiniDeck.Services
{
    public sealed class ButtonDropResult
    {
        private ButtonDropResult(ActionButton button, string errorMessage)
        {
            Button = button;
            ErrorMessage = errorMessage ?? "";
        }

        public bool Success => Button != null && string.IsNullOrWhiteSpace(ErrorMessage);
        public ActionButton Button { get; }
        public string ErrorMessage { get; }

        public static ButtonDropResult Succeeded(ActionButton button)
        {
            return new ButtonDropResult(button, "");
        }

        public static ButtonDropResult Failed(string errorMessage)
        {
            return new ButtonDropResult(null, errorMessage);
        }
    }

    public sealed class ButtonDropService
    {
        private const string DefaultIconPath = "/Resources/Icons/default_icon.jpg";
        private readonly string _iconStorageRoot;

        public ButtonDropService(string iconStorageRoot = null)
        {
            _iconStorageRoot = iconStorageRoot;
        }

        public static bool CanAcceptData(IDataObject data)
        {
            if (data == null)
            {
                return false;
            }

            try
            {
                return data.GetDataPresent(DataFormats.FileDrop, true) ||
                       data.GetDataPresent(DataFormats.UnicodeText, true) ||
                       data.GetDataPresent(DataFormats.Text, true) ||
                       data.GetDataPresent(DataFormats.StringFormat, true) ||
                       data.GetDataPresent("UniformResourceLocatorW", true) ||
                       data.GetDataPresent("UniformResourceLocator", true);
            }
            catch
            {
                return false;
            }
        }

        public ButtonDropResult CreateButton(IDataObject data)
        {
            if (data == null)
            {
                return ButtonDropResult.Failed("ドロップされたデータを読み取れませんでした。");
            }

            try
            {
                if (data.GetDataPresent(DataFormats.FileDrop, true))
                {
                    var paths = data.GetData(DataFormats.FileDrop, true) as string[];
                    return CreateFromFilePaths(paths);
                }

                string text = GetDroppedText(data);
                return string.IsNullOrWhiteSpace(text)
                    ? ButtonDropResult.Failed("ファイル、フォルダー、またはURLをドロップしてください。")
                    : CreateFromText(text);
            }
            catch (Exception ex)
            {
                return ButtonDropResult.Failed($"ドロップされたデータを処理できませんでした。\n{ex.Message}");
            }
        }

        public ButtonDropResult CreateFromFilePaths(IEnumerable<string> paths)
        {
            string[] values = (paths ?? Enumerable.Empty<string>())
                .Where(itemPath => !string.IsNullOrWhiteSpace(itemPath))
                .ToArray();
            if (values.Length == 0)
            {
                return ButtonDropResult.Failed("ドロップされたファイルまたはフォルダーを読み取れませんでした。");
            }

            if (values.Length != 1)
            {
                return ButtonDropResult.Failed("複数の項目を同時に登録できません。ボタン1つにつき1項目をドロップしてください。");
            }

            string path;
            try
            {
                path = Path.GetFullPath(values[0]);
            }
            catch (Exception ex)
            {
                return ButtonDropResult.Failed($"ファイルパスが正しくありません。\n{ex.Message}");
            }

            if (Directory.Exists(path))
            {
                string name = new DirectoryInfo(path).Name;
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = path;
                }

                return CreateFileSystemButton(path, name);
            }

            if (!File.Exists(path))
            {
                return ButtonDropResult.Failed($"ドロップされた項目が見つかりません。\n{path}");
            }

            string displayName = Path.GetFileNameWithoutExtension(path);
            if (string.Equals(Path.GetExtension(path), ".url", StringComparison.OrdinalIgnoreCase))
            {
                return CreateFromInternetShortcut(path, displayName);
            }

            return CreateFileSystemButton(path, displayName);
        }

        public ButtonDropResult CreateFromText(string text)
        {
            string value = text?.Trim().Trim('\0');
            if (!ActionService.TryCreateWebUri(value, out Uri uri, out string errorMessage))
            {
                return ButtonDropResult.Failed($"URLを登録できません。{errorMessage}");
            }

            string displayName = uri.Host;
            if (displayName.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            {
                displayName = displayName.Substring(4);
            }

            string iconPath = ShellIconService.TryExtractAndStoreIcon(
                ".url",
                displayName,
                uri.AbsoluteUri,
                true,
                _iconStorageRoot);
            return ButtonDropResult.Succeeded(new ActionButton
            {
                DisplayText = displayName,
                ImagePath = string.IsNullOrWhiteSpace(iconPath) ? DefaultIconPath : iconPath,
                ActionType = ActionType.OpenUrl,
                Url = uri.AbsoluteUri
            });
        }

        private ButtonDropResult CreateFromInternetShortcut(string path, string displayName)
        {
            try
            {
                string urlValue = File.ReadLines(path)
                    .Select(line => line?.Trim())
                    .FirstOrDefault(line => line != null && line.StartsWith("URL=", StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrWhiteSpace(urlValue))
                {
                    return ButtonDropResult.Failed("インターネットショートカットにURLが見つかりません。");
                }

                string value = urlValue.Substring(urlValue.IndexOf('=') + 1).Trim().Trim('"');
                if (!ActionService.TryCreateWebUri(value, out Uri uri, out string errorMessage))
                {
                    return ButtonDropResult.Failed($"インターネットショートカットのURLが正しくありません。{errorMessage}");
                }

                string iconPath = GetIconPath(path, displayName);
                return ButtonDropResult.Succeeded(new ActionButton
                {
                    DisplayText = displayName,
                    ImagePath = iconPath,
                    ActionType = ActionType.OpenUrl,
                    Url = uri.AbsoluteUri
                });
            }
            catch (Exception ex)
            {
                return ButtonDropResult.Failed($"インターネットショートカットを読み取れませんでした。\n{ex.Message}");
            }
        }

        private ButtonDropResult CreateFileSystemButton(string path, string displayName)
        {
            return ButtonDropResult.Succeeded(new ActionButton
            {
                DisplayText = displayName,
                ImagePath = GetIconPath(path, displayName),
                ActionType = ActionType.LaunchApplication,
                ApplicationPath = path,
                ApplicationArguments = ""
            });
        }

        private string GetIconPath(string path, string displayName)
        {
            string iconPath = ShellIconService.TryExtractAndStoreIcon(
                path,
                displayName,
                path,
                false,
                _iconStorageRoot);
            return string.IsNullOrWhiteSpace(iconPath) ? DefaultIconPath : iconPath;
        }

        private static string GetDroppedText(IDataObject data)
        {
            foreach (string format in new[]
            {
                DataFormats.UnicodeText,
                DataFormats.Text,
                DataFormats.StringFormat,
                "UniformResourceLocatorW",
                "UniformResourceLocator"
            })
            {
                if (!data.GetDataPresent(format, true))
                {
                    continue;
                }

                object value = data.GetData(format, true);
                if (value is string stringValue)
                {
                    return stringValue;
                }

                if (value is MemoryStream stream)
                {
                    byte[] bytes = stream.ToArray();
                    Encoding encoding = string.Equals(format, "UniformResourceLocatorW", StringComparison.Ordinal)
                        ? Encoding.Unicode
                        : Encoding.Default;
                    return encoding.GetString(bytes).TrimEnd('\0');
                }
            }

            return "";
        }
    }
}
