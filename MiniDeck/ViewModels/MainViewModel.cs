using MiniDeck.Models;
using MiniDeck.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MiniDeck.ViewModels
{
    // ICommandインターフェースを実装したシンプルなコマンドクラス
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object parameter) => _execute(parameter);
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ActionService _actionService;

        // マウスイベントをキャッチするためのダミーコマンド
        public ICommand PlaceholderCommand { get; private set; }

        // 設定保存コマンド
        public ICommand SaveSettingsCommand { get; private set; }

        public ObservableCollection<ActionButton> Buttons { get; set; }
        
        private int _buttonRows = 2; // デフォルト値
        public int ButtonRows
        {
            get => _buttonRows;
            set 
            { 
                if (_buttonRows != value)
                {
                    _buttonRows = value; 
                    OnPropertyChanged();
                    UpdateButtons(); // ボタン数が変更されたらボタンを更新
                    SaveSettings(); // 設定を保存
                }
            }
        }

        private int _buttonColumns = 4; // デフォルト値
        public int ButtonColumns
        {
            get => _buttonColumns;
            set 
            { 
                if (_buttonColumns != value)
                {
                    _buttonColumns = value; 
                    OnPropertyChanged();
                    UpdateButtons(); // ボタン数が変更されたらボタンを更新
                    SaveSettings(); // 設定を保存
                }
            }
        }
          
        private string _backgroundColor = "#FF000000"; // デフォルト値
        public string BackgroundColor
        {
            get => _backgroundColor;
            set 
            { 
                _backgroundColor = value; 
                OnPropertyChanged();
                SaveSettings(); // 設定を保存
            }
        }
        
        private string _backgroundImagePath = ""; // デフォルト値は空
        public string BackgroundImagePath
        {
            get => _backgroundImagePath;
            set 
            { 
                _backgroundImagePath = value; 
                OnPropertyChanged();
                SaveSettings(); // 設定を保存
            }
        }
          private double _backgroundOpacity = 0.0; // デフォルト値を0%に設定（完全に透明）
        public double BackgroundOpacity
        {
            get => _backgroundOpacity;
            set 
            { 
                if (_backgroundOpacity != value)
                {
                    double oldValue = _backgroundOpacity;
                    _backgroundOpacity = value; 
                    Console.WriteLine($"BackgroundOpacity プロパティが変更されました: {oldValue:F2} -> {value:F2}");
                    
                    // このプロパティ変更を明示的に通知
                    OnPropertyChanged();
                    
                    // 変更後の値を確認（デバッグ用）
                    Console.WriteLine($"変更後の値を確認: BackgroundOpacity = {_backgroundOpacity:F2}");
                    
                    // 設定を保存
                    SaveSettings();
                }
            }
        }
        
        private double _buttonOpacity = 0.6; // ボタンの透明度（デフォルト値）
        public double ButtonOpacity
        {
            get => _buttonOpacity;
            set 
            { 
                if (_buttonOpacity != value)
                {
                    double oldValue = _buttonOpacity;
                    _buttonOpacity = value; 
                    OnPropertyChanged();
                    Console.WriteLine($"ButtonOpacity changed from: {oldValue:F2} to: {_buttonOpacity:F2}");
                    
                    // 他のリスナーにも通知
                    OnPropertyChanged(nameof(ButtonOpacity));
                    
                    // 設定を保存
                    SaveSettings();
                }
            }
        }
          
        private bool _useBackgroundImage = false; // デフォルトは背景色を使用
        public bool UseBackgroundImage
        {
            get => _useBackgroundImage;
            set 
            { 
                _useBackgroundImage = value; 
                OnPropertyChanged();
                SaveSettings(); // 設定を保存
            }
        }
        
        private string _statusText = "Ready"; // ステータステキストのデフォルト値
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }        public MainViewModel()
        {
            Console.WriteLine("MainViewModel: コンストラクタが呼び出されました");
            _actionService = new ActionService();
            Buttons = new ObservableCollection<ActionButton>();
            
            // コマンドの初期化
            PlaceholderCommand = new RelayCommand(ExecutePlaceholderCommand);
            SaveSettingsCommand = new RelayCommand(_ => SaveSettings());
            
            // 設定ファイルの確認
            string settingsPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MiniDeck", "settings.xml");
            bool fileExists = System.IO.File.Exists(settingsPath);
            Console.WriteLine($"MainViewModel初期化時: 設定ファイル存在={fileExists}, パス={settingsPath}");
            
            // 設定を読み込む
            LoadSettings();
            
            // 設定が読み込めなかった場合はデフォルトのボタンをロード
            if (Buttons.Count == 0)
            {
                Console.WriteLine("設定が読み込めなかったため、デフォルトボタンをロードします");
                LoadDefaultButtons();
            }
            else
            {
                Console.WriteLine($"設定から{Buttons.Count}個のボタンを読み込みました");
            }
              // 初期化後に保存を実行して設定ファイルを確実に作成
            Console.WriteLine("MainViewModel初期化完了後、設定保存を実行します");
            try 
            {
                SaveSettings();
                Console.WriteLine("初期化時の設定保存が完了しました");
            }
            catch (Exception saveEx)
            {
                Console.WriteLine($"初期化時の設定保存でエラー: {saveEx.Message}");
            }
        }// 設定を保存する
        public void SaveSettings()
        {
            try
            {
                Console.WriteLine("アプリケーション設定を保存中...");
                
                // 現在の設定を取得
                var settings = new AppSettings
                {
                    ButtonRows = ButtonRows,
                    ButtonColumns = ButtonColumns,
                    BackgroundColor = BackgroundColor,
                    BackgroundImagePath = BackgroundImagePath,
                    BackgroundOpacity = BackgroundOpacity,
                    ButtonOpacity = ButtonOpacity,
                    UseBackgroundImage = UseBackgroundImage,
                    AlwaysOnTop = true, // 常に最前面表示
                    Buttons = new List<MiniDeck.Models.ButtonSetting>()
                };

                // ボタン設定を追加
                Console.WriteLine($"保存するボタン数: {Buttons.Count}個");
                int index = 0;
                
                foreach (var button in Buttons)
                {
                    try
                    {
                        // ButtonSettingオブジェクトを作成
                        var buttonSetting = new MiniDeck.Models.ButtonSetting
                        {
                            DisplayText = button.DisplayText,
                            ImagePath = button.ImagePath,
                            ActionType = button.ActionType,
                            ShortcutKeySequence = button.ShortcutKeySequence,
                            ApplicationPath = button.ApplicationPath,
                            ApplicationArguments = button.ApplicationArguments
                        };
                        
                        // アプリケーション起動の場合、詳細をログに出力
                        if (button.ActionType == ActionType.LaunchApplication)
                        {
                            Console.WriteLine($"ボタン[{index}] 保存中: \"{button.DisplayText}\", アプリケーションパス: \"{button.ApplicationPath}\"");
                        }
                        else
                        {
                            Console.WriteLine($"ボタン[{index}] 保存中: \"{button.DisplayText}\", アクションタイプ: {button.ActionType}");
                        }
                        
                        settings.Buttons.Add(buttonSetting);
                        index++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ボタン[{index}]の保存中にエラーが発生: {ex.Message}");
                    }
                }
                
                // 設定保存パスの取得
                string settingsPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MiniDeck", "settings.xml");
                Console.WriteLine($"設定保存先: {settingsPath}");
                
                // 設定を保存
                bool result = MiniDeck.Services.SettingsService.SaveSettings(settings);
                if (result)
                {
                    Console.WriteLine($"設定を正常に保存しました。ボタン設定: {settings.Buttons.Count}個");
                }
                else
                {
                    Console.WriteLine("設定の保存に失敗しました");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"設定の保存中にエラーが発生しました: {ex.Message}");
                Console.WriteLine($"スタックトレース: {ex.StackTrace}");
            }
        }// 設定を読み込む
        private void LoadSettings()
        {
            try
            {
                // 設定ファイルのパスを取得して確認
                string settingsPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MiniDeck", "settings.xml");
                
                Console.WriteLine($"設定ファイルを読み込みます: {settingsPath}");
                bool fileExists = System.IO.File.Exists(settingsPath);
                Console.WriteLine($"設定ファイルの存在: {fileExists}");
                
                // 設定を読み込み
                var settings = MiniDeck.Services.SettingsService.LoadSettings();
                
                // 読み込んだ設定を適用
                ButtonRows = settings.ButtonRows;
                ButtonColumns = settings.ButtonColumns;
                BackgroundColor = settings.BackgroundColor;
                BackgroundImagePath = settings.BackgroundImagePath;
                BackgroundOpacity = settings.BackgroundOpacity;
                ButtonOpacity = settings.ButtonOpacity;
                UseBackgroundImage = settings.UseBackgroundImage;
                
                // ボタン数のチェック
                Console.WriteLine($"読み込まれたボタン設定の数: {settings.Buttons?.Count ?? 0}");
                
                // ボタンを読み込み
                if (settings.Buttons != null && settings.Buttons.Count > 0)
                {
                    LoadButtonsFromSettings(settings.Buttons);
                    Console.WriteLine("ボタン設定を読み込みました");
                }
                else
                {
                    Console.WriteLine("ボタン設定がないため、デフォルトのボタンをロードします");
                    LoadDefaultButtons();
                }
                
                Console.WriteLine("設定の読み込みが完了しました");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"設定の読み込み中にエラーが発生しました: {ex.Message}");
                Console.WriteLine($"スタックトレース: {ex.StackTrace}");
                LoadDefaultButtons(); // エラーが発生したらデフォルト設定をロード
            }
        }        // 設定からボタンを読み込む
        private void LoadButtonsFromSettings(List<MiniDeck.Models.ButtonSetting> buttonSettings)
        {
            Console.WriteLine("ボタン設定からボタンを読み込み中...");
            
            // コレクションをクリア
            Buttons.Clear();
            
            // 読み込んだボタン設定からボタンを作成
            int index = 0;
            foreach (var buttonSetting in buttonSettings)
            {
                try
                {
                    Console.WriteLine($"ボタン[{index}] 読み込み中: 表示テキスト=\"{buttonSetting.DisplayText}\", アクションタイプ={buttonSetting.ActionType}");
                    
                    // ButtonSettingからActionButtonへの変換
                    var button = new ActionButton
                    {
                        DisplayText = buttonSetting.DisplayText,
                        ImagePath = buttonSetting.ImagePath,
                        ActionType = buttonSetting.ActionType,
                        ShortcutKeySequence = buttonSetting.ShortcutKeySequence,
                        ApplicationPath = buttonSetting.ApplicationPath,
                        ApplicationArguments = buttonSetting.ApplicationArguments
                    };
                    
                    // ボタンのクリックコマンドを設定
                    int buttonIndex = index; // インデックスをキャプチャしないようにローカル変数に保存
                    button.ClickCommand = new RelayCommand(_ => ExecuteButtonAction(Buttons[buttonIndex]));
                    
                    // アプリケーション起動の場合、詳細をログに出力
                    if (button.ActionType == ActionType.LaunchApplication)
                    {
                        Console.WriteLine($"  アプリケーション起動パス: \"{button.ApplicationPath}\"");
                        Console.WriteLine($"  アプリケーション引数: \"{button.ApplicationArguments}\"");
                    }                      // ボタンをコレクションに追加
                    Buttons.Add(button);
                    
                    // ボタンのプロパティ変更イベントを監視して自動保存（インデックスキャプチャ問題を回避）
                    int capturedIndex = index; // ローカル変数でインデックスをキャプチャ
                    button.PropertyChanged += (sender, e) => {
                        Console.WriteLine($"ボタン[{capturedIndex}]のプロパティ{e.PropertyName}が変更されました - 設定を保存します");
                        SaveSettings();
                    };
                    
                    index++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ボタン[{index}]の読み込み中にエラーが発生: {ex.Message}");
                }
            }
            
            Console.WriteLine($"読み込まれたボタン: {Buttons.Count}個");
              // ボタン不足分を補充
            int totalButtons = ButtonRows * ButtonColumns;
            for (int i = Buttons.Count; i < totalButtons; i++)
            {
                try
                {
                    int buttonIndex = i; // このインデックスをキャプチャ
                    var newButton = new ActionButton
                    {
                        DisplayText = $"ボタン {i+1}",
                        ActionType = ActionType.None,
                        ClickCommand = new RelayCommand(_ => {
                            // 安全なインデックスアクセス
                            if (buttonIndex < Buttons.Count)
                                ExecuteButtonAction(Buttons[buttonIndex]);
                        })
                    };
                    
                    // プロパティ変更イベントの監視
                    int capturedIndex = i;
                    newButton.PropertyChanged += (sender, e) => {
                        Console.WriteLine($"デフォルトボタン[{capturedIndex}]のプロパティ{e.PropertyName}が変更されました - 設定を保存します");
                        SaveSettings();
                    };
                    
                    Buttons.Add(newButton);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"デフォルトボタン[{i}]の作成中にエラーが発生: {ex.Message}");
                }
            }
            
            Console.WriteLine($"合計ボタン数: {Buttons.Count}個");
        }

        // マウスイベントをキャッチするためのダミーメソッド
        private void ExecutePlaceholderCommand(object parameter)
        {
            // 何もしない - このメソッドの目的はイベントをキャッチすることだけ
            Console.WriteLine("ウィンドウ内のマウスイベントをキャプチャしました");
        }
        
        private void LoadDefaultButtons()
        {
            // サンプルボタン（最初に配置するボタン）を定義
            var sampleButtons = new ActionButton[]
            {                new ActionButton
                {
                    DisplayText = "メモ帳",
                    ImagePath = "/Resources/Icons/memo_icon.png",
                    ActionType = ActionType.LaunchApplication,
                    ApplicationPath = "notepad.exe"
                },
                new ActionButton
                {
                    DisplayText = "電卓",
                    ImagePath = "/Resources/Icons/calculator_icon.png",
                    ActionType = ActionType.LaunchApplication,
                    ApplicationPath = "calc.exe"
                }
            };
            
            // 必要なボタン数を計算
            int totalButtons = ButtonRows * ButtonColumns;
            
            // コレクションをクリアして再構築
            Buttons.Clear();
            
            // 必要な数だけボタンを追加
            for (int i = 0; i < totalButtons; i++)
            {
                ActionButton newButton;
                
                if (i < sampleButtons.Length)
                {
                    // サンプルボタンがある場合はそれを使用
                    newButton = sampleButtons[i];
                }
                else
                {
                    // サンプルボタンがない場合は空のボタンを追加
                    newButton = new ActionButton
                    {
                        DisplayText = $"ボタン {i+1}",
                        ActionType = ActionType.None
                    };
                }                  // インデックスキャプチャ問題を回避するため、ローカル変数を使用
                int buttonIndex = i; // この値をキャプチャ
                newButton.ClickCommand = new RelayCommand(_ => {
                    // 安全なインデックスアクセス
                    if (buttonIndex < Buttons.Count)
                        ExecuteButtonAction(Buttons[buttonIndex]);
                });
                
                // ボタンのプロパティ変更イベントを監視して自動保存
                int capturedIndex = i; // ローカル変数でキャプチャ
                newButton.PropertyChanged += (sender, e) => {
                    Console.WriteLine($"デフォルトボタン[{capturedIndex}]のプロパティ{e.PropertyName}が変更されました - 設定を保存します");
                    SaveSettings();
                };
                
                Buttons.Add(newButton);
            }
        }        // ボタン数が変更されたときに呼び出されるメソッド
        private void UpdateButtons()
        {
            // 現在のボタンを保存
            var existingButtons = new ActionButton[Buttons.Count];
            Buttons.CopyTo(existingButtons, 0);
            
            // 必要なボタン数
            int totalButtons = ButtonRows * ButtonColumns;
            
            // コレクションをクリアして再構築
            Buttons.Clear();
              // ボタンを再追加
            for (int i = 0; i < totalButtons; i++)
            {                if (i < existingButtons.Length)
                {                    // 既存のボタンがある場合はそれを再利用
                    int buttonIndex = i; // ローカル変数でキャプチャ
                    existingButtons[i].ClickCommand = new RelayCommand(_ => {
                        // 安全なインデックスアクセス
                        if (buttonIndex < Buttons.Count)
                            ExecuteButtonAction(Buttons[buttonIndex]);
                    });
                    
                    // 既存ボタンは既にPropertyChangedイベントハンドラが設定されているため再設定不要
                    // （重複を避けるため）
                    
                    Buttons.Add(existingButtons[i]);
                }
                else
                {
                    // 新しいボタンを追加
                    int buttonIndex = i; // ローカル変数でキャプチャ
                    var newButton = new ActionButton
                    {
                        DisplayText = $"ボタン {i+1}",
                        ActionType = ActionType.None,
                        ClickCommand = new RelayCommand(_ => {
                            // 安全なインデックスアクセス
                            if (buttonIndex < Buttons.Count)
                                ExecuteButtonAction(Buttons[buttonIndex]);
                        })
                    };
                    
                    // 新規ボタンにもPropertyChangedイベントハンドラを設定
                    int capturedIndex = i; // ローカル変数でキャプチャ
                    newButton.PropertyChanged += (sender, e) => {
                        Console.WriteLine($"新規ボタン[{capturedIndex}]のプロパティ{e.PropertyName}が変更されました - 設定を保存します");
                        SaveSettings();
                    };
                    
                    Buttons.Add(newButton);
                }
            }
        }
        
        private void ExecuteButtonAction(ActionButton button)
        {
            if (button == null) return;
            
            // ステータステキストを更新
            StatusText = button.DisplayText;

            switch (button.ActionType)
            {
                case ActionType.KeyPress:
                    _actionService.ExecuteKeyPress(button.ShortcutKeySequence);
                    break;
                case ActionType.LaunchApplication:
                    _actionService.LaunchApplication(button.ApplicationPath, button.ApplicationArguments);
                    break;                case ActionType.None:
                    // アクションなし - 何もしない（メッセージボックスは表示しない）
                    break;
            }
        }        // ダミーコマンドのハンドラメソッド        
        // INotifyPropertyChangedの実装
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}