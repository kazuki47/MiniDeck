using System.Windows;
using MiniDeck.ViewModels;
using MiniDeck.Models;
using System.Windows.Controls;
using Microsoft.Win32;
using System;
using System.Windows.Media;

namespace MiniDeck
{
    public partial class SettingsWindow : Window
    {
        private MainViewModel _viewModel;
        private bool _isSynchronizingOpacitySliders;
          
        public SettingsWindow(MainViewModel viewModel)
        {
            try
            {
                InitializeComponent();
                _viewModel = viewModel;
                DataContext = _viewModel;

                InitializeSliderValues();
                
                // ボタンリストにデータをバインド
                ButtonListView.ItemsSource = _viewModel.Buttons;
                
                // ラジオボタンのイベントハンドラーを設定
                ColorRadioButton.Checked += BackgroundType_Changed;
                ImageRadioButton.Checked += BackgroundType_Changed;
                
                // 初期状態を設定
                if (_viewModel.UseBackgroundImage)
                {
                    ImageRadioButton.IsChecked = true;
                }
                else
                {
                    ColorRadioButton.IsChecked = true;
                }
                
                // 編集ボタンを追加
                var editButton = new Button { Content = "編集", Width = 60, Margin = new Thickness(5) };
                editButton.Click += EditButton_Click;
                
                var stackPanel = new StackPanel { 
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                stackPanel.Children.Add(editButton);
                
                // ButtonListViewの親要素をUIツリーから正しく取得
                var buttonSettingsGrid = ButtonListView.Parent as Grid;
                
                if (buttonSettingsGrid != null)
                {
                    Grid.SetRow(stackPanel, 0);
                    Grid.SetColumn(stackPanel, 1);
                    buttonSettingsGrid.Children.Add(stackPanel);
                }
                  
                // すべてのコントロールが初期化された後にプレビューを設定
                this.Loaded += (s, e) => 
                {
                    UpdateBackgroundPreview();
                    // 初期プレビューで画像があれば表示
                    if (!string.IsNullOrEmpty(_viewModel.BackgroundImagePath))
                    {
                        UpdateImagePreview(_viewModel.BackgroundImagePath);
                    }
                    
                    // デバッグ情報
                    Console.WriteLine($"SettingsWindowがロードされました。現在の透明度: {_viewModel.BackgroundOpacity:F2}");
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"設定ウィンドウの初期化でエラーが発生しました:\n{ex.Message}\n\nスタックトレース:\n{ex.StackTrace}", 
                    "初期化エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeSliderValues()
        {
            _isSynchronizingOpacitySliders = true;
            try
            {
                RowsSlider.Value = _viewModel.ButtonRows;
                ColumnsSlider.Value = _viewModel.ButtonColumns;
                OpacitySlider.Value = _viewModel.BackgroundOpacity;
                BackgroundOpacitySlider.Value = _viewModel.BackgroundOpacity;
                ButtonOpacitySlider.Value = _viewModel.ButtonOpacity;
            }
            finally
            {
                _isSynchronizingOpacitySliders = false;
            }

            UpdateLayoutChangeHint();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            // 選択されたボタンを編集
            var selectedButton = ButtonListView.SelectedItem as ActionButton;
            if (selectedButton == null) 
            {
                MessageBox.Show("編集するボタンを選択してください", "選択エラー", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            var buttonSettingsWindow = new ButtonSettingsWindow(selectedButton);
            buttonSettingsWindow.Owner = this;
            var result = buttonSettingsWindow.ShowDialog();
            
            if (result == true)
            {
                // ボタン設定が変更された場合、設定を保存
                if (_viewModel != null)
                {
                    _viewModel.SaveSettings();
                }
            }
        }
        
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryApplyPendingSliderSettings())
                {
                    return;
                }

                // 背景設定をメインウィンドウに適用
                ApplyBackgroundSettingsToMainWindow();
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OK_Click中にエラーが発生しました: {ex.Message}");
                MessageBox.Show($"設定の保存中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // スライダーの値はViewModelへ未反映なので、そのまま破棄する
            DialogResult = false;
            Close();
        }
        
        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryApplyPendingSliderSettings())
                {
                    return;
                }

                // 背景設定をメインウィンドウに適用
                ApplyBackgroundSettingsToMainWindow();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Apply_Click中にエラーが発生しました: {ex.Message}");
            }
        }
        
        private void BackgroundType_Changed(object sender, RoutedEventArgs e)
        {
            // ViewModelが初期化されているかチェック
            if (_viewModel == null)
                return;
                
            // ViewModelの状態を更新
            _viewModel.UseBackgroundImage = ImageRadioButton?.IsChecked == true;
            
            // プレビューを更新
            UpdateBackgroundPreview();
        }
        
        private void UpdateBackgroundPreview()
        {
            // XAMLコントロールが初期化されているかチェック
            if (ColorPreviewRect == null || ImagePreviewImg == null)
                return;
                
            if (ColorRadioButton?.IsChecked == true)
            {
                // 背景色を表示
                ColorPreviewRect.Visibility = Visibility.Visible;
                ImagePreviewImg.Visibility = Visibility.Collapsed;
                
                // 透明度を適用
                ColorPreviewRect.Opacity = GetPendingBackgroundOpacity();
            }
            else if (ImageRadioButton?.IsChecked == true)
            {
                // 背景画像を表示
                ColorPreviewRect.Visibility = Visibility.Collapsed;
                ImagePreviewImg.Visibility = Visibility.Visible;
                
                // 画像を読み込んでプレビューに設定
                UpdateImagePreview(_viewModel.BackgroundImagePath);
                
                // 透明度を適用
                ImagePreviewImg.Opacity = GetPendingBackgroundOpacity();
            }
        }

        private void LayoutSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateLayoutChangeHint();
        }

        private void LayoutOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isSynchronizingOpacitySliders)
            {
                return;
            }

