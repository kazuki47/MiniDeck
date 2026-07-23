using MiniDeck.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace MiniDeck.Services
{
    /// <summary>
    /// アプリケーション設定の保存、バックアップ、バージョン移行を行う。
    /// </summary>
    public class SettingsService
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MiniDeck", "settings.xml");

        public static bool SaveSettings(AppSettings settings)
        {
            return SaveSettings(settings, SettingsFilePath);
        }

        public static bool SaveSettings(AppSettings settings, string settingsFilePath)
        {
            string temporaryFilePath = null;

            try
            {
                if (settings == null)
                {
                    throw new ArgumentNullException(nameof(settings));
                }

                if (string.IsNullOrWhiteSpace(settingsFilePath))
                {
                    throw new ArgumentException("設定ファイルのパスが指定されていません。", nameof(settingsFilePath));
                }

                if (settings.IsReadOnly || settings.SettingsVersion > AppSettings.CurrentSettingsVersion)
                {
                    Console.WriteLine("新しい形式または復旧用の設定は安全のため上書きしません。");
                    return false;
                }

                NormalizeSettings(settings);

                string fullSettingsPath = Path.GetFullPath(settingsFilePath);
                string directory = Path.GetDirectoryName(fullSettingsPath);
                if (string.IsNullOrWhiteSpace(directory))
                {
                    throw new InvalidOperationException("設定ディレクトリを取得できませんでした。");
                }

                Directory.CreateDirectory(directory);
                temporaryFilePath = Path.Combine(
                    directory,
                    Path.GetFileName(fullSettingsPath) + "." + Guid.NewGuid().ToString("N") + ".tmp");

                SerializeSettings(settings, temporaryFilePath);

                // 一時ファイルを再読込し、不完全なXMLで本体を置換しないようにする。
                AppSettings verificationSettings = DeserializeSettings(temporaryFilePath);
                if (verificationSettings == null ||
                    verificationSettings.SettingsVersion != AppSettings.CurrentSettingsVersion ||
                    verificationSettings.Pages == null || verificationSettings.Pages.Count == 0)
                {
                    throw new InvalidDataException("保存前の設定ファイル検証に失敗しました。");
                }

                if (File.Exists(fullSettingsPath))
                {
                    File.Copy(fullSettingsPath, GetBackupFilePath(fullSettingsPath), true);
                    File.Replace(temporaryFilePath, fullSettingsPath, null, true);
                }
                else
                {
                    File.Move(temporaryFilePath, fullSettingsPath);
                }

                temporaryFilePath = null;
                Console.WriteLine($"設定を保存しました: {fullSettingsPath}");
                Console.WriteLine($"保存したページ数: {settings.Pages.Count}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"設定の保存中にエラーが発生しました: {ex.Message}");
                Console.WriteLine($"スタックトレース: {ex.StackTrace}");
                return false;
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(temporaryFilePath) && File.Exists(temporaryFilePath))
                {
                    try
                    {
                        File.Delete(temporaryFilePath);
                    }
                    catch (Exception cleanupException)
                    {
                        Console.WriteLine($"一時設定ファイルを削除できませんでした: {cleanupException.Message}");
                    }
                }
            }
        }

        public static AppSettings LoadSettings()
        {
            return LoadSettings(SettingsFilePath);
        }

        public static AppSettings LoadSettings(string settingsFilePath)
        {
            string fullSettingsPath = Path.GetFullPath(settingsFilePath);
            Console.WriteLine($"設定ファイルを読み込みます: {fullSettingsPath}");

            if (!File.Exists(fullSettingsPath))
            {
                var defaultSettings = new AppSettings();
                NormalizeSettings(defaultSettings);
                return defaultSettings;
            }

            try
            {
                AppSettings settings = DeserializeSettings(fullSettingsPath);
                if (settings.SettingsVersion > AppSettings.CurrentSettingsVersion)
                {
                    settings.IsReadOnly = true;
                    settings.LoadWarning =
                        $"この設定は新しいバージョン（{settings.SettingsVersion}）で作成されているため上書きしません。";
                    Console.WriteLine(settings.LoadWarning);
                    return settings;
                }

                bool migrationRequired = NormalizeSettings(settings);
                if (migrationRequired)
                {
                    string migrationBackupPath = GetMigrationBackupFilePath(fullSettingsPath);
                    if (!CreateMigrationBackup(fullSettingsPath, migrationBackupPath))
                    {
                        settings.IsReadOnly = true;
                        settings.LoadWarning = "移行前バックアップを作成できなかったため、設定を上書きしません。";
                        return settings;
                    }

                    if (!SaveSettings(settings, fullSettingsPath))
                    {
                        settings.IsReadOnly = true;
                        settings.LoadWarning = "設定の移行保存に失敗したため、元ファイルを保持して読み取り専用で使用します。";
                        Console.WriteLine(settings.LoadWarning);
                    }
                    else
                    {
                        Console.WriteLine($"設定をバージョン{AppSettings.CurrentSettingsVersion}へ移行しました。");
                    }
                }

                return settings;
            }
            catch (Exception primaryException)
            {
                Console.WriteLine($"設定の読み込み中にエラーが発生しました: {primaryException.Message}");

                string backupFilePath = GetBackupFilePath(fullSettingsPath);
                if (File.Exists(backupFilePath))
                {
                    try
                    {
                        AppSettings backupSettings = DeserializeSettings(backupFilePath);
                        if (backupSettings.SettingsVersion <= AppSettings.CurrentSettingsVersion)
                        {
                            NormalizeSettings(backupSettings);
                        }

                        backupSettings.IsReadOnly = true;
                        backupSettings.LoadWarning =
                            "設定本体を読み込めなかったため、バックアップを読み取り専用で使用しています。";
                        Console.WriteLine(backupSettings.LoadWarning);
                        return backupSettings;
                    }
                    catch (Exception backupException)
                    {
                        Console.WriteLine($"バックアップ設定も読み込めませんでした: {backupException.Message}");
                    }
                }

                var safeDefaults = new AppSettings
                {
                    IsReadOnly = true,
                    LoadWarning = "設定を読み込めなかったため、安全のため自動保存を停止しています。"
                };
                NormalizeSettings(safeDefaults);
                safeDefaults.IsReadOnly = true;
                return safeDefaults;
            }
        }

        public static string GetBackupFilePath(string settingsFilePath)
        {
            return Path.GetFullPath(settingsFilePath) + ".bak";
        }

        public static string GetMigrationBackupFilePath(string settingsFilePath)
        {
            string fullSettingsPath = Path.GetFullPath(settingsFilePath);
            string directory = Path.GetDirectoryName(fullSettingsPath);
            string fileName = Path.GetFileNameWithoutExtension(fullSettingsPath);
            string extension = Path.GetExtension(fullSettingsPath);
            return Path.Combine(
                directory,
                fileName + $".pre-v{AppSettings.CurrentSettingsVersion}" + extension);
        }

        public static List<ButtonSetting> CreateButtonSettingsList(ObservableCollection<ActionButton> buttons)
        {
            var buttonSettings = new List<ButtonSetting>();
            if (buttons == null)
            {
                return buttonSettings;
            }

            foreach (var button in buttons)
            {
                buttonSettings.Add(ButtonSetting.FromActionButton(button));
            }

            return buttonSettings;
        }

        private static bool NormalizeSettings(AppSettings settings)
        {
            bool changed = settings.SettingsVersion != AppSettings.CurrentSettingsVersion;
            settings.Pages = settings.Pages ?? new List<PageSetting>();
            List<ButtonSetting> legacyButtons = settings.Buttons ?? new List<ButtonSetting>();

            if (settings.Pages.Count == 0)
            {
                settings.Pages.Add(new PageSetting
                {
                    Name = "メイン",
                    Buttons = legacyButtons
                        .Select(button => button?.Clone() ?? new ButtonSetting())
                        .ToList()
                });
                changed = true;
            }
            else if (legacyButtons.Count > 0)
            {
                PageSetting firstPage = settings.Pages.FirstOrDefault(page => page != null);
                if (firstPage != null && (firstPage.Buttons == null || firstPage.Buttons.Count == 0))
                {
                    firstPage.Buttons = legacyButtons
                        .Select(button => button?.Clone() ?? new ButtonSetting())
                        .ToList();
                }
                else
                {
                    settings.Pages.Add(new PageSetting
                    {
                        Name = "移行されたボタン",
                        Buttons = legacyButtons
                            .Select(button => button?.Clone() ?? new ButtonSetting())
                            .ToList()
                    });
                }

                changed = true;
            }

            var normalizedPages = new List<PageSetting>();
            var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int pageNumber = 1;

            foreach (PageSetting pageValue in settings.Pages)
            {
                PageSetting page = pageValue ?? new PageSetting();
                if (pageValue == null)
                {
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(page.Id) || !usedIds.Add(page.Id))
                {
                    page.Id = Guid.NewGuid().ToString("N");
                    usedIds.Add(page.Id);
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(page.Name))
                {
                    page.Name = $"ページ {pageNumber}";
                    changed = true;
                }
                else
                {
                    string trimmedName = page.Name.Trim();
                    if (!string.Equals(trimmedName, page.Name, StringComparison.Ordinal))
                    {
                        page.Name = trimmedName;
                        changed = true;
                    }
                }

                if (page.Buttons == null)
                {
                    page.Buttons = new List<ButtonSetting>();
                    changed = true;
                }

                for (int index = 0; index < page.Buttons.Count; index++)
                {
                    if (page.Buttons[index] == null)
                    {
                        page.Buttons[index] = new ButtonSetting();
                        changed = true;
                    }

                    ButtonSetting button = page.Buttons[index];
                    if (button.MacroActions == null)
                    {
                        button.MacroActions = new List<MacroActionStep>();
                        changed = true;
                    }
                    else if (button.MacroActions.Any(action => action == null))
                    {
                        button.MacroActions = button.MacroActions
                            .Where(action => action != null)
                            .ToList();
                        changed = true;
                    }
                }

                normalizedPages.Add(page);
                pageNumber++;
            }

            settings.Pages = normalizedPages;
            if (settings.Pages.All(page => !string.Equals(
                page.Id,
                settings.ActivePageId,
                StringComparison.OrdinalIgnoreCase)))
            {
                settings.ActivePageId = settings.Pages[0].Id;
                changed = true;
            }

            if (legacyButtons.Count > 0)
            {
                settings.Buttons = new List<ButtonSetting>();
                changed = true;
            }
            else if (settings.Buttons == null)
            {
                settings.Buttons = new List<ButtonSetting>();
            }

            settings.SettingsVersion = AppSettings.CurrentSettingsVersion;
            return changed;
        }

        private static bool CreateMigrationBackup(string sourcePath, string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                {
                    File.Copy(sourcePath, backupPath, false);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"移行前バックアップを作成できませんでした: {ex.Message}");
                return false;
            }
        }

        private static void SerializeSettings(AppSettings settings, string filePath)
        {
            var xmlSettings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
                IndentChars = "  ",
                OmitXmlDeclaration = false
            };

            using (var fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var xmlWriter = XmlWriter.Create(fileStream, xmlSettings))
            {
                var serializer = new XmlSerializer(typeof(AppSettings));
                serializer.Serialize(xmlWriter, settings);
            }
        }

        private static AppSettings DeserializeSettings(string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var serializer = new XmlSerializer(typeof(AppSettings));
                return (AppSettings)serializer.Deserialize(fileStream);
            }
        }
    }
}
