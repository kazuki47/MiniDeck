using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MiniDeck.Controls
{
    /// <summary>
    /// 透明でもマウスイベントを確実にキャプチャするカスタムパネル
    /// </summary>
    public class TransparentPanel : Panel
    {
        public TransparentPanel()
        {
            // このパネルは見た目は透明だが、マウスイベントを確実にキャプチャします
            this.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
            
            // マウスイベントを処理
            this.MouseDown += TransparentPanel_MouseDown;
        }

        private void TransparentPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // マウスイベントを確実にキャプチャ
            e.Handled = true;
        }

        protected override void OnRender(DrawingContext dc)
        {
            // ヒットテスト用に最小限の背景を描画
            // 通常は完全に透明ですが、ヒットテストが機能するようにAlphaを1にします
            dc.DrawRectangle(
                new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)),
                null,
                new Rect(0, 0, this.ActualWidth, this.ActualHeight)
            );
            
            base.OnRender(dc);
        }

        // IsHitTestVisibleを常にtrueに設定
        public new bool IsHitTestVisible
        {
            get { return true; }
            set { base.IsHitTestVisible = true; }
        }
    }
}