            _isSynchronizingOpacitySliders = true;
            try
            {
                if (BackgroundOpacitySlider != null)
                {
                    BackgroundOpacitySlider.Value = e.NewValue;
                }
            }
            finally
            {
                _isSynchronizingOpacitySliders = false;
            }

            UpdateBackgroundPreview();
        }
        
        private void BackgroundOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isSynchronizingOpacitySliders)
            {
                return;
            }

            _isSynchronizingOpacitySliders = true;
            try
            {
                if (OpacitySlider != null)
                {
                    OpacitySlider.Value = e.NewValue;
                }
            }
            finally
            {
                _isSynchronizingOpacitySliders = false;
            }

            UpdateBackgroundPreview();
        }

        private double GetPendingBackgroundOpacity()
        {
            return BackgroundOpacitySlider?.Value ?? _viewModel?.BackgroundOpacity ?? 0.0;
        }

        private void UpdateLayoutChangeHint()
        {
            if (LayoutChangeHint == null || RowsSlider == null || ColumnsSlider == null || _viewModel == null)
            {
                return;
            }

            int rows = (int)Math.Round(RowsSlider.Value);
            int columns = (int)Math.Round(ColumnsSlider.Value);
            int newButtonCount = rows * columns;
            int currentButtonCount = _viewModel.Buttons?.Count ?? 0;

            if (newButtonCount < currentButtonCount)
            {
                int removedButtonCount = currentButtonCount - newButtonCount;
                LayoutChangeHint.Text = $"合計 {newButtonCount} ボタン。適用すると末尾の {removedButtonCount} ボタン設定が削除されます。";
            }
            else if (newButtonCount > currentButtonCount)
            {
                int addedButtonCount = newButtonCount - currentButtonCount;
                LayoutChangeHint.Text = $"合計 {newButtonCount} ボタン。適用すると空のボタンが {addedButtonCount} 個追加されます。";
            }
            else
            {
                LayoutChangeHint.Text = $"合計 {newButtonCount} ボタン。現在のボタン数と同じです。";
            }
        }

        private bool TryApplyPendingSliderSettings()
        {
            if (_viewModel == null)
            {
                return false;
            }

            int rows = (int)Math.Round(RowsSlider.Value);
            int columns = (int)Math.Round(ColumnsSlider.Value);
            int newButtonCount = rows * columns;
            int currentButtonCount = _viewModel.Buttons?.Count ?? 0;

            if (newButtonCount < currentButtonCount)
            {
                int removedButtonCount = currentButtonCount - newButtonCount;
                MessageBoxResult result = MessageBox.Show(
                    $"末尾の {removedButtonCount} ボタン設定が削除されます。続行しますか？",
                    "ボタン数の確認",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    return false;
                }
            }

            _viewModel.ApplyLayoutSettings(
                rows,
                columns,
                OpacitySlider.Value,
                ButtonOpacitySlider.Value);

            if (Owner is MainWindow mainWindow)
            {
                mainWindow.ApplyLayoutFromSettings();
            }

            UpdateLayoutChangeHint();
            return true;
        }

        // 画像プレビュー更新メソッド
        private void UpdateImagePreview(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath))
                {
                    ImagePreviewImg.Source = null;
                    return;
                }

                string fullPath;
                if (imagePath.StartsWith("/"))
                {
                    // アプリケーションリソースを処理
                    string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    string baseDir = System.IO.Path.GetDirectoryName(appPath);
                    fullPath = baseDir + imagePath.Replace('/', '\\');
                }
                else
                {
                    fullPath = imagePath;
                }

                if (System.IO.File.Exists(fullPath))
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(fullPath);
                    bitmap.EndInit();
                    ImagePreviewImg.Source = bitmap;
                }
                else
                {
                    ImagePreviewImg.Source = null;
                }
            }
            catch
            {
                ImagePreviewImg.Source = null;
            }
        }
        
        private void SelectColor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // XAMLコントロールが初期化されているかチェック
                if (ColorPreview == null || ColorRadioButton == null)
                    return;
                    
                // カラーピッカーダイアログを表示
                Color currentColor = (Color)ColorConverter.ConvertFromString(_viewModel.BackgroundColor);
                var colorPicker = new ColorPickerDialog(currentColor);
                colorPicker.Owner = this;
                colorPicker.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                
                bool? result = colorPicker.ShowDialog();
                
                if (result == true)
                {
                    // 選択した色を設定
                    Color selectedColor = colorPicker.SelectedColor;
                    _viewModel.BackgroundColor = selectedColor.ToString();
                    ColorPreview.Background = new SolidColorBrush(selectedColor);
                    
                    // 背景色ラジオボタンを選択してプレビューを更新
                    ColorRadioButton.IsChecked = true;
                    UpdateBackgroundPreview();
                }
                
                // ダイアログが確実に閉じられるように
                colorPicker = null;
                
                // フォーカスを親ウィンドウに戻す
                this.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"色選択中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
          
        private void SelectBackgroundImage_Click(object sender, RoutedEventArgs e)
        {
            // 画像ファイル選択ダイアログを表示
            var dialog = new OpenFileDialog
            {
                Filter = "画像ファイル (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|すべてのファイル (*.*)|*.*",
                Title = "背景画像を選択"
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
                        "Resources", "Backgrounds");
                    
                    // リソースディレクトリが存在しない場合は作成
                    if (!System.IO.Directory.Exists(resourceDir))
                    {
                        System.IO.Directory.CreateDirectory(resourceDir);
                    }
                    
                    // ファイルをコピー
                    string destPath = System.IO.Path.Combine(resourceDir, fileName);
                    System.IO.File.Copy(selectedFilePath, destPath, true);
                    
                    // 相対パスとしてセット
                    _viewModel.BackgroundImagePath = $"/Resources/Backgrounds/{fileName}";
                    BackgroundImagePath.Text = _viewModel.BackgroundImagePath;
                      
                    // 背景画像ラジオボタンを選択してプレビューを更新
                    ImageRadioButton.IsChecked = true;
                    UpdateBackgroundPreview();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"画像の追加中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    // エラー時は絶対パスをセット
                    _viewModel.BackgroundImagePath = dialog.FileName;
                    BackgroundImagePath.Text = _viewModel.BackgroundImagePath;
                    
                    // 背景画像ラジオボタンを選択してプレビューを更新
                    ImageRadioButton.IsChecked = true;
                    UpdateBackgroundPreview();
                }
            }
        }
          
        private void ClearBackgroundImage_Click(object sender, RoutedEventArgs e)
        {
            // 背景画像をクリア
            _viewModel.BackgroundImagePath = "";
            BackgroundImagePath.Text = "";
            
            // プレビューも更新
            UpdateBackgroundPreview();
        }
          
        private void ApplyBackgroundSettingsToMainWindow()
        {
            var mainWindow = Owner as MainWindow;
            if (mainWindow == null) return;
            
            try
            {
                // 明示的に透明度を更新
                if (_viewModel != null)
                {
                    double opacity = _viewModel.BackgroundOpacity;
                    Console.WriteLine($"メインウィンドウに適用する透明度: {opacity:F2}");
                }
                
                if (ColorRadioButton?.IsChecked == true)
                {                    
                    // 背景色を適用
                    Color bgColor = (Color)ColorConverter.ConvertFromString(_viewModel.BackgroundColor);
                    SolidColorBrush brush = new SolidColorBrush(bgColor);
                    brush.Opacity = _viewModel.BackgroundOpacity;
                    mainWindow.Background = brush;
                    
                    Console.WriteLine($"背景色を適用しました。色: {_viewModel.BackgroundColor}, 透明度: {_viewModel.BackgroundOpacity:F2}");
                }
                else if (ImageRadioButton?.IsChecked == true && !string.IsNullOrEmpty(_viewModel.BackgroundImagePath))
                {
                    // 背景画像を適用
                    try
                    {
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
                            ImageBrush imageBrush = new ImageBrush
                            {
                                ImageSource = new System.Windows.Media.Imaging.BitmapImage(new Uri(fullPath)),
                                Opacity = _viewModel.BackgroundOpacity,
                                Stretch = Stretch.UniformToFill
                            };
                            
                            mainWindow.Background = imageBrush;
                            Console.WriteLine($"背景画像を適用しました。パス: {fullPath}, 透明度: {_viewModel.BackgroundOpacity:F2}");
                        }
                        else
                        {
                            MessageBox.Show("指定された画像ファイルが見つかりません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"背景画像の適用に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {                    
                    // デフォルトの背景色を適用
                    Color bgColor = (Color)ColorConverter.ConvertFromString(_viewModel.BackgroundColor);
                    SolidColorBrush brush = new SolidColorBrush(bgColor);
                    brush.Opacity = _viewModel.BackgroundOpacity;
                    mainWindow.Background = brush;
                    Console.WriteLine($"デフォルト背景色を適用しました。色: {_viewModel.BackgroundColor}, 透明度: {_viewModel.BackgroundOpacity:F2}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"背景設定の適用に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
