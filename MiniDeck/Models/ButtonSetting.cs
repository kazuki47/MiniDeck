using MiniDeck.Models;
using System;
using System.Xml.Serialization;

namespace MiniDeck.Models
{
    /// <summary>
    /// ボタン設定を保存するためのクラス（ActionButtonの保存可能なバージョン）
    /// </summary>
    [Serializable]
    [XmlRoot("ButtonSetting")]
    public class ButtonSetting
    {
        [XmlElement("DisplayText")]
        public string DisplayText { get; set; } = "";
        
        [XmlElement("ImagePath")]
        public string ImagePath { get; set; } = "";
        
        [XmlElement("ActionType")]
        public ActionType ActionType { get; set; } = ActionType.None;
        
        [XmlElement("ShortcutKeySequence")]
        public string ShortcutKeySequence { get; set; } = "";
        
        [XmlElement("ApplicationPath")]
        public string ApplicationPath { get; set; } = "";
        
        [XmlElement("ApplicationArguments")]
        public string ApplicationArguments { get; set; } = "";

        [XmlElement("Url")]
        public string Url { get; set; } = "";

        [XmlElement("StateDisplayType")]
        public ButtonStateDisplayType StateDisplayType { get; set; } = ButtonStateDisplayType.None;

        [XmlElement("StateActiveDisplayText")]
        public string StateActiveDisplayText { get; set; } = "";

        [XmlElement("StateActiveImagePath")]
        public string StateActiveImagePath { get; set; } = "";

        [XmlElement("StateActiveBackgroundColor")]
        public string StateActiveBackgroundColor { get; set; } = "#CC2E7D32";

        [XmlElement("StateInactiveBackgroundColor")]
        public string StateInactiveBackgroundColor { get; set; } = "#403F3F46";
        
        // パラメータなしのコンストラクタ（XMLシリアライゼーション用）
        public ButtonSetting()
        {
        }

        public ButtonSetting Clone()
        {
            return new ButtonSetting
            {
                DisplayText = DisplayText,
                ImagePath = ImagePath,
                ActionType = ActionType,
                ShortcutKeySequence = ShortcutKeySequence,
                ApplicationPath = ApplicationPath,
                ApplicationArguments = ApplicationArguments,
                Url = Url,
                StateDisplayType = StateDisplayType,
                StateActiveDisplayText = StateActiveDisplayText,
                StateActiveImagePath = StateActiveImagePath,
                StateActiveBackgroundColor = StateActiveBackgroundColor,
                StateInactiveBackgroundColor = StateInactiveBackgroundColor
            };
        }

          // ActionButtonからButtonSettingを作成するコンバータ
        public static ButtonSetting FromActionButton(ActionButton button)
        {
            if (button == null) return null;
            
            try
            {
                // 変換処理のデバッグログ
                Console.WriteLine($"ActionButton→ButtonSetting変換: テキスト={button.DisplayText}, アクションタイプ={button.ActionType}");
                
                var setting = new ButtonSetting
                {
                    DisplayText = button.DisplayText,
                    ImagePath = button.ImagePath,
                    ActionType = button.ActionType,
                    ShortcutKeySequence = button.ShortcutKeySequence,
                    ApplicationPath = button.ApplicationPath,
                    ApplicationArguments = button.ApplicationArguments,
                    Url = button.Url,
                    StateDisplayType = button.StateDisplayType,
                    StateActiveDisplayText = button.StateActiveDisplayText,
                    StateActiveImagePath = button.StateActiveImagePath,
                    StateActiveBackgroundColor = button.StateActiveBackgroundColor,
                    StateInactiveBackgroundColor = button.StateInactiveBackgroundColor
                };
                
                // アクションタイプ別のデバッグ出力
                if (button.ActionType == ActionType.LaunchApplication)
                {
                    Console.WriteLine($"  アプリケーション設定保存: パス={button.ApplicationPath}, 引数={button.ApplicationArguments}");
                    
                    // 重要：ApplicationPathがnullの場合は空文字列にする（XMLシリアライズのため）
                    if (setting.ApplicationPath == null)
                    {
                        setting.ApplicationPath = "";
                    }
                    
                    // 重要：ApplicationArgumentsがnullの場合は空文字列にする
                    if (setting.ApplicationArguments == null)
                    {
                        setting.ApplicationArguments = "";
                    }
                }
                else if (button.ActionType == ActionType.OpenUrl)
                {
                    setting.Url = setting.Url ?? "";
                    Console.WriteLine($"  URL設定保存: URL={setting.Url}");
                }
                
                return setting;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ActionButtonからButtonSettingへの変換中にエラー: {ex.Message}");
                // 最低限の情報を持つButtonSettingを返す
                return new ButtonSetting { DisplayText = button.DisplayText ?? "変換エラー" };
            }
        }// ButtonSettingからActionButtonを作成するコンバータ
        public ActionButton ToActionButton()
        {
            try
            {
                // 変換処理のデバッグログ
                Console.WriteLine($"ButtonSetting→ActionButton変換: テキスト={this.DisplayText}, アクションタイプ={this.ActionType}");
                
                // 確実にすべてのプロパティが正しく設定されるようにする
                var actionButton = new ActionButton();
                
                // 基本プロパティを設定
                actionButton.DisplayText = this.DisplayText ?? "未設定ボタン";
                actionButton.ImagePath = this.ImagePath;
                actionButton.ActionType = this.ActionType;
                actionButton.StateDisplayType = StateDisplayType;
                actionButton.StateActiveDisplayText = StateActiveDisplayText;
                actionButton.StateActiveImagePath = StateActiveImagePath;
                actionButton.StateActiveBackgroundColor = string.IsNullOrWhiteSpace(StateActiveBackgroundColor)
                    ? "#CC2E7D32"
                    : StateActiveBackgroundColor;
                actionButton.StateInactiveBackgroundColor = string.IsNullOrWhiteSpace(StateInactiveBackgroundColor)
                    ? "#403F3F46"
                    : StateInactiveBackgroundColor;
                
                // アクションタイプ別の設定
                switch (this.ActionType)
                {
                    case ActionType.KeyPress:
                        actionButton.ShortcutKeySequence = this.ShortcutKeySequence;
                        Console.WriteLine($"  キーシーケンス設定: {this.ShortcutKeySequence}");
                        break;
                        
                    case ActionType.LaunchApplication:
                        actionButton.ApplicationPath = this.ApplicationPath;
                        actionButton.ApplicationArguments = this.ApplicationArguments;
                        Console.WriteLine($"  アプリケーション設定: パス={this.ApplicationPath}, 引数={this.ApplicationArguments}");
                        break;

                    case ActionType.OpenUrl:
                        actionButton.Url = this.Url;
                        Console.WriteLine($"  URL設定: {this.Url}");
                        break;
                }
                
                return actionButton;
            }
            catch (Exception ex)
            {
                // エラーが発生した場合はデフォルトのボタンを返す
                Console.WriteLine($"ButtonSettingからActionButtonの変換中にエラー: {ex.Message}");
                Console.WriteLine($"スタックトレース: {ex.StackTrace}");
                return new ActionButton { DisplayText = "変換エラー", ActionType = ActionType.None };
            }
        }
    }
}
