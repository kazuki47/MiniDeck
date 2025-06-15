using MiniDeck.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Xml.Serialization;

namespace MiniDeck.Services
{
    /// <summary>
    /// アプリケーション設定の保存と読み込みを行うサービスクラス
    /// </summary>
    public class SettingsService
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MiniDeck", "settings.xml");

        /// <summary>
        /// アプリケーション設定を保存する
        /// </summary>
        /// <param name="settings">保存する設定</param>
        /// <returns>保存に成功したかどうか</returns>
        public static bool SaveSettings(AppSettings settings)
        {
            try
            {
                Console.WriteLine("SettingsService.SaveSettings: 保存処理を開始します");
                
                // 設定ファイルのディレクトリを作成
                string directory = Path.GetDirectoryName(SettingsFilePath);
                Console.WriteLine($"設定ディレクトリ: {directory}");
                
                if (!Directory.Exists(directory))
                {
                    Console.WriteLine("設定ディレクトリが存在しないため作成します");
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"設定ディレクトリを作成しました: {directory}");
                }
                else
                {
                    Console.WriteLine("設定ディレクトリは既に存在します");
                }
                
                // 保存前のボタン情報をログ出力
                Console.WriteLine($"保存するボタン設定数: {settings.Buttons?.Count ?? 0}");
                if (settings.Buttons != null)
                {
                    for (int i = 0; i < settings.Buttons.Count && i < 5; i++) // 最初の5個のみ表示
                    {
                        var btn = settings.Buttons[i];
                        Console.WriteLine($"  ボタン[{i}]: {btn.DisplayText}, タイプ: {btn.ActionType}, アプリ: {btn.ApplicationPath}");
                    }
                }                // XMLシリアライザを使用して設定を保存（UTF-8エンコーディングでBOMなし）
                Console.WriteLine($"XMLファイルに保存中: {SettingsFilePath}");
                var xmlSettings = new System.Xml.XmlWriterSettings
                {
                    Encoding = new System.Text.UTF8Encoding(false), // BOMなしUTF-8
                    Indent = true,
                    IndentChars = "  ",
                    OmitXmlDeclaration = false
                };
                
                using (var fileStream = new FileStream(SettingsFilePath, FileMode.Create, FileAccess.Write))
                using (var xmlWriter = System.Xml.XmlWriter.Create(fileStream, xmlSettings))
                {
                    var serializer = new XmlSerializer(typeof(AppSettings));
                    serializer.Serialize(xmlWriter, settings);
                    xmlWriter.Flush();
                }

                // 保存後の確認
                bool fileExists = File.Exists(SettingsFilePath);
                long fileSize = fileExists ? new FileInfo(SettingsFilePath).Length : 0;
                Console.WriteLine($"設定を保存しました: {SettingsFilePath}");
                Console.WriteLine($"ファイル存在: {fileExists}, サイズ: {fileSize} バイト");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"設定の保存中にエラーが発生しました: {ex.Message}");
                Console.WriteLine($"スタックトレース: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// アプリケーション設定を読み込む
        /// </summary>
        /// <returns>読み込んだ設定、または新しい設定</returns>
        public static AppSettings LoadSettings()
        {
            try
            {
                Console.WriteLine("SettingsService.LoadSettings: 読み込み処理を開始します");
                Console.WriteLine($"設定ファイルパス: {SettingsFilePath}");
                
                // ファイルが存在しない場合はデフォルト設定を返す
                if (!File.Exists(SettingsFilePath))
                {
                    Console.WriteLine("設定ファイルが見つからないため、デフォルト設定を使用します");
                    var defaultSettings = new AppSettings();
                    Console.WriteLine($"デフォルト設定のボタン数: {defaultSettings.Buttons?.Count ?? 0}");
                    return defaultSettings;
                }

                // ファイル情報を出力
                var fileInfo = new FileInfo(SettingsFilePath);
                Console.WriteLine($"設定ファイル情報 - サイズ: {fileInfo.Length} バイト, 更新日時: {fileInfo.LastWriteTime}");                // XMLシリアライザを使用して設定を読み込む（UTF-8エンコーディングで）
                Console.WriteLine("XMLファイルから設定を読み込み中...");
                AppSettings settings;
                
                using (var fileStream = new FileStream(SettingsFilePath, FileMode.Open, FileAccess.Read))
                {
                    // UTF-8でファイルを読み込み
                    var serializer = new XmlSerializer(typeof(AppSettings));
                    settings = (AppSettings)serializer.Deserialize(fileStream);
                }
                
                Console.WriteLine($"設定を読み込みました: {SettingsFilePath}");
                Console.WriteLine($"読み込んだボタン設定数: {settings.Buttons?.Count ?? 0}");
                
                // 読み込んだボタン情報をログ出力
                if (settings.Buttons != null)
                {
                    for (int i = 0; i < settings.Buttons.Count && i < 5; i++) // 最初の5個のみ表示
                    {
                        var btn = settings.Buttons[i];
                        Console.WriteLine($"  読み込みボタン[{i}]: {btn.DisplayText}, タイプ: {btn.ActionType}, アプリ: {btn.ApplicationPath}");
                    }
                }
                
                return settings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"設定の読み込み中にエラーが発生しました: {ex.Message}");
                Console.WriteLine($"スタックトレース: {ex.StackTrace}");
                
                // エラーが発生した場合はデフォルト設定を返す
                Console.WriteLine("エラーのため、デフォルト設定を返します");
                return new AppSettings();
            }
        }

        /// <summary>
        /// AppSettingsからButtonSettingsリストを作成する
        /// </summary>
        /// <param name="buttons">ActionButtonのコレクション</param>
        /// <returns>ButtonSettingのリスト</returns>
        public static List<ButtonSetting> CreateButtonSettingsList(ObservableCollection<ActionButton> buttons)
        {
            var buttonSettings = new List<ButtonSetting>();
            
            foreach (var button in buttons)
            {
                buttonSettings.Add(ButtonSetting.FromActionButton(button));
            }
            
            return buttonSettings;
        }
    }
}
