using System.Windows;
using System.Windows.Input; // MouseButtonEventArgsのため
using System.Windows.Data;  // IValueConverterのため
using System;               // NotImplementedExceptionのため
using System.Globalization; // CultureInfoのため
using MiniDeck.ViewModels;
using MiniDeck.Services;
using System.Windows.Media.Imaging; // BitmapImageのため
using System.Windows.Media;  // SolidColorBrush, ImageBrushのため
using System.Windows.Markup; // XamlReaderのため
using System.Windows.Controls; // Buttonのため
using System.IO; // StringReaderのため
using System.Collections.Generic; // IEnumerable<T>のため
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;


namespace MiniDeck
{    public partial class MainWindow : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const long WS_EX_NOACTIVATE = 0x08000000L;
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 3;

        private MainViewModel _viewModel;
        private HwndSource _windowSource;
        private readonly ButtonStateMonitorService _buttonStateMonitor = new ButtonStateMonitorService();
        private DispatcherTimer _buttonStateTimer;

        public MainWindow()
            : this(new MainViewModel(), true)
        {
        }

        internal MainWindow(MainViewModel viewModel)
            : this(viewModel, false)
        {
        }

        private MainWindow(MainViewModel viewModel, bool showInitializationError)
        {
            try
            {
                InitializeComponent();

                // 手動でウィンドウを初期化
                InitializeWindow();

                _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
                DataContext = _viewModel;
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                Loaded += MainWindow_Loaded;

                // ウィンドウ表示前から保存された最前面設定を使用する
                Topmost = _viewModel.AlwaysOnTop;

                // 初回描画より前に保存済みの背景を反映し、既定色のちらつきを防ぐ
                UpdateBackgroundOpacity();
                
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
                
            }
            catch (Exception ex)
            {
                if (!showInitializationError)
                {
                    throw new InvalidOperationException("メインウィンドウの初期化に失敗しました。", ex);
                }

                MessageBox.Show(
                    $"メインウィンドウの初期化中にエラーが発生しました: {ex.Message}\n{ex.StackTrace}",
                    "初期化エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            IntPtr windowHandle = new WindowInteropHelper(this).Handle;
            long extendedStyle = GetWindowLongPtr(windowHandle, GWL_EXSTYLE).ToInt64();
            long nonActivatingStyle = extendedStyle | WS_EX_NOACTIVATE;

            SetWindowLongPtr(
                windowHandle,
                GWL_EXSTYLE,
                new IntPtr(nonActivatingStyle));

            long appliedStyle = GetWindowLongPtr(windowHandle, GWL_EXSTYLE).ToInt64();
            if ((appliedStyle & WS_EX_NOACTIVATE) == 0)
            {
                int errorCode = Marshal.GetLastWin32Error();
                Console.WriteLine($"非アクティブウィンドウの設定に失敗しました。Win32エラー: {errorCode}");
            }

            _windowSource = HwndSource.FromHwnd(windowHandle);
            _windowSource?.AddHook(MainWindowHook);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_buttonStateTimer != null)
            {
                _buttonStateTimer.Stop();
                _buttonStateTimer.Tick -= ButtonStateTimer_Tick;
                _buttonStateTimer = null;
            }

            if (_windowSource != null)
            {
                _windowSource.RemoveHook(MainWindowHook);
                _windowSource = null;
            }

            base.OnClosed(e);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= MainWindow_Loaded;
            RefreshButtonStates();
            _buttonStateTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _buttonStateTimer.Tick += ButtonStateTimer_Tick;
            _buttonStateTimer.Start();
        }

        private void ButtonStateTimer_Tick(object sender, EventArgs e)
        {
            RefreshButtonStates();
        }

        private void RefreshButtonStates()
        {
            try
            {
                _buttonStateMonitor.Refresh(_viewModel?.Buttons);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ボタン状態の更新に失敗しました: {ex.Message}");
            }
        }

        private IntPtr MainWindowHook(
            IntPtr hwnd,
            int message,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            if (message == WM_MOUSEACTIVATE)
            {
                handled = true;
                return new IntPtr(MA_NOACTIVATE);
            }

            return IntPtr.Zero;
        }

        private static IntPtr GetWindowLongPtr(IntPtr windowHandle, int index)
        {
            return IntPtr.Size == 8
                ? GetWindowLongPtr64(windowHandle, index)
                : new IntPtr(GetWindowLong32(windowHandle, index));
        }

        private static IntPtr SetWindowLongPtr(IntPtr windowHandle, int index, IntPtr newValue)
        {
            return IntPtr.Size == 8
                ? SetWindowLongPtr64(windowHandle, index, newValue)
                : new IntPtr(SetWindowLong32(windowHandle, index, newValue.ToInt32()));
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern int GetWindowLong32(IntPtr windowHandle, int index);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr windowHandle, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int SetWindowLong32(IntPtr windowHandle, int index, int newValue);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr windowHandle, int index, IntPtr newValue);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr windowHandle);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindow(IntPtr windowHandle);
        
