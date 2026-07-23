using Microsoft.Win32;
using MiniDeck.Models;
using MiniDeck.Services;
using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MiniDeck
{
    public partial class ButtonSettingsWindow : Window
    {
        public ActionButton Button { get; private set; }
        private bool _isCapturingKey = false;
        private readonly ActionService _actionService = new ActionService();

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
            UrlBox.Text = button.Url;
            StateActiveTextBox.Text = button.StateActiveDisplayText;
            StateActiveImagePathBox.Text = button.StateActiveImagePath;
            StateActiveColorBox.Text = string.IsNullOrWhiteSpace(button.StateActiveBackgroundColor)
                ? "#CC2E7D32"
                : button.StateActiveBackgroundColor;
            StateInactiveColorBox.Text = string.IsNullOrWhiteSpace(button.StateInactiveBackgroundColor)
                ? "#403F3F46"
                : button.StateInactiveBackgroundColor;
            
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
                case ActionType.OpenUrl:
                    ActionTypeCombo.SelectedIndex = 3;
                    UrlPanel.Visibility = Visibility.Visible;
                    break;
                default:
                    ActionTypeCombo.SelectedIndex = 0;
                    break;
            }
            SelectStateDisplayType(button.StateDisplayType);
            UpdateStateColorPreviews();
            UpdateStateImagePreview(button.StateActiveImagePath);
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

            UrlPanel.Visibility = (actionType == ActionType.OpenUrl) ?
                Visibility.Visible : Visibility.Collapsed;
                
            // パネルの表示状態が変わったら、ウィンドウサイズを調整
            AdjustWindowSize();
        }

        private void StateTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StateOptionsPanel == null || StateTypeHint == null)
            {
                return;
            }

            ButtonStateDisplayType stateType = GetSelectedStateDisplayType();
            StateOptionsPanel.Visibility = stateType == ButtonStateDisplayType.None
                ? Visibility.Collapsed
                : Visibility.Visible;
            switch (stateType)
            {
                case ButtonStateDisplayType.ApplicationRunning:
                    StateTypeHint.Text = "アプリ・ファイルを開くアクションの対象パスを監視し、起動中を状態ONとして表示します。";
                    break;
                case ButtonStateDisplayType.MicrophoneMuted:
                    StateTypeHint.Text = "Windowsの既定マイクがミュート中のとき、状態ONとして表示します。";
                    break;
                case ButtonStateDisplayType.SystemAudioMuted:
                    StateTypeHint.Text = "Windowsの既定出力デバイスがミュート中のとき、状態ONとして表示します。";
                    break;
                default:
                    StateTypeHint.Text = "状態表示を使用しません。従来どおり通常の表示名と画像を表示します。";
                    break;
            }

            AdjustWindowSize();
        }

        private void SelectStateDisplayType(ButtonStateDisplayType stateType)
        {
            foreach (ComboBoxItem item in StateTypeCombo.Items)
            {
                if (item.Tag is ButtonStateDisplayType itemType && itemType == stateType)
                {
                    StateTypeCombo.SelectedItem = item;
                    return;
                }
            }

            StateTypeCombo.SelectedIndex = 0;
        }

        private ButtonStateDisplayType GetSelectedStateDisplayType()
        {
            return (StateTypeCombo.SelectedItem as ComboBoxItem)?.Tag is ButtonStateDisplayType stateType
                ? stateType
                : ButtonStateDisplayType.None;
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
                Filter = "すべてのファイル (*.*)|*.*|実行ファイル (*.exe)|*.exe|ショートカット (*.lnk)|*.lnk",
                Title = "開くアプリまたはファイルを選択"
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
                    // ビルド出力ではなく、再ビルド後も残るユーザーデータ領域へ保存する
                    ImagePathBox.Text = ImageStorageService.ImportImage(
                        dialog.FileName,
                        ImageStorageKind.Button);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"画像の追加中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BrowseStateActiveImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "画像ファイル (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp|すべてのファイル (*.*)|*.*",
                Title = "状態ONで表示する画像を選択"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    StateActiveImagePathBox.Text = ImageStorageService.ImportImage(
                        dialog.FileName,
                        ImageStorageKind.Button);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"画像の追加中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SelectStateActiveColor_Click(object sender, RoutedEventArgs e)
        {
            SelectStateColor(StateActiveColorBox, "#CC2E7D32");
        }

        private void SelectStateInactiveColor_Click(object sender, RoutedEventArgs e)
        {
            SelectStateColor(StateInactiveColorBox, "#403F3F46");
        }

        private void SelectStateColor(TextBox target, string fallbackColor)
        {
            Color initialColor = TryParseColor(target.Text, out Color parsedColor)
                ? parsedColor
                : (Color)ColorConverter.ConvertFromString(fallbackColor);
            var dialog = new ColorPickerDialog(initialColor) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                target.Text = dialog.SelectedColor.ToString();
            }
        }

        private void StateColorBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateStateColorPreviews();
        }

        private void UpdateStateColorPreviews()
        {
            if (StateActiveColorPreview != null)
            {
                StateActiveColorPreview.Background = TryParseColor(StateActiveColorBox?.Text, out Color activeColor)
                    ? new SolidColorBrush(activeColor)
                    : Brushes.Transparent;
            }

            if (StateInactiveColorPreview != null)
            {
                StateInactiveColorPreview.Background = TryParseColor(StateInactiveColorBox?.Text, out Color inactiveColor)
                    ? new SolidColorBrush(inactiveColor)
                    : Brushes.Transparent;
            }
        }

        private void StateActiveImagePathBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateStateImagePreview(StateActiveImagePathBox.Text);
        }

        private void UpdateStateImagePreview(string imagePath)
        {
            LoadImagePreview(StateActiveImagePreview, imagePath);
        }

        private static bool TryParseColor(string value, out Color color)
        {
            try
            {
                color = (Color)ColorConverter.ConvertFromString(value?.Trim());
                return true;
            }
            catch
            {
                color = Colors.Transparent;
                return false;
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = ActionTypeCombo.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;

            var selectedActionType = (ActionType)selectedItem.Tag;
            ButtonStateDisplayType stateDisplayType = GetSelectedStateDisplayType();
            if (stateDisplayType == ButtonStateDisplayType.ApplicationRunning &&
                (selectedActionType != ActionType.LaunchApplication || string.IsNullOrWhiteSpace(ApplicationPathBox.Text)))
            {
                MessageBox.Show(
                    "アプリの起動状態を表示するには、アクション種別を「アプリ・ファイルを開く」にして対象パスを指定してください。",
                    "状態表示を設定できません",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (stateDisplayType == ButtonStateDisplayType.ApplicationRunning)
            {
                string extension = System.IO.Path.GetExtension(ApplicationPathBox.Text.Trim().Trim('"'));
                if (!string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(extension, ".lnk", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(
                        "アプリの起動状態は、.exeファイルまたは.exeへのショートカット（.lnk）で利用できます。",
                        "監視対象を確認してください",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }
            }

            if (stateDisplayType != ButtonStateDisplayType.None &&
                (!TryParseColor(StateActiveColorBox.Text, out Color _) ||
                 !TryParseColor(StateInactiveColorBox.Text, out Color _)))
            {
                MessageBox.Show(
                    "状態ONと状態OFFの色を #AARRGGBB または #RRGGBB 形式で指定してください。",
                    "状態表示の色を確認してください",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            string url = UrlBox.Text?.Trim() ?? "";
            if (selectedActionType == ActionType.OpenUrl)
            {
                if (!TryGetValidatedUrl(out Uri validatedUri))
                {
                    return;
                }

                url = validatedUri.AbsoluteUri;
            }

            try
            {
                // 呼び出し元から渡された編集用コピーだけを更新する。
                // 永続化とメイン画面への反映は設定画面の「適用」「OK」が担当する。
                Button.DisplayText = DisplayTextBox.Text ?? "";
                Button.ImagePath = ImagePathBox.Text ?? "";
                Button.ActionType = selectedActionType;
                Button.ShortcutKeySequence = ShortcutKeyBox.Text ?? "";
                Button.ApplicationPath = ApplicationPathBox.Text ?? "";
                Button.ApplicationArguments = ArgumentsBox.Text ?? "";
                Button.Url = url;
                Button.StateDisplayType = stateDisplayType;
                Button.StateActiveDisplayText = StateActiveTextBox.Text ?? "";
                Button.StateActiveImagePath = StateActiveImagePathBox.Text ?? "";
                Button.StateActiveBackgroundColor = StateActiveColorBox.Text?.Trim() ?? "#CC2E7D32";
                Button.StateInactiveBackgroundColor = StateInactiveColorBox.Text?.Trim() ?? "#403F3F46";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ボタン設定の更新中にエラーが発生: {ex.Message}");
                Console.WriteLine($"スタックトレース: {ex.StackTrace}");
                MessageBox.Show($"設定の更新中にエラーが発生しました: {ex.Message}", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
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

        private void UrlBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (UrlValidationText == null)
            {
                return;
            }

            string value = UrlBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                SetUrlValidationMessage("※ http:// または https:// で始まるURLを入力してください", false);
                return;
            }

            if (ActionService.TryCreateWebUri(value, out _, out string errorMessage))
            {
                SetUrlValidationMessage("入力されたURLを開くことができます。", false);
            }
            else
            {
                SetUrlValidationMessage(errorMessage, true);
            }
        }

        private void TestUrl_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetValidatedUrl(out Uri uri))
            {
                return;
            }

            UrlBox.Text = uri.AbsoluteUri;
            _actionService.OpenUrl(uri.AbsoluteUri);
        }

        private bool TryGetValidatedUrl(out Uri uri)
        {
            if (ActionService.TryCreateWebUri(UrlBox.Text, out uri, out string errorMessage))
            {
                SetUrlValidationMessage("入力されたURLを開くことができます。", false);
                return true;
            }

            SetUrlValidationMessage(errorMessage, true);
            UrlBox.Focus();
            UrlBox.SelectAll();
            return false;
        }

        private void SetUrlValidationMessage(string message, bool isError)
        {
            UrlValidationText.Text = message;
            UrlValidationText.Foreground = isError ? Brushes.LightCoral : Brushes.White;
        }
        
        // 画像プレビュー更新メソッド
        private void UpdateImagePreview(string imagePath)
        {
            LoadImagePreview(ImagePreview, imagePath);
        }

        private void LoadImagePreview(Image target, string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath))
                {
                    target.Source = null;
                    return;
                }

                if (ImageStorageService.TryResolveImagePath(imagePath, out string fullPath))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(fullPath);
                    bitmap.EndInit();
                    target.Source = bitmap;
                }
                else
                {
                    target.Source = null;
                }
            }
            catch
            {
                target.Source = null;
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
