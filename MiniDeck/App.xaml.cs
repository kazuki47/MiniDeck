using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MiniDeck
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // グローバル例外ハンドラーを設定
            this.DispatcherUnhandledException += (sender, args) =>
            {
                string errorMessage = $"予期しないエラーが発生しました:\n\n" +
                                    $"エラー: {args.Exception.Message}\n\n" +
                                    $"スタックトレース:\n{args.Exception.StackTrace}";
                
                MessageBox.Show(errorMessage, "アプリケーションエラー", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // コンソールにも出力
                Console.WriteLine(errorMessage);
                
                args.Handled = true;
                this.Shutdown();
            };
            
            base.OnStartup(e);
        }
    }
}