        private void AdjustWindowSize()
        {
            // ボタンのサイズと余白を考慮してウィンドウサイズを計算
            const int BUTTON_WIDTH = 80;
            const int BUTTON_HEIGHT = 80;
            const int BUTTON_HORIZONTAL_MARGIN = 8;
            const int BUTTON_VERTICAL_MARGIN = 4;
            const int HEADER_HEIGHT = 30;    // タイトルバー
            const int WINDOW_PADDING = 10;   // ウィンドウ内部の余白

            int pageNavigationWidth = _viewModel.HasMultiplePages
                ? 2 * (BUTTON_WIDTH + BUTTON_HORIZONTAL_MARGIN)
                : 0;

            int totalWidth = (_viewModel.ButtonColumns * (BUTTON_WIDTH + BUTTON_HORIZONTAL_MARGIN)) +
                             pageNavigationWidth +
                             (WINDOW_PADDING * 2);
            int totalHeight = (_viewModel.ButtonRows * (BUTTON_HEIGHT + BUTTON_VERTICAL_MARGIN)) + HEADER_HEIGHT + (WINDOW_PADDING * 2);

            Width = totalWidth;
            Height = totalHeight;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.PageCount) && IsLoaded)
            {
                ApplyLayoutFromSettings();
            }
        }

        internal void ApplyLayoutFromSettings()
        {
            if (_viewModel == null)
            {
                return;
            }

            // ウィンドウ下端を維持し、行数増減時に画面外へ伸びにくくする
            double currentBottom = Top + ActualHeight;
            AdjustWindowSize();
            Top = currentBottom - Height;
        }

        internal void ApplyWindowSettingsFromViewModel()
        {
            ApplyLayoutFromSettings();
            Topmost = _viewModel.AlwaysOnTop;
            UpdateBackgroundOpacity();
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
            this.ShowActivated = false;
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
            // 保存された一般設定を起動時に反映
            Topmost = _viewModel.AlwaysOnTop;

            // 初期ロード時に背景透明度を適用
            UpdateBackgroundOpacity();

            // 保存された行列数に合わせてウィンドウサイズを調整
            AdjustWindowSize();
            
            // ウィンドウの位置を画面左下に設定（レイアウト完了後）
            PositionWindowAtBottomLeft();
            
            // ウィンドウの子要素にマウスイベントを確実に伝播させる
            var panel = this.Content as Panel;
            if (panel != null)
            {
                // パネル自体がマウスイベントをキャプチャするようにする
                panel.Background = Brushes.Transparent;
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
                bool backgroundImageApplied = false;

                // 背景画像の処理
                if (_viewModel.UseBackgroundImage && !string.IsNullOrEmpty(_viewModel.BackgroundImagePath))
                {
                    try
                    {
                        if (ImageStorageService.TryResolveImagePath(
                            _viewModel.BackgroundImagePath,
                            out string fullPath))
                        {
                            BitmapImage bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                            bitmap.EndInit();
                            bitmap.Freeze();

                            ImageBrush imageBrush = new ImageBrush
                            {
                                ImageSource = bitmap,
                                Stretch = Stretch.UniformToFill,
                                Opacity = ClampOpacity(_viewModel.BackgroundOpacity)
                            };

                            Background = imageBrush;
                            backgroundImageApplied = true;
                            Console.WriteLine($"背景画像を適用しました: {fullPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"背景画像の適用中にエラー: {ex.Message}");
                    }
                }

                // 色背景の設定時、または背景画像を読み込めない場合は保存色を使用する
                if (!backgroundImageApplied)
                {
                    Background = CreateBackgroundColorBrush(
                        _viewModel.BackgroundColor,
                        _viewModel.BackgroundOpacity);
                    Console.WriteLine($"背景色を適用しました: {_viewModel.BackgroundColor}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"背景透明度の更新中にエラーが発生しました: {ex.Message}");
            }
        }
        
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            IntPtr previousForegroundWindow = GetForegroundWindow();
            IntPtr mainWindowHandle = new WindowInteropHelper(this).Handle;

            if (previousForegroundWindow == mainWindowHandle)
            {
                previousForegroundWindow = IntPtr.Zero;
            }

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
            finally
            {
                if (previousForegroundWindow != IntPtr.Zero && IsWindow(previousForegroundWindow))
                {
                    if (!SetForegroundWindow(previousForegroundWindow))
                    {
                        Console.WriteLine("設定画面を開く前のウィンドウへフォーカスを戻せませんでした。");
                    }
                }
            }
        }

        internal static SolidColorBrush CreateBackgroundColorBrush(string colorValue, double opacity)
        {
            Color color;
            try
            {
                color = (Color)ColorConverter.ConvertFromString(colorValue ?? "#FFFFFFFF");
            }
            catch (FormatException)
            {
                color = Colors.White;
            }

            return new SolidColorBrush(color)
            {
                Opacity = ClampOpacity(opacity)
            };
        }

        private static double ClampOpacity(double opacity)
        {
            if (double.IsNaN(opacity))
            {
                return 1.0;
            }

            return Math.Max(0.0, Math.Min(1.0, opacity));
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
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF"));
                }
            }
            
            // null や空文字の場合はデフォルト色を返す
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF"));
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return brush.Color.ToString();
            }
            return "#FFFFFFFF";
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
                if (!ImageStorageService.TryResolveImagePath(path, out string filePath))
                {
                    Console.WriteLine($"画像が見つかりません: {path}");
                    return null;
                }

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(filePath);
                bitmap.EndInit();
                return bitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"画像パス変換エラー: {ex.Message}");
                Console.WriteLine($"スタックトレース: {ex.StackTrace}");
                return null;
            }
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
