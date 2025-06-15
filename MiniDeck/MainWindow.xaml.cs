using System.Windows;
using System.Windows.Input; // MouseButtonEventArgsのため
using System.Windows.Data;  // IValueConverterのため
using System;               // NotImplementedExceptionのため
using System.Globalization; // CultureInfoのため
using MiniDeck.ViewModels;
using System.Windows.Media.Imaging; // BitmapImageのため
using System.Windows.Media;  // SolidColorBrush, ImageBrushのため
using System.Windows.Markup; // XamlReaderのため
using System.Windows.Controls; // Buttonのため
using System.IO; // StringReaderのため
using System.Collections.Generic; // IEnumerable<T>のため


namespace MiniDeck
{    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;        public MainWindow()
        {
            try
            {
                // デバッグ用：設定ファイルが存在するか確認
                string settingsPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MiniDeck", "settings.xml");
                Console.WriteLine($"設定ファイルのパス: {settingsPath}");
                bool fileExists = System.IO.File.Exists(settingsPath);
                Console.WriteLine($"設定ファイルの存在: {fileExists}");
                
                if (fileExists)
                {
                    try
                    {
                        string content = System.IO.File.ReadAllText(settingsPath);
                        Console.WriteLine("=== 設定ファイルの内容 ===");
                        Console.WriteLine(content.Substring(0, Math.Min(content.Length, 500)) + "...");
                        Console.WriteLine("=========================");
                    }
                    catch (Exception readEx)
                    {
                        Console.WriteLine($"設定ファイルの読み込みエラー: {readEx.Message}");
                    }
                }
                  // XAMLコンポーネントを初期化
                try {
                    InitializeComponent();
                }
                catch (Exception initEx) {
                    Console.WriteLine($"InitializeComponentエラー: {initEx.Message}");
                }
                
                // 手動でウィンドウを初期化
                InitializeWindow();
                  // デバッグ用：設定ファイルが存在するか確認
                ShowSettingsFileInfo();
                
                // XAMLで作成されたMainViewModelを取得
                _viewModel = DataContext as MainViewModel;
                
                // DataContextが設定されていない場合は手動で作成
                if (_viewModel == null)
                {
                    Console.WriteLine("警告: DataContextが設定されていないため、MainViewModelを新規作成します");
                    _viewModel = new MainViewModel();
                    DataContext = _viewModel;
                }
                else
                {
                    Console.WriteLine("XAML経由でMainViewModelが正しく設定されています");
                }
                
                // リソースディレクトリが存在するか確認
                CheckResourceDirectory();
                
                // アプリケーション終了時に設定を保存するためのイベントハンドラを追加
                this.Closing += MainWindow_Closing;
                
                // マウスイベントを追加（透明部分でもキャプチャ）
                this.MouseDown += MainWindow_MouseDown;
                  // デバッグ: 現在のボタン設定を表示
                Console.WriteLine($"初期ボタン数: {_viewModel.Buttons.Count}");
                foreach (var button in _viewModel.Buttons)
                {
                    if (button.ActionType == MiniDeck.Models.ActionType.LaunchApplication)
                    {
                        Console.WriteLine($"ボタン: {button.DisplayText}, アクション: {button.ActionType}, アプリ: {button.ApplicationPath}");
                    }
                    else
                    {
                        Console.WriteLine($"ボタン: {button.DisplayText}, アクション: {button.ActionType}");
                    }
                }
                
                // 設定保存・読み込みテストを実行
                TestSettingsSaveLoad();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"メインウィンドウの初期化中にエラーが発生しました: {ex.Message}\n{ex.StackTrace}",
                    "初期化エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void AdjustWindowSize()
        {
            // ボタンのサイズと余白を考慮してウィンドウサイズを計算
            // ボタンのサイズは XAML で Width="80" Height="70" Margin="4" と定義されている
            const int BUTTON_WIDTH = 80;
            const int BUTTON_HEIGHT = 70;
            const int BUTTON_MARGIN = 4 * 2; // 上下左右の余白 (余白は両側にあるので2倍)
            const int HEADER_HEIGHT = 30;    // タイトルバー
            const int FOOTER_HEIGHT = 20;    // ステータスバー
            const int WINDOW_PADDING = 10;   // ウィンドウ内部の余白
            
            int totalWidth = (_viewModel.ButtonColumns * (BUTTON_WIDTH + BUTTON_MARGIN)) + (WINDOW_PADDING * 2);
            int totalHeight = (_viewModel.ButtonRows * (BUTTON_HEIGHT + BUTTON_MARGIN)) + HEADER_HEIGHT + FOOTER_HEIGHT + (WINDOW_PADDING * 2);
            
            Width = totalWidth;
            Height = totalHeight;
        }
        
        private void PositionWindowAtBottomLeft()
        {
            // ウィンドウの位置を画面左下に設定
            Top = SystemParameters.PrimaryScreenHeight - Height -60; // タスクバーの高さを考慮して少し上に
            Left = 0; // 左端から少し離れた位置
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // ウィンドウのドラッグ移動
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
          private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }        private void InitializeWindow()
        {
            // 基本的なウィンドウプロパティを設定
            this.Title = "MiniDeck";
            this.Height = 250;
            this.Width = 400;
            this.AllowsTransparency = true;
            this.WindowStyle = WindowStyle.None;
            this.Topmost = true;
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.BorderThickness = new Thickness(1);
            this.BorderBrush = Brushes.Black;
            
            // ウィンドウの位置を画面左下に設定
            PositionWindowAtBottomLeft();
            
            // アイコンの設定（コードから明示的に）
            try
            {
                Uri iconUri = new Uri("pack://application:,,,/image/MiniDeckIcon.ico", UriKind.Absolute);
                this.Icon = BitmapFrame.Create(iconUri);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"アイコンの設定中にエラーが発生しました: {ex.Message}");
            }
            
            // ウィンドウイベントハンドラの設定
            this.Loaded += Window_Loaded;
            
            // マウスイベントハンドラを追加（透明部分でもイベントをキャプチャ）
            this.MouseDown += MainWindow_MouseDown;
            this.MouseUp += MainWindow_MouseUp;
            this.MouseMove += MainWindow_MouseMove;
        }

        // マウスイベントをキャプチャするハンドラー
        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // マウスイベントが確実にウィンドウにキャプチャされるようにする
            this.CaptureMouse();
            
            // 左クリックの場合はドラッグ移動を開始
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
            {
                Window_MouseLeftButtonDown(sender, e);
            }
        }

