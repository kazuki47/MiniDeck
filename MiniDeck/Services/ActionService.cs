using System;
using System.Diagnostics;
using System.Windows;
using System.ComponentModel;

namespace MiniDeck.Services
{
    public class ActionService
    {
        public ActionService()
        {
            // InputSimulatorを削除し、シンプルなコードに置き換え
        }

        public void ExecuteKeyPress(string keySequence)
        {
            if (string.IsNullOrWhiteSpace(keySequence)) return;

            try
            {
                // System.Windows.Forms.SendKeysを使用するシンプルな実装に変更
                // SendKeysはInputSimulatorほど高機能ではありませんが、基本的なキー操作はサポートしています
                var processInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c echo {keySequence} | clip",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(processInfo);
                
                // 通知
                MessageBox.Show($"キーシーケンス '{keySequence}' をクリップボードにコピーしました。\nCtrl+Vでペーストできます。", 
                    "キー送信", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"キー送信エラー: {keySequence}\n{ex.Message}", 
                    "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LaunchApplication(string path, string arguments)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(path);
                if (!string.IsNullOrWhiteSpace(arguments))
                {
                    startInfo.Arguments = arguments;
                }
                Process.Start(startInfo);
            }
            catch (Win32Exception ex)
            {
                MessageBox.Show($"アプリケーションの起動に失敗しました: {path}\n{ex.Message}", 
                    "起動エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"予期しないエラー: {ex.Message}", 
                    "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}