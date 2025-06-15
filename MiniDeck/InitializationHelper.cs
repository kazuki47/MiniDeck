using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media; // VisualTreeHelperのため

namespace MiniDeck
{
    /// <summary>
    /// XAMLとコードビハインド間のイベント接続を処理するヘルパークラス
    /// </summary>
    public static class InitializationHelper
    {
        /// <summary>
        /// イベントハンドラを手動で登録する
        /// </summary>
        public static void ConnectButtonEvents(MainWindow window)
        {
            try
            {
                // ウィンドウがロードされた後で実行するため、Loadedイベント内で処理する
                window.Loaded += (s, e) =>
                {
                    try
                    {
                        // UIツリーからボタンを検索
                        var settingsButton = FindChildByContent(window, "設定") as Button;
                        if (settingsButton != null)
                        {
                            settingsButton.Click += window.Settings_Click;
                            Console.WriteLine("設定ボタンのイベント接続に成功しました");
                        }
                        else
                        {
                            Console.WriteLine("設定ボタンが見つかりませんでした");
                        }

                        var closeButton = FindChildByContent(window, "X") as Button;
                        if (closeButton != null)
                        {
                            closeButton.Click += window.Close_Click;
                            Console.WriteLine("閉じるボタンのイベント接続に成功しました");
                        }
                        else
                        {
                            Console.WriteLine("閉じるボタンが見つかりませんでした");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ボタンイベント接続中にエラーが発生しました: {ex.Message}");
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"イベントハンドラ接続中にエラーが発生しました: {ex.Message}");
            }
        }

        /// <summary>
        /// 指定したコンテンツを持つ子要素を再帰的に検索する
        /// </summary>
        private static DependencyObject FindChildByContent(DependencyObject parent, string content)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is Button button && button.Content?.ToString() == content)
                {
                    return child;
                }

                DependencyObject result = FindChildByContent(child, content);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }
    }
}