        private void MainWindow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // マウスキャプチャを解放
            this.ReleaseMouseCapture();
        }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            // マウス移動のイベント処理（必要に応じて）
            // このイベントハンドラはマウスイベントがウィンドウにキャプチャされることを確認するために追加
        }
        
        /// <summary>
        /// 指定した型の子要素をすべて取得する
        /// </summary>
        private IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is T item)
                {
                    yield return item;
                }
                
                foreach (var childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 初期ロード時に背景透明度を適用
            UpdateBackgroundOpacity();
            
            // ウィンドウの位置を画面左下に設定（レイアウト完了後）
            PositionWindowAtBottomLeft();
            
            // ウィンドウの子要素にマウスイベントを確実に伝播させる
            var panel = this.Content as Panel;
            if (panel != null)
            {
                // パネル自体がマウスイベントをキャプチャするようにする
                panel.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)); // ほぼ透明だがイベントはキャプチャ
            }
            
            // ボタンイベントの手動接続
            try
            {
                Console.WriteLine("ボタンとイベントの接続を開始します");
                
                // 名前付きコントロールを取得し、イベントハンドラを接続
                var titleBorder = this.FindName("TitleBorder") as Border;
                if (titleBorder != null)
                {
                    titleBorder.MouseLeftButtonDown += Window_MouseLeftButtonDown;
                    Console.WriteLine("タイトルバーのドラッグイベント接続に成功しました");
                }
                
                var settingsButton = this.FindName("SettingsButton") as Button;
                if (settingsButton != null)
                {
                    settingsButton.Click += Settings_Click;
                    Console.WriteLine("設定ボタンのイベント接続に成功しました");
                }
                
                var closeButton = this.FindName("CloseButton") as Button;
                if (closeButton != null)
                {
                    closeButton.Click += Close_Click;
                    Console.WriteLine("閉じるボタンのイベント接続に成功しました");
                }
                
                // 念のため、コンテンツが一致するボタンも探して接続
                var buttons = FindVisualChildren<Button>(this);
                foreach (var button in buttons)
                {
                    if (button.Content?.ToString() == "設定" && button.Name != "SettingsButton")
                    {
                        button.Click += Settings_Click;
                        Console.WriteLine("追加の設定ボタン接続に成功しました");
                    }
                    else if (button.Content?.ToString() == "X" && button.Name != "CloseButton")
                    {
                        button.Click += Close_Click;
                        Console.WriteLine("追加の閉じるボタン接続に成功しました");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"イベント接続中にエラーが発生しました: {ex.Message}");
            }
        }        // 背景透明度の更新
        private void UpdateBackgroundOpacity()
        {
            if (_viewModel == null) return;
            
            try
            {
                // 背景画像の処理
                if (_viewModel.UseBackgroundImage && !string.IsNullOrEmpty(_viewModel.BackgroundImagePath))
                {
                    // 背景画像を使用する場合 - 鮮やかさを保持
                    try {
                        string fullPath;
                        if (_viewModel.BackgroundImagePath.StartsWith("/"))
                        {
                            // アプリケーションリソースを処理
                            string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                            string baseDir = System.IO.Path.GetDirectoryName(appPath);
                            fullPath = baseDir + _viewModel.BackgroundImagePath.Replace('/', '\\');
                        }
                        else
                        {
                            fullPath = _viewModel.BackgroundImagePath;
                        }
                        
                        if (System.IO.File.Exists(fullPath))
                        {
                            // 透明ブラシをレイヤーとして追加して、マウスイベントを確実にキャプチャ
                            // 不透明度は最小限に設定（0.01）して画面表示に影響しないようにする
                            SolidColorBrush hitTestBrush = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
                            
                            // 背景画像用のブラシを設定
                            ImageBrush imageBrush = new ImageBrush
                            {
                                ImageSource = new BitmapImage(new Uri(fullPath)),
                                Stretch = Stretch.UniformToFill,
                                Opacity = _viewModel.BackgroundOpacity // 透明度を設定
                            };
                            
                            // グラデーションブラシを使って複数のレイヤーを表現
                            var brushes = new GradientStopCollection();
                            brushes.Add(new GradientStop(hitTestBrush.Color, 0.0)); // ヒットテスト用の透明なレイヤ
                            
                            // ブラシを適用
                            this.Background = imageBrush;
                            Console.WriteLine($"マウスイベントをキャプチャする背景画像を適用しました。");
                        }
                    }
                    catch (Exception ex) {
                        Console.WriteLine($"背景画像の適用中にエラー: {ex.Message}");
                    }
                }
                else
                {
                    // 背景画像を使用しない場合は半透明ブラシを設定（マウスイベントは確実にキャプチャ）
                    // Color.FromArgb(1, 0, 0, 0) はほぼ完全な透明色だが、マウスイベントはキャプチャする
                    SolidColorBrush transparentBrush = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
                    this.Background = transparentBrush;
                    Console.WriteLine("マウスイベントをキャプチャする透明背景を適用しました。");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"背景透明度の更新中にエラーが発生しました: {ex.Message}");
            }
        }
        
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 設定ウィンドウを表示
                var settingsWindow = new SettingsWindow(_viewModel);
                settingsWindow.Owner = this;
                settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"設定ウィンドウを開く際にエラーが発生しました:\n{ex.Message}\n\nスタックトレース:\n{ex.StackTrace}", 
                    "設定ウィンドウエラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // リソースディレクトリの存在を確認するメソッド
        private void CheckResourceDirectory()
        {
            try
            {
                // アプリケーションのリソースディレクトリへのパスを取得
                string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string baseDir = System.IO.Path.GetDirectoryName(appPath);
                string resourceDir = System.IO.Path.Combine(baseDir, "Resources", "Icons");
                
                Console.WriteLine($"リソースディレクトリのパス: {resourceDir}");
                
                // ディレクトリが存在するか確認
                if (!System.IO.Directory.Exists(resourceDir))
                {
                    Console.WriteLine($"警告: リソースディレクトリが見つかりません: {resourceDir}");
                    return;
                }
                
                // 各アイコンファイルの存在をチェック
                string[] iconFiles = { "memo_icon.png", "calculator_icon.png", "default_icon.jpg" };
                foreach (var iconFile in iconFiles)
                {
                    string iconPath = System.IO.Path.Combine(resourceDir, iconFile);
                    if (System.IO.File.Exists(iconPath))
                    {
                        Console.WriteLine($"アイコンファイルが見つかりました: {iconPath}");
                    }
                    else
                    {
                        Console.WriteLine($"警告: アイコンファイルが見つかりません: {iconPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"リソースチェック中にエラーが発生しました: {ex.Message}");
            }
        }        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                Console.WriteLine("MainWindow_Closing: アプリケーション終了時の設定保存を開始します");
                // アプリケーション終了時に設定を保存
                if (_viewModel != null)
                {
                    // 現在のボタン設定をログ出力
                    Console.WriteLine($"保存前のボタン数: {_viewModel.Buttons.Count}");
                    foreach (var button in _viewModel.Buttons)
                    {
                        if (button.ActionType == MiniDeck.Models.ActionType.LaunchApplication)
                        {
                            Console.WriteLine($"保存対象ボタン: {button.DisplayText}, アクション: {button.ActionType}, アプリ: {button.ApplicationPath}");
                        }
                    }
                    
                    // 設定を保存
                    _viewModel.SaveSettings();
                    Console.WriteLine("アプリケーション終了時に設定を保存しました");
                    
                    // 保存後、設定ファイルの存在確認
                    string settingsPath = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "MiniDeck", "settings.xml");
                    bool fileExists = System.IO.File.Exists(settingsPath);
                    Console.WriteLine($"終了時の設定ファイル存在確認: {fileExists}, サイズ: {(fileExists ? new System.IO.FileInfo(settingsPath).Length : 0)}バイト");
                }
                else
                {
                    Console.WriteLine("エラー: _viewModel が null のため、設定を保存できません");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"設定の保存中にエラーが発生しました: {ex.Message}");
                Console.WriteLine($"スタックトレース: {ex.StackTrace}");
                // 終了処理なのでメッセージボックスは表示しない（表示すると閉じられなくなる可能性がある）
            }
        }

        // テスト用：設定ファイルの詳細情報を表示するメソッド
        private void ShowSettingsFileInfo()
        {
            try
            {
                string settingsPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MiniDeck", "settings.xml");
                
                Console.WriteLine("=== 設定ファイル詳細情報 ===");
                Console.WriteLine($"パス: {settingsPath}");
                Console.WriteLine($"存在: {System.IO.File.Exists(settingsPath)}");
                
                if (System.IO.File.Exists(settingsPath))
                {
                    var fileInfo = new System.IO.FileInfo(settingsPath);
                    Console.WriteLine($"サイズ: {fileInfo.Length} バイト");
                    Console.WriteLine($"作成日時: {fileInfo.CreationTime}");
                    Console.WriteLine($"更新日時: {fileInfo.LastWriteTime}");
                    
                    // ファイル内容を表示（最初の1000文字）
                    string content = System.IO.File.ReadAllText(settingsPath);
                    Console.WriteLine("=== ファイル内容 ===");
                    Console.WriteLine(content.Length > 1000 ? content.Substring(0, 1000) + "..." : content);
                    Console.WriteLine("===================");
                }
                else
                {
                    Console.WriteLine("設定ファイルが存在しません");
                    
                    // ディレクトリが存在するかチェック
                    string directory = System.IO.Path.GetDirectoryName(settingsPath);
                    Console.WriteLine($"ディレクトリ存在: {System.IO.Directory.Exists(directory)}");
                    
                    if (!System.IO.Directory.Exists(directory))
                    {
                        Console.WriteLine("設定ディレクトリを作成します");
                        System.IO.Directory.CreateDirectory(directory);
                    }
                }
                Console.WriteLine("=============================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"設定ファイル情報取得エラー: {ex.Message}");
            }
        }
        
        // テスト用：設定保存・読み込みのデバッグ機能
        private void TestSettingsSaveLoad()
        {
            try
            {
                Console.WriteLine("=== 設定保存・読み込みテスト開始 ===");
                
                // 現在のViewModelの状態を確認
                if (_viewModel?.Buttons != null)
                {
                    Console.WriteLine($"現在のボタン数: {_viewModel.Buttons.Count}");
                    
                    for (int i = 0; i < Math.Min(_viewModel.Buttons.Count, 8); i++)
                    {
                        var btn = _viewModel.Buttons[i];
                        Console.WriteLine($"ボタン[{i}]: 表示=\"{btn.DisplayText}\", アクション={btn.ActionType}");
                        if (btn.ActionType == MiniDeck.Models.ActionType.LaunchApplication)
                        {
                            Console.WriteLine($"  → アプリパス: \"{btn.ApplicationPath}\"");
                            Console.WriteLine($"  → 引数: \"{btn.ApplicationArguments}\"");
                        }
                        else if (btn.ActionType == MiniDeck.Models.ActionType.KeyPress)
                        {
                            Console.WriteLine($"  → キーシーケンス: \"{btn.ShortcutKeySequence}\"");
                        }
                    }
                }
                
                // 手動で設定を保存
                Console.WriteLine("\n--- 手動設定保存 ---");
                _viewModel?.SaveSettings();
                
                // 設定ファイルの内容を完全に表示
                ShowCompleteSettingsFile();
                
                // 設定を手動で読み込み
                Console.WriteLine("\n--- 手動設定読み込み ---");
                var loadedSettings = MiniDeck.Services.SettingsService.LoadSettings();
                
                Console.WriteLine($"読み込んだ設定 - ボタン数: {loadedSettings.Buttons?.Count ?? 0}");
                if (loadedSettings.Buttons != null)
                {
                    for (int i = 0; i < Math.Min(loadedSettings.Buttons.Count, 8); i++)
                    {
                        var btnSetting = loadedSettings.Buttons[i];
                        Console.WriteLine($"読み込みボタン[{i}]: 表示=\"{btnSetting.DisplayText}\", アクション={btnSetting.ActionType}");
                        if (btnSetting.ActionType == MiniDeck.Models.ActionType.LaunchApplication)
                        {
                            Console.WriteLine($"  → アプリパス: \"{btnSetting.ApplicationPath}\"");
                            Console.WriteLine($"  → 引数: \"{btnSetting.ApplicationArguments}\"");
                        }
                    }
                }
                
                Console.WriteLine("=== 設定保存・読み込みテスト終了 ===\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"設定テスト中にエラー: {ex.Message}");
                Console.WriteLine($"スタックトレース: {ex.StackTrace}");
            }
        }

        // 設定ファイルの完全な内容を表示
        private void ShowCompleteSettingsFile()
        {
            try
            {
                string settingsPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MiniDeck", "settings.xml");
                
                Console.WriteLine($"\n=== 設定ファイル完全内容 ===");
                Console.WriteLine($"パス: {settingsPath}");
                
                if (System.IO.File.Exists(settingsPath))
                {
                    string content = System.IO.File.ReadAllText(settingsPath);
                    Console.WriteLine($"ファイルサイズ: {content.Length} 文字");
                    Console.WriteLine("--- ファイル内容開始 ---");
                    Console.WriteLine(content);
                    Console.WriteLine("--- ファイル内容終了 ---");
                }
                else
                {
                    Console.WriteLine("設定ファイルが存在しません！");
                }
                Console.WriteLine("================================\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"設定ファイル内容表示エラー: {ex.Message}");
            }
        }
    }
    
    // StringをSolidColorBrushに変換するコンバータ
    public class StringToSolidColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorString && !string.IsNullOrWhiteSpace(colorString))
            {
                try
                {
                    Color color = (Color)ColorConverter.ConvertFromString(colorString);
                    return new SolidColorBrush(color);
                }
                catch
                {
                    // エラーの場合はデフォルト色を返す
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF404040"));
                }
            }
            
            // null や空文字の場合はデフォルト色を返す
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF404040"));
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return brush.Color.ToString();
            }
            return "#FF000000";
        }
    }

    // ImagePathがnullまたは空文字列の場合にImageコントロールを非表示にするためのコンバータ
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    // 画像パスをURIに変換するコンバータ
    public class ImagePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(value as string))
                return null;
                
            string path = value as string;
            
            try
            {
                Console.WriteLine($"画像パス変換を試行: {path}");
                
                // URIとして解析可能か確認
                if (path.StartsWith("/"))
                {
                    // アプリケーションリソースを処理
                    string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    string baseDir = System.IO.Path.GetDirectoryName(appPath);
                    string filePath = baseDir + path.Replace('/', '\\');
                    
                    Console.WriteLine($"相対パスを変換: {path} -> {filePath}");
                    
                    if (System.IO.File.Exists(filePath))
                    {
                        try
                        {
                            // BitmapImageを使用して、正確に画像をロード
                            BitmapImage bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.UriSource = new Uri(filePath);
                            bitmap.EndInit();
                            return bitmap;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"画像読み込みエラー: {ex.Message}");
                            return null;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"画像が見つかりません: {filePath}");
                        return null;
                    }
                }
                else if (System.IO.File.Exists(path))
                {
                    // 絶対パスとして処理
                    try
                    {
                        // BitmapImageを使用して、正確に画像をロード
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(path);
                        bitmap.EndInit();
                        return bitmap;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"絶対パス画像読み込みエラー: {ex.Message}");
                        return null;
                    }
                }
                else if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
                {
                    // 絶対URIをそのまま使用
                    try
                    {
                        // BitmapImageを使用して、正確に画像をロード
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(path);
                        bitmap.EndInit();
                        return bitmap;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"絶対URI画像読み込みエラー: {ex.Message}");
                        return null;
                    }
                }
                
                // デフォルト値として処理
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"画像パス変換エラー: {ex.Message}");
                Console.WriteLine($"スタックトレース: {ex.StackTrace}");
                
                // エラー発生時でも最低限の表示を試みる
                try {
                    // エラーが発生したが、最後の手段として基本的なBitmapImageを作成
                    if (path.StartsWith("/"))
                    {
                        string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        string baseDir = System.IO.Path.GetDirectoryName(appPath);
                        // デフォルトアイコンを返す
                        string defaultIconPath = baseDir + "\\Resources\\Icons\\default_icon.jpg";
                        if (System.IO.File.Exists(defaultIconPath)) {
                            BitmapImage bitmap = new BitmapImage(new Uri(defaultIconPath));
                            return bitmap;
                        }
                    }
                } catch {
                    // 最後の手段も失敗
                }
                return null;
            }
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}