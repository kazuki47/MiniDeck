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
        // 基本設定
        [XmlElement("ButtonRows")]
        public int ButtonRows { get; set; } = 2;
        
        [XmlElement("ButtonColumns")]
        public int ButtonColumns { get; set; } = 4;
        
        // 背景設定
        [XmlElement("BackgroundColor")]
        public string BackgroundColor { get; set; } = "#FF000000";
        
        [XmlElement("BackgroundImagePath")]
        public string BackgroundImagePath { get; set; } = "";
        
        [XmlElement("BackgroundOpacity")]
        public double BackgroundOpacity { get; set; } = 0.0;
        
        [XmlElement("ButtonOpacity")]
        public double ButtonOpacity { get; set; } = 0.6;
        
        [XmlElement("UseBackgroundImage")]
        public bool UseBackgroundImage { get; set; } = false;
        
        // 一般設定
        [XmlElement("AlwaysOnTop")]
        public bool AlwaysOnTop { get; set; } = true;
        
        [XmlElement("AutoStart")]
        public bool AutoStart { get; set; } = false;
        
        // ボタン設定のリスト
        [XmlArray("Buttons")]
        [XmlArrayItem("ButtonSetting")]
        public List<ButtonSetting> Buttons { get; set; } = new List<ButtonSetting>();
        
        // パラメータなしのコンストラクタ（XMLシリアライゼーション用）
        public AppSettings()
        {
        }}
}
