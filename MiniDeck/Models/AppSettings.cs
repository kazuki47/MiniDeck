using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace MiniDeck.Models
{    /// <summary>
    /// アプリケーションの設定を保存するためのクラス
    /// </summary>
    [Serializable]
    [XmlRoot("AppSettings")]
    public class AppSettings
    {
        public const int CurrentSettingsVersion = 4;

        [XmlElement("SettingsVersion")]
        public int SettingsVersion { get; set; }

        // 基本設定
        [XmlElement("ButtonRows")]
        public int ButtonRows { get; set; } = 2;
        
        [XmlElement("ButtonColumns")]
        public int ButtonColumns { get; set; } = 4;
        
        // 背景設定
        [XmlElement("BackgroundColor")]
        public string BackgroundColor { get; set; } = "#FFFFFFFF";
        
        [XmlElement("BackgroundImagePath")]
        public string BackgroundImagePath { get; set; } = "";
        
        [XmlElement("BackgroundOpacity")]
        public double BackgroundOpacity { get; set; } = 1.0;
        
        [XmlElement("ButtonOpacity")]
        public double ButtonOpacity { get; set; } = 0.6;
        
        [XmlElement("UseBackgroundImage")]
        public bool UseBackgroundImage { get; set; } = false;
        
        // 一般設定
        [XmlElement("AlwaysOnTop")]
        public bool AlwaysOnTop { get; set; } = true;
        
        [XmlElement("AutoStart")]
        public bool AutoStart { get; set; } = false;

        // ページ設定
        [XmlElement("ActivePageId")]
        public string ActivePageId { get; set; } = "";

        [XmlArray("Pages")]
        [XmlArrayItem("Page")]
        public List<PageSetting> Pages { get; set; } = new List<PageSetting>();
        
        // バージョン1以前の設定を読み込むために残す旧ボタン設定
        [XmlArray("Buttons")]
        [XmlArrayItem("ButtonSetting")]
        public List<ButtonSetting> Buttons { get; set; } = new List<ButtonSetting>();

        [XmlIgnore]
        public bool IsReadOnly { get; set; }

        [XmlIgnore]
        public string LoadWarning { get; set; } = "";

        public bool ShouldSerializeButtons()
        {
            return (Pages == null || Pages.Count == 0) && Buttons != null && Buttons.Count > 0;
        }
        
        // パラメータなしのコンストラクタ（XMLシリアライゼーション用）
        public AppSettings()
        {
        }}
}
