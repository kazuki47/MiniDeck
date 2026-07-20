using Microsoft.Win32;
using MiniDeck.Models;
using MiniDeck.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MiniDeck
{
    public partial class ButtonSettingsWindow : Window
    {
        public ActionButton Button { get; private set; }
        private bool _isCapturingKey = false;

        public ButtonSettingsWindow(ActionButton button)
        {
            InitializeComponent();

            // ウィンドウのサイズ変更が完了した時にイベントを登録
            this.Loaded += ButtonSettingsWindow_Loaded;

            Button = button;            // ボタンの値をUI要素に入力
            DisplayTextBox.Text = button.DisplayText;
            ImagePathBox.Text = button.ImagePath;
            ShortcutKeyBox.Text = button.ShortcutKeySequence;
            ApplicationPathBox.Text = button.ApplicationPath;
            ArgumentsBox.Text = button.ApplicationArguments;
            
            // 画像プレビューを更新
            UpdateImagePreview(button.ImagePath);

            // アクションタイプを選択
            switch (button.ActionType)
            {
                case ActionType.KeyPress:
                    ActionTypeCombo.SelectedIndex = 1;
                    KeyPressPanel.Visibility = Visibility.Visible;
                    break;
                case ActionType.LaunchApplication:
                    ActionTypeCombo.SelectedIndex = 2;
                    AppLaunchPanel.Visibility = Visibility.Visible;
                    break;
                default:
                    ActionTypeCombo.SelectedIndex = 0;
                    break;
            }
        }        private void ActionTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = ActionTypeCombo.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;

            var actionType = (ActionType)selectedItem.Tag;
            
            // パネルの表示切り替え
            KeyPressPanel.Visibility = (actionType == ActionType.KeyPress) ? 
                Visibility.Visible : Visibility.Collapsed;
                
            AppLaunchPanel.Visibility = (actionType == ActionType.LaunchApplication) ? 
                Visibility.Visible : Visibility.Collapsed;
                
            // パネルの表示状態が変わったら、ウィンドウサイズを調整
            AdjustWindowSize();
        }
          private void AdjustWindowSize()
        {
            // UIを更新してレイアウトを再計算
            this.UpdateLayout();
            
            // ウィンドウサイズを内容に合わせて調整
            this.SizeToContent = SizeToContent.WidthAndHeight;
            
            // 最低幅を保証
            if (this.ActualWidth < 500)
            {
                this.Width = 500;
            }
        }

        private void BrowseApp_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "実行ファイル (*.exe)|*.exe|すべてのファイル (*.*)|*.*",
                Title = "アプリケーションを選択"
            };

            if (dialog.ShowDialog() == true)
            {
                ApplicationPathBox.Text = dialog.FileName;
            }
        }        private void BrowseImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "画像ファイル (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp|すべてのファイル (*.*)|*.*",
                Title = "画像を選択"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // 選択されたファイルのパス
                    string selectedFilePath = dialog.FileName;
                    // ファイル名を取得
                    string fileName = System.IO.Path.GetFileName(selectedFilePath);
                      // アプリケーションのリソースフォルダへのパス
                    string resourceDir = System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                        "Resources", "Icons");
                    
                    // リソースディレクトリが存在しない場合は作成
                    if (!System.IO.Directory.Exists(resourceDir))
                    {
                        System.IO.Directory.CreateDirectory(resourceDir);
                    }
                    
                    // ファイルをコピー
                    string destPath = System.IO.Path.Combine(resourceDir, fileName);
                    System.IO.File.Copy(selectedFilePath, destPath, true);
                    
                    // 相対パスとしてセット
                    ImagePathBox.Text = $"/Resources/Icons/{fileName}";
                    
                    MessageBox.Show("画像が正常に追加されました", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"画像の追加中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    // エラー時は絶対パスをセット
                    ImagePathBox.Text = dialog.FileName;
                }
            }
        }        private void OK_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = ActionTypeCombo.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;

            try
            {
                Console.WriteLine($"ボタン設定の保存を開始します: {Button.DisplayText}");
                  // ボタン情報を更新
                Button.DisplayText = DisplayTextBox.Text ?? "";
                Button.ImagePath = ImagePathBox.Text ?? "";
                Button.ActionType = (ActionType)selectedItem.Tag;
                Button.ShortcutKeySequence = ShortcutKeyBox.Text ?? "";
                Button.ApplicationPath = ApplicationPathBox.Text ?? "";
                Button.ApplicationArguments = ArgumentsBox.Text ?? "";

                // 設定内容をログ出力
                Console.WriteLine($"更新されたボタン設定:");
                Console.WriteLine($"  表示テキスト: '{Button.DisplayText}'");
                Console.WriteLine($"  画像パス: '{Button.ImagePath}'");
                Console.WriteLine($"  アクションタイプ: {Button.ActionType}");
                if (Button.ActionType == ActionType.LaunchApplication)
                {
                    Console.WriteLine($"  アプリケーションパス: '{Button.ApplicationPath}'");
                    Console.WriteLine($"  アプリケーション引数: '{Button.ApplicationArguments}'");
                }
                else if (Button.ActionType == ActionType.KeyPress)
                {
                    Console.WriteLine($"  キーシーケンス: '{Button.ShortcutKeySequence}'");
                }

                // ボタンのプロパティ変更を明示的に通知
                Button.OnPropertyChanged("DisplayText");
                Button.OnPropertyChanged("ImagePath");
                Button.OnPropertyChanged("ActionType");
                Button.OnPropertyChanged("ShortcutKeySequence");
                Button.OnPropertyChanged("ApplicationPath");
                Button.OnPropertyChanged("ApplicationArguments");

                // 親ウィンドウから設定を保存
                bool settingsSaved = false;
                MainViewModel targetViewModel = null;

                // 設定ウィンドウから呼ばれた場合
                var settingsWindow = Owner as SettingsWindow;
                if (settingsWindow?.DataContext is MainViewModel settingsViewModel)
                {
                    targetViewModel = settingsViewModel;
                    Console.WriteLine("SettingsWindow経由でViewModel取得");
                }
                else
                {
                    // メインウィンドウから直接呼ばれた場合
                    var mainWindow = Owner as MainWindow;
                    if (mainWindow?.DataContext is MainViewModel mainViewModel)
                    {
                        targetViewModel = mainViewModel;
                        Console.WriteLine("MainWindow経由でViewModel取得");
                    }
                }

                if (targetViewModel != null)
                {
                    Console.WriteLine("ViewModelが見つかりました。設定を保存します。");
                    targetViewModel.SaveSettings();
                    settingsSaved = true;
                    
                    // 保存確認
                    string settingsPath = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "MiniDeck", "settings.xml");
                    
                    if (System.IO.File.Exists(settingsPath))
                    {
                        var fileInfo = new System.IO.FileInfo(settingsPath);
                        Console.WriteLine($"設定ファイル保存確認: サイズ={fileInfo.Length}バイト, 更新時刻={fileInfo.LastWriteTime}");
                    }
                    else
                    {
                        Console.WriteLine("警告: 設定ファイルが見つかりません");
                    }
                }

                if (!settingsSaved)
                {
                    Console.WriteLine("警告: ViewModelが見つからないため、直接保存を試みます");
                    
                    // 直接設定サービスを使用して保存
                    var currentSettings = MiniDeck.Services.SettingsService.LoadSettings();
                    if (currentSettings.Buttons == null)
                    {
                        currentSettings.Buttons = new List<MiniDeck.Models.ButtonSetting>();
                    }

                    // 現在のボタン設定を変換して追加
                    var buttonSetting = MiniDeck.Models.ButtonSetting.FromActionButton(Button);
                    Console.WriteLine($"ButtonSettingに変換: {buttonSetting.DisplayText}, {buttonSetting.ActionType}");
                    
                    // TODO: ここで正しいインデックスでボタンを更新する必要がある
                    // 現在は単純に追加しているが、既存のボタンを更新する必要がある
                    bool saved = MiniDeck.Services.SettingsService.SaveSettings(currentSettings);
                    Console.WriteLine($"直接保存結果: {saved}");
                }

                Console.WriteLine("ボタン設定の保存処理が完了しました");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ボタン設定保存中にエラーが発生: {ex.Message}");
                Console.WriteLine($"スタックトレース: {ex.StackTrace}");
                MessageBox.Show($"設定の保存中にエラーが発生しました: {ex.Message}", "エラー", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            DialogResult = true;
            Close();
        }private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
          private void ButtonSettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // ウィンドウのサイズを内容に合わせて最適化
            InvalidateMeasure();
            UpdateLayout();
            
            // アクションタイプの初期状態に応じたサイズ調整
            this.SizeToContent = SizeToContent.WidthAndHeight;
            
            // 最低サイズを設定して、すべての内容が表示されるようにする
            if (this.ActualHeight < 450)
            {
                this.MinHeight = 450;
            }
            
            if (this.ActualWidth < 500)
            {
                this.Width = 500;
                this.MinWidth = 500;
            }
        }

        // 画像パス変更時にプレビューを更新するイベントハンドラを追加
        private void ImagePathBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateImagePreview(ImagePathBox.Text);
        }
        
        // 画像プレビュー更新メソッド
        private void UpdateImagePreview(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath))
                {
                    ImagePreview.Source = null;
                    return;
                }

                string fullPath;
                if (imagePath.StartsWith("/"))
                {
                    // アプリケーションリソースを処理
                    string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    string baseDir = Path.GetDirectoryName(appPath);
                    fullPath = baseDir + imagePath.Replace('/', '\\');
                }
                else
                {
                    fullPath = imagePath;
                }

                if (File.Exists(fullPath))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(fullPath);
                    bitmap.EndInit();
                    ImagePreview.Source = bitmap;
                }
                else
                {
                    ImagePreview.Source = null;
                }
            }
            catch
            {
                ImagePreview.Source = null;
            }
        }

        // キーキャプチャボタンのクリックハンドラ
        private void CaptureKey_Click(object sender, RoutedEventArgs e)
        {
            _isCapturingKey = true;
            CaptureKeyButton.Content = "入力待ち...";
            CaptureKeyButton.IsEnabled = false;
            KeyCaptureHint.Text = "※キーを押してください（Escでキャンセル）";
            ShortcutKeyBox.Focus();
        }

        // キー入力のキャプチャハンドラ
        private void ShortcutKeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_isCapturingKey)
            {
                e.Handled = true;
                return;
            }

            e.Handled = true;

            // Escapeキーでキャンセル
            if (e.Key == Key.Escape)
            {
                StopKeyCapture();
                return;
            }

            // 修飾キーのみの場合は待機
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
                e.Key == Key.LeftAlt || e.Key == Key.RightAlt ||
                e.Key == Key.LeftShift || e.Key == Key.RightShift ||
                e.Key == Key.LWin || e.Key == Key.RWin ||
                e.Key == Key.System)
            {
                return;
            }

            // キーの組み合わせを構築
            var keySequence = new StringBuilder();
            
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                keySequence.Append("Ctrl+");
            }
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                keySequence.Append("Shift+");
            }
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                keySequence.Append("Alt+");
            }
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows))
            {
                keySequence.Append("Win+");
            }

            // メインキーを追加
            var mainKey = e.Key == Key.System ? e.SystemKey : e.Key;
            keySequence.Append(ConvertKeyToString(mainKey));

            ShortcutKeyBox.Text = keySequence.ToString();
            StopKeyCapture();
        }

        private void StopKeyCapture()
        {
            _isCapturingKey = false;
            CaptureKeyButton.Content = "キーを入力";
            CaptureKeyButton.IsEnabled = true;
            KeyCaptureHint.Text = "※「キーを入力」ボタンを押して、登録したいキーの組み合わせを押してください";
        }

        private string ConvertKeyToString(Key key)
        {
            switch (key)
            {
                // ファンクションキー
                case Key.F1: return "F1";
                case Key.F2: return "F2";
                case Key.F3: return "F3";
                case Key.F4: return "F4";
                case Key.F5: return "F5";
                case Key.F6: return "F6";
                case Key.F7: return "F7";
                case Key.F8: return "F8";
                case Key.F9: return "F9";
                case Key.F10: return "F10";
                case Key.F11: return "F11";
                case Key.F12: return "F12";

                // 特殊キー
                case Key.Return: return "Enter";
                case Key.Escape: return "Escape";
                case Key.Tab: return "Tab";
                case Key.Space: return "Space";
                case Key.Back: return "Backspace";
                case Key.Delete: return "Delete";
                case Key.Insert: return "Insert";
                case Key.Home: return "Home";
                case Key.End: return "End";
                case Key.PageUp: return "PageUp";
                case Key.PageDown: return "PageDown";
                case Key.Up: return "Up";
                case Key.Down: return "Down";
                case Key.Left: return "Left";
                case Key.Right: return "Right";
                case Key.PrintScreen: return "PrintScreen";
                case Key.Pause: return "Pause";
                case Key.CapsLock: return "CapsLock";
                case Key.NumLock: return "NumLock";
                case Key.Scroll: return "ScrollLock";

                // 数字キー (メインキーボード)
                case Key.D0: return "0";
                case Key.D1: return "1";
                case Key.D2: return "2";
                case Key.D3: return "3";
                case Key.D4: return "4";
                case Key.D5: return "5";
                case Key.D6: return "6";
                case Key.D7: return "7";
                case Key.D8: return "8";
                case Key.D9: return "9";

                // テンキー
                case Key.NumPad0: return "NumPad0";
                case Key.NumPad1: return "NumPad1";
                case Key.NumPad2: return "NumPad2";
                case Key.NumPad3: return "NumPad3";
                case Key.NumPad4: return "NumPad4";
                case Key.NumPad5: return "NumPad5";
                case Key.NumPad6: return "NumPad6";
                case Key.NumPad7: return "NumPad7";
                case Key.NumPad8: return "NumPad8";
                case Key.NumPad9: return "NumPad9";
                case Key.Multiply: return "Multiply";
                case Key.Add: return "Add";
                case Key.Subtract: return "Subtract";
                case Key.Divide: return "Divide";
                case Key.Decimal: return "Decimal";

                // アルファベット
                case Key.A: return "A";
                case Key.B: return "B";
                case Key.C: return "C";
                case Key.D: return "D";
                case Key.E: return "E";
                case Key.F: return "F";
                case Key.G: return "G";
                case Key.H: return "H";
                case Key.I: return "I";
                case Key.J: return "J";
                case Key.K: return "K";
                case Key.L: return "L";
                case Key.M: return "M";
                case Key.N: return "N";
                case Key.O: return "O";
                case Key.P: return "P";
                case Key.Q: return "Q";
                case Key.R: return "R";
                case Key.S: return "S";
                case Key.T: return "T";
                case Key.U: return "U";
                case Key.V: return "V";
                case Key.W: return "W";
                case Key.X: return "X";
                case Key.Y: return "Y";
                case Key.Z: return "Z";

                // その他
                case Key.OemMinus: return "-";
                case Key.OemPlus: return "=";
                case Key.OemOpenBrackets: return "[";
                case Key.OemCloseBrackets: return "]";
                case Key.OemSemicolon: return ";";
                case Key.OemQuotes: return "'";
                case Key.OemComma: return ",";
                case Key.OemPeriod: return ".";
                case Key.OemQuestion: return "/";
                case Key.OemPipe: return "\\";
                case Key.OemTilde: return "`";

                default:
                    return key.ToString();
            }
        }
    }
}
