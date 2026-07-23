using MiniDeck.Models;

namespace MiniDeck.Services
{
    /// <summary>
    /// 設定画面で使用するMiniDeck内クリップボード。
    /// OSのクリップボードを変更せず、コピー元とは独立した複製を返す。
    /// </summary>
    public sealed class EditorClipboardService
    {
        private ButtonSetting _button;
        private PageSetting _page;

        public bool HasButton => _button != null;
        public bool HasPage => _page != null;

        public bool CopyButton(ActionButton button)
        {
            ButtonSetting setting = ButtonSetting.FromActionButton(button);
            if (setting == null)
            {
                return false;
            }

            _button = setting.Clone();
            _page = null;
            return true;
        }

        public ActionButton GetButtonCopy()
        {
            return _button?.Clone().ToActionButton();
        }

        public bool CopyPage(PageSetting page)
        {
            if (page == null)
            {
                return false;
            }

            _page = page.Clone();
            _button = null;
            return true;
        }

        public PageSetting GetPageCopy()
        {
            return _page?.Clone();
        }
    }
}
