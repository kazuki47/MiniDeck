using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MiniDeck
{
    public partial class ColorPickerDialog : Window
    {
        private bool _isDraggingColorWheel = false;
        private bool _isDraggingBrightness = false;
        private WriteableBitmap _colorWheelBitmap;
        private double _hue = 0; // 0-359
        private double _saturation = 1; // 0-1
        private double _value = 1; // 0-1
        private byte _alpha = 255; // 0-255

        public Color SelectedColor { get; private set; }
          public ColorPickerDialog(Color initialColor)
        {
            InitializeComponent();
            
            // 初期色を設定
            SelectedColor = initialColor;
            RgbToHsv(initialColor.R, initialColor.G, initialColor.B, out _hue, out _saturation, out _value);
            _alpha = initialColor.A;
            AlphaSlider.Value = _alpha;
            
            // カラーホイールを作成
            CreateColorWheel();
            UpdateBrightnessGradient();
            UpdateUI();
            
            Loaded += ColorPickerDialog_Loaded;
            Closing += ColorPickerDialog_Closing;
        }

        private void ColorPickerDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // ウィンドウが閉じられる際のクリーンアップ
            ReleaseAllMouseCaptures();
        }

        private void ColorPickerDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // 初期位置を設定
            Point wheelPoint = HsvToWheelPosition(_hue, _saturation);
            Canvas.SetLeft(SelectorPoint, wheelPoint.X - SelectorPoint.Width / 2);
            Canvas.SetTop(SelectorPoint, wheelPoint.Y - SelectorPoint.Height / 2);
            
            Canvas.SetTop(BrightnessSelector, (1 - _value) * BrightnessCanvas.Height - BrightnessSelector.Height / 2);
        }
          private void CreateColorWheel()
        {
            int size = 280;
            int radius = size / 2;
            int centerX = radius;
            int centerY = radius;
            
            _colorWheelBitmap = new WriteableBitmap(size, size, 96, 96, PixelFormats.Bgra32, null);
            
            int stride = _colorWheelBitmap.BackBufferStride;
            byte[] pixels = new byte[stride * size];
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = x - centerX;
                    int dy = y - centerY;
                    double distance = Math.Sqrt(dx * dx + dy * dy);
                    
                    if (distance <= radius)
                    {
                        double hue = (Math.Atan2(dy, dx) * 180 / Math.PI + 180) % 360; // 0-359
                        double saturation = Math.Min(distance / radius, 1); // 0-1
                        
                        Color color = HsvToRgb(hue, saturation, 1.0);
                        
                        int index = y * stride + x * 4;
                        pixels[index] = color.B;
                        pixels[index + 1] = color.G;
                        pixels[index + 2] = color.R;
                        pixels[index + 3] = 255; // alpha
                    }
                }
            }
            
            _colorWheelBitmap.WritePixels(new Int32Rect(0, 0, size, size), pixels, stride, 0);
            
            ImageBrush brush = new ImageBrush(_colorWheelBitmap);
            ColorWheelEllipse.Fill = brush;
        }
        
        private void UpdateBrightnessGradient()
        {
            Color colorTop = HsvToRgb(_hue, _saturation, 1.0);
            Color colorBottom = HsvToRgb(_hue, _saturation, 0.0);
            
            LinearGradientBrush brush = new LinearGradientBrush(colorTop, colorBottom, new Point(0, 0), new Point(0, 1));
            BrightnessGradient.Fill = brush;
        }
        
        private void UpdateUI()
        {
            // 現在選択されている色をHSVからRGBに変換
            Color color = HsvToRgb(_hue, _saturation, _value);
            color.A = _alpha;
            SelectedColor = color;
            
            // プレビューを更新
            CurrentColorPreview.Background = new SolidColorBrush(color);
            
            // HTMLカラーコードを更新
            HtmlColorCode.Text = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
            
            // 明るさスライダーのグラデーションを更新
            UpdateBrightnessGradient();
        }
        
        private Point HsvToWheelPosition(double hue, double saturation)
        {
            double radius = ColorWheelEllipse.Width / 2;
            double angle = hue * Math.PI / 180;
            
            double x = radius + saturation * radius * Math.Cos(angle);
            double y = radius + saturation * radius * Math.Sin(angle);
            
            // キャンバス上のオフセットを考慮
            x += Canvas.GetLeft(ColorWheelEllipse);
            y += Canvas.GetTop(ColorWheelEllipse);
            
            return new Point(x, y);
        }
        
        private void RgbToHsv(byte r, byte g, byte b, out double h, out double s, out double v)
        {
            double red = r / 255.0;
            double green = g / 255.0;
            double blue = b / 255.0;
            
            double max = Math.Max(red, Math.Max(green, blue));
            double min = Math.Min(red, Math.Min(green, blue));
            double delta = max - min;
            
            // 明度
            v = max;
            
            // 彩度
            if (max == 0)
                s = 0;
            else
                s = delta / max;
            
            // 色相
            h = 0;
            
            if (delta != 0)
            {
                if (max == red)
                    h = 60 * ((green - blue) / delta % 6);
                else if (max == green)
                    h = 60 * ((blue - red) / delta + 2);
                else
                    h = 60 * ((red - green) / delta + 4);
            }
            
            if (h < 0)
                h += 360;
        }
        
        private Color HsvToRgb(double h, double s, double v)
        {
            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            double m = v - c;
            
            double red = 0, green = 0, blue = 0;
            
            if (0 <= h && h < 60)
            {
                red = c; green = x; blue = 0;
            }
            else if (60 <= h && h < 120)
            {
                red = x; green = c; blue = 0;
            }
            else if (120 <= h && h < 180)
            {
                red = 0; green = c; blue = x;
            }
            else if (180 <= h && h < 240)
            {
                red = 0; green = x; blue = c;
            }
            else if (240 <= h && h < 300)
            {
                red = x; green = 0; blue = c;
            }
            else
            {
                red = c; green = 0; blue = x;
            }
            
            byte r = (byte)((red + m) * 255);
            byte g = (byte)((green + m) * 255);
            byte b = (byte)((blue + m) * 255);
            
            return Color.FromRgb(r, g, b);
        }
          private void ColorWheel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _isDraggingColorWheel = true;
                UpdateColorFromMousePosition(e.GetPosition(ColorWheel));
                ((UIElement)sender).CaptureMouse();
                e.Handled = true;
            }
        }
        
        private void ColorWheel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingColorWheel && e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateColorFromMousePosition(e.GetPosition(ColorWheel));
                e.Handled = true;
            }
        }
        
        private void ColorWheel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && _isDraggingColorWheel)
            {
                _isDraggingColorWheel = false;
                ((UIElement)sender).ReleaseMouseCapture();
                e.Handled = true;
            }
        }
          private void UpdateColorFromMousePosition(Point position)
        {
            try
            {
                double centerX = ColorWheelEllipse.Width / 2 + Canvas.GetLeft(ColorWheelEllipse);
                double centerY = ColorWheelEllipse.Height / 2 + Canvas.GetTop(ColorWheelEllipse);
                
                double dx = position.X - centerX;
                double dy = position.Y - centerY;
                double radius = ColorWheelEllipse.Width / 2;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                
                if (distance > radius)
                {
                    // カラーホイールの外をクリックした場合、円周上に制限
                    double angle = Math.Atan2(dy, dx);
                    dx = radius * Math.Cos(angle);
                    dy = radius * Math.Sin(angle);
                    distance = radius;
                    position.X = centerX + dx;
                    position.Y = centerY + dy;
                }
                
                // セレクタの位置を更新
                Canvas.SetLeft(SelectorPoint, position.X - SelectorPoint.Width / 2);
                Canvas.SetTop(SelectorPoint, position.Y - SelectorPoint.Height / 2);
                
                // HSVの値を更新
                _hue = (Math.Atan2(dy, dx) * 180 / Math.PI + 180) % 360;
                _saturation = Math.Min(distance / radius, 1);                UpdateUI();
            }
            catch
            {
                // エラーが発生した場合はマウスキャプチャをリリース
                _isDraggingColorWheel = false;
                if (ColorWheel.IsMouseCaptured)
                    ColorWheel.ReleaseMouseCapture();
            }
        }
          private void BrightnessCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _isDraggingBrightness = true;
                UpdateBrightnessFromMousePosition(e.GetPosition(BrightnessCanvas));
                ((UIElement)sender).CaptureMouse();
                e.Handled = true;
            }
        }
        
        private void BrightnessCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingBrightness && e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateBrightnessFromMousePosition(e.GetPosition(BrightnessCanvas));
                e.Handled = true;
            }
        }
        
        private void BrightnessCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && _isDraggingBrightness)
            {
                _isDraggingBrightness = false;
                ((UIElement)sender).ReleaseMouseCapture();
                e.Handled = true;
            }
        }
          private void UpdateBrightnessFromMousePosition(Point position)
        {
            try
            {
                // 位置を制限
                double y = Math.Max(0, Math.Min(position.Y, BrightnessCanvas.Height));
                
                // セレクタの位置を更新
                Canvas.SetTop(BrightnessSelector, y - BrightnessSelector.Height / 2);
                
                // 明度を更新 (上が1、下が0)
                _value = 1.0 - (y / BrightnessCanvas.Height);                UpdateUI();
            }
            catch
            {
                // エラーが発生した場合はマウスキャプチャをリリース
                _isDraggingBrightness = false;
                if (BrightnessCanvas.IsMouseCaptured)
                    BrightnessCanvas.ReleaseMouseCapture();
            }
        }
          private void HtmlColorCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                // プログラムによる変更の場合は処理をスキップ
                if (!HtmlColorCode.IsFocused) return;
                
                // #AARRGGBB または #RRGGBB 形式のHTMLカラーコードを解析
                string colorCode = HtmlColorCode.Text.Trim();
                
                if (colorCode.StartsWith("#") && (colorCode.Length == 7 || colorCode.Length == 9))
                {
                    Color color;
                    
                    if (colorCode.Length == 9) // #AARRGGBB
                    {
                        byte a = Convert.ToByte(colorCode.Substring(1, 2), 16);
                        byte r = Convert.ToByte(colorCode.Substring(3, 2), 16);
                        byte g = Convert.ToByte(colorCode.Substring(5, 2), 16);
                        byte b = Convert.ToByte(colorCode.Substring(7, 2), 16);
                        color = Color.FromArgb(a, r, g, b);
                        _alpha = a;
                        AlphaSlider.Value = a;
                    }
                    else // #RRGGBB
                    {
                        byte r = Convert.ToByte(colorCode.Substring(1, 2), 16);
                        byte g = Convert.ToByte(colorCode.Substring(3, 2), 16);
                        byte b = Convert.ToByte(colorCode.Substring(5, 2), 16);
                        color = Color.FromRgb(r, g, b);
                    }
                    
                    RgbToHsv(color.R, color.G, color.B, out _hue, out _saturation, out _value);
                    
                    // UI更新
                    Point wheelPoint = HsvToWheelPosition(_hue, _saturation);
                    Canvas.SetLeft(SelectorPoint, wheelPoint.X - SelectorPoint.Width / 2);
                    Canvas.SetTop(SelectorPoint, wheelPoint.Y - SelectorPoint.Height / 2);
                    
                    Canvas.SetTop(BrightnessSelector, (1 - _value) * BrightnessCanvas.Height - BrightnessSelector.Height / 2);
                    
                    UpdateUI();
                }
            }
            catch
            {
                // パースに失敗した場合は何もしない
            }
        }
        
        private void AlphaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _alpha = (byte)AlphaSlider.Value;
            UpdateUI();
        }
          private void OK_Click(object sender, RoutedEventArgs e)
        {
            // マウスキャプチャを確実にリリース
            ReleaseAllMouseCaptures();
            DialogResult = true;
            Close();
        }
        
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // マウスキャプチャを確実にリリース
            ReleaseAllMouseCaptures();
            DialogResult = false;
            Close();
        }
        
        private void ReleaseAllMouseCaptures()
        {
            // すべてのマウスキャプチャをリリース
            _isDraggingColorWheel = false;
            _isDraggingBrightness = false;
            
            // 各コントロールのマウスキャプチャをリリース
            if (ColorWheel.IsMouseCaptured)
                ColorWheel.ReleaseMouseCapture();
            if (BrightnessCanvas.IsMouseCaptured)
                BrightnessCanvas.ReleaseMouseCapture();
            if (IsMouseCaptured)
                ReleaseMouseCapture();
        }
    }
}
