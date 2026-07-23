using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;

namespace MiniDeck.Services
{
    public static class AutoStartService
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "MiniDeck";

        public static bool IsEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false))
                {
                    return !string.IsNullOrWhiteSpace(key?.GetValue(ValueName) as string);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"自動起動設定の読み込み中にエラーが発生しました: {ex.Message}");
                return false;
            }
        }

        public static void Apply(bool enabled)
        {
            if (enabled)
            {
                string expectedCommand = GetExpectedCommand();

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
                {
                    if (key == null)
                    {
                        throw new InvalidOperationException("Windowsの自動起動設定を開けませんでした。");
                    }

                    string currentCommand = key.GetValue(ValueName) as string;
                    if (!string.Equals(currentCommand?.Trim(), expectedCommand, StringComparison.OrdinalIgnoreCase))
                    {
                        key.SetValue(ValueName, expectedCommand, RegistryValueKind.String);
                    }
                }

                return;
            }

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true))
            {
                key?.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }

        private static string GetExpectedCommand()
        {
            string executablePath = typeof(AutoStartService).Assembly.Location;
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                throw new InvalidOperationException("MiniDeckの実行ファイルの場所を取得できませんでした。");
            }

            return $"\"{Path.GetFullPath(executablePath)}\"";
        }
    }
}
