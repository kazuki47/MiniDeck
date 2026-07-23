using MiniDeck.Models;
using MiniDeck.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
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
        private readonly string _settingsFilePath;
        private bool _isLoadingSettings;
        private bool _isSettingsReadOnly;

        // マウスイベントをキャッチするためのダミーコマンド
        public ICommand PlaceholderCommand { get; private set; }

        // 設定保存コマンド
        public ICommand SaveSettingsCommand { get; private set; }

        public ICommand PreviousPageCommand { get; private set; }
        public ICommand NextPageCommand { get; private set; }

        public ObservableCollection<ActionButton> Buttons { get; set; }

        public ObservableCollection<PageSetting> Pages { get; private set; }

        private PageSetting _activePage;
        public PageSetting ActivePage
        {
            get => _activePage;
            private set
            {
                if (!ReferenceEquals(_activePage, value))
                {
                    _activePage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ActivePageId));
                    OnPropertyChanged(nameof(ActivePageName));
                    NotifyPageNavigationChanged();
                }
            }
        }

        public string ActivePageId => ActivePage?.Id ?? "";
        public string ActivePageName => ActivePage?.Name ?? "";
        public int PageCount => Pages?.Count ?? 0;
        public int ActivePageIndex => ActivePage == null || Pages == null
            ? -1
            : Pages.IndexOf(ActivePage);
        public int ActivePageNumber => ActivePageIndex < 0 ? 0 : ActivePageIndex + 1;
        public string ActivePagePositionText => $"{ActivePageNumber} / {PageCount}";
        public bool HasMultiplePages => PageCount > 1;
        public bool CanMoveToPreviousPage => ActivePageIndex > 0;
        public bool CanMoveToNextPage => ActivePageIndex >= 0 && ActivePageIndex < PageCount - 1;
        
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
          
        private string _backgroundColor = "#FFFFFFFF"; // デフォルトは白
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
          private double _backgroundOpacity = 1.0; // デフォルトは不透明
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

        private bool _alwaysOnTop = true;
        public bool AlwaysOnTop => _alwaysOnTop;

        private bool _autoStart;
        public bool AutoStart => _autoStart;
        
        private string _statusText = "Ready"; // ステータステキストのデフォルト値
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public void ApplyLayoutSettings(
            int buttonRows,
            int buttonColumns,
            double backgroundOpacity,
            double buttonOpacity)
        {
            ApplySettings(
                buttonRows,
                buttonColumns,
                backgroundOpacity,
                buttonOpacity,
                AutoStart,
                AlwaysOnTop);
        }

        public void ApplySettings(
            int buttonRows,
            int buttonColumns,
            double backgroundOpacity,
            double buttonOpacity,
            bool autoStart,
            bool alwaysOnTop,
            IEnumerable<ActionButton> activePageButtons = null,
            IEnumerable<PageSetting> pageSettings = null,
            string activePageId = null)
        {
            buttonRows = Math.Max(1, Math.Min(3, buttonRows));
            buttonColumns = Math.Max(1, Math.Min(5, buttonColumns));
            backgroundOpacity = Math.Max(0.0, Math.Min(1.0, backgroundOpacity));
            buttonOpacity = Math.Max(0.0, Math.Min(1.0, buttonOpacity));

            bool pagesChanged = pageSettings != null;
            List<PageSetting> pendingPages = pagesChanged
                ? CreateNormalizedPageSettings(pageSettings, buttonRows * buttonColumns)
                : null;
            List<ButtonSetting> pendingButtonSettings = pagesChanged || activePageButtons == null
                ? null
                : CreateNormalizedButtonSettings(activePageButtons, buttonRows * buttonColumns);

            bool layoutChanged = _buttonRows != buttonRows || _buttonColumns != buttonColumns;
            bool buttonsChanged = pendingButtonSettings != null &&
                                  !ButtonSettingsMatch(Buttons, pendingButtonSettings);
            bool backgroundOpacityChanged = Math.Abs(_backgroundOpacity - backgroundOpacity) > 0.0001;
            bool buttonOpacityChanged = Math.Abs(_buttonOpacity - buttonOpacity) > 0.0001;
            bool autoStartChanged = _autoStart != autoStart;
            bool alwaysOnTopChanged = _alwaysOnTop != alwaysOnTop;

            // 有効時は古い実行ファイルへの登録も修復する。無効かつ変更なしならレジストリへ触れない。
            // レジストリ更新に失敗した場合は、他の設定を変更する前に呼び出し元へ通知する。
            if (autoStart || autoStartChanged)
            {
                AutoStartService.Apply(autoStart);
            }

            if (!layoutChanged && !buttonsChanged && !backgroundOpacityChanged && !buttonOpacityChanged &&
                !autoStartChanged && !alwaysOnTopChanged && !pagesChanged)
            {
                return;
            }

            if (layoutChanged)
            {
                _buttonRows = buttonRows;
                _buttonColumns = buttonColumns;
                OnPropertyChanged(nameof(ButtonRows));
                OnPropertyChanged(nameof(ButtonColumns));

                // ページ全体を受け取った場合は、この後まとめて各ページを差し替える
                if (!pagesChanged && pendingButtonSettings != null)
                {
                    LoadButtonsFromSettings(pendingButtonSettings);
                }
                else if (!pagesChanged)
                {
                    UpdateButtons();
                }

                if (!pagesChanged)
                {
                    ResizeInactivePagesToLayout();
                }
            }
            else if (!pagesChanged && buttonsChanged)
            {
                LoadButtonsFromSettings(pendingButtonSettings);
            }

            if (pagesChanged)
            {
                Pages.Clear();
                foreach (PageSetting page in pendingPages)
                {
                    Pages.Add(page);
                }

                PageSetting selectedPage = Pages.FirstOrDefault(page => string.Equals(
                    page.Id,
                    activePageId,
                    StringComparison.OrdinalIgnoreCase)) ?? Pages[0];
                ActivePage = selectedPage;
                LoadButtonsFromSettings(selectedPage.Buttons);
                NotifyPageNavigationChanged();
            }

            if (backgroundOpacityChanged)
            {
                _backgroundOpacity = backgroundOpacity;
                OnPropertyChanged(nameof(BackgroundOpacity));
            }

            if (buttonOpacityChanged)
            {
                _buttonOpacity = buttonOpacity;
                OnPropertyChanged(nameof(ButtonOpacity));
            }

            if (autoStartChanged)
            {
                _autoStart = autoStart;
                OnPropertyChanged(nameof(AutoStart));
            }

            if (alwaysOnTopChanged)
            {
                _alwaysOnTop = alwaysOnTop;
                OnPropertyChanged(nameof(AlwaysOnTop));
            }

            // 設定画面での操作中は保存せず、適用時に一度だけ保存する
            SaveSettings();
        }

        public MainViewModel()
            : this(null)
        {
        }

        public MainViewModel(string settingsFilePath)
        {
            Console.WriteLine("MainViewModel: コンストラクタが呼び出されました");
            _actionService = new ActionService();
            _settingsFilePath = settingsFilePath;
            Buttons = new ObservableCollection<ActionButton>();
            Pages = new ObservableCollection<PageSetting>();
            Pages.CollectionChanged += (sender, args) => NotifyPageNavigationChanged();
            
            // コマンドの初期化
            PlaceholderCommand = new RelayCommand(ExecutePlaceholderCommand);
            SaveSettingsCommand = new RelayCommand(_ => SaveSettings());
            PreviousPageCommand = new RelayCommand(
                _ => MoveToPreviousPage(),
                _ => CanMoveToPreviousPage);
            NextPageCommand = new RelayCommand(
                _ => MoveToNextPage(),
                _ => CanMoveToNextPage);
            
            // 設定ファイルの確認
            string settingsPath = string.IsNullOrWhiteSpace(_settingsFilePath)
                ? System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MiniDeck", "settings.xml")
                : System.IO.Path.GetFullPath(_settingsFilePath);
            bool fileExists = System.IO.File.Exists(settingsPath);
            Console.WriteLine($"MainViewModel初期化時: 設定ファイル存在={fileExists}, パス={settingsPath}");

            _isLoadingSettings = true;
            try
            {
                // 設定を読み込む
                LoadSettings();

                // 設定が読み込めなかった場合はデフォルトのボタンをロード
                if (Buttons.Count == 0)
                {
                    Console.WriteLine("設定が読み込めなかったため、デフォルトボタンをロードします");
                    LoadDefaultButtons();
                }

                EnsurePageCollection();
                SyncActivePageButtons();
            }
            finally
            {
                _isLoadingSettings = false;
            }

            Console.WriteLine($"設定から{Buttons.Count}個のボタンと{Pages.Count}ページを読み込みました");

            // 初期化後に一度だけ保存を実行して設定ファイルを確実に作成
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
            if (_isLoadingSettings)
            {
                return;
            }

            if (_isSettingsReadOnly)
            {
                Console.WriteLine("設定は読み取り専用で読み込まれているため保存をスキップします。");
                return;
            }

            try
            {
                Console.WriteLine("アプリケーション設定を保存中...");

                EnsurePageCollection();
                SyncActivePageButtons();

                // 現在の設定を取得
                var settings = new AppSettings
                {
                    SettingsVersion = AppSettings.CurrentSettingsVersion,
                    ButtonRows = ButtonRows,
                    ButtonColumns = ButtonColumns,
                    BackgroundColor = BackgroundColor,
                    BackgroundImagePath = BackgroundImagePath,
                    BackgroundOpacity = BackgroundOpacity,
                    ButtonOpacity = ButtonOpacity,
                    UseBackgroundImage = UseBackgroundImage,
                    AlwaysOnTop = AlwaysOnTop,
                    AutoStart = AutoStart,
                    ActivePageId = ActivePageId,
                    Pages = Pages.Select(page => page.Clone()).ToList(),
                    Buttons = new List<ButtonSetting>()
                };

                // 設定を保存
                bool result = string.IsNullOrWhiteSpace(_settingsFilePath)
                    ? SettingsService.SaveSettings(settings)
                    : SettingsService.SaveSettings(settings, _settingsFilePath);
                if (result)
                {
                    Console.WriteLine($"設定を正常に保存しました。ページ: {settings.Pages.Count}、現在のボタン: {Buttons.Count}個");
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
                // 設定を読み込み
                var settings = string.IsNullOrWhiteSpace(_settingsFilePath)
                    ? SettingsService.LoadSettings()
                    : SettingsService.LoadSettings(_settingsFilePath);

                _isSettingsReadOnly = settings.IsReadOnly;
                if (!string.IsNullOrWhiteSpace(settings.LoadWarning))
                {
                    Console.WriteLine(settings.LoadWarning);
                }

                // 読み込んだ設定を適用
                ButtonRows = settings.ButtonRows;
                ButtonColumns = settings.ButtonColumns;
                BackgroundColor = settings.BackgroundColor;
                BackgroundImagePath = settings.BackgroundImagePath;
                BackgroundOpacity = settings.BackgroundOpacity;
                ButtonOpacity = settings.ButtonOpacity;
                UseBackgroundImage = settings.UseBackgroundImage;

                _alwaysOnTop = settings.AlwaysOnTop;
                _autoStart = AutoStartService.IsEnabled();
                OnPropertyChanged(nameof(AlwaysOnTop));
                OnPropertyChanged(nameof(AutoStart));

                Pages.Clear();
                foreach (PageSetting page in settings.Pages ?? new List<PageSetting>())
                {
                    Pages.Add(page.Clone());
                }

                EnsurePageCollection();
                ActivePage = Pages.FirstOrDefault(page => string.Equals(
                    page.Id,
                    settings.ActivePageId,
                    StringComparison.OrdinalIgnoreCase)) ?? Pages[0];

                if (ActivePage.Buttons != null && ActivePage.Buttons.Count > 0)
                {
                    LoadButtonsFromSettings(ActivePage.Buttons);
                }
                else
                {
                    LoadDefaultButtons();
                }

                SyncActivePageButtons();
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
            int totalButtons = ButtonRows * ButtonColumns;

            // コレクションをクリア
            Buttons.Clear();
            
            // 読み込んだボタン設定からボタンを作成
            int index = 0;
            foreach (var buttonSettingValue in buttonSettings ?? new List<ButtonSetting>())
            {
                if (index >= totalButtons)
                {
                    break;
                }

                var buttonSetting = buttonSettingValue ?? new ButtonSetting();
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
                        ApplicationArguments = buttonSetting.ApplicationArguments,
                        Url = buttonSetting.Url,
                        MacroActions = buttonSetting.MacroActions?
                            .Where(action => action != null)
                            .Select(action => action.Clone())
                            .ToList() ?? new List<MacroActionStep>(),
                        MacroFailureBehavior = buttonSetting.MacroFailureBehavior,
                        MacroRequireConfirmation = buttonSetting.MacroRequireConfirmation,
                        StateDisplayType = buttonSetting.StateDisplayType,
                        StateActiveDisplayText = buttonSetting.StateActiveDisplayText,
                        StateActiveImagePath = buttonSetting.StateActiveImagePath,
                        StateActiveBackgroundColor = string.IsNullOrWhiteSpace(buttonSetting.StateActiveBackgroundColor)
                            ? "#CC2E7D32"
                            : buttonSetting.StateActiveBackgroundColor,
                        StateInactiveBackgroundColor = string.IsNullOrWhiteSpace(buttonSetting.StateInactiveBackgroundColor)
                            ? "#403F3F46"
                            : buttonSetting.StateInactiveBackgroundColor
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
                        if (ActionButton.IsRuntimeProperty(e.PropertyName)) return;
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
                        if (ActionButton.IsRuntimeProperty(e.PropertyName)) return;
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

        public PageSetting AddPage(string name = null)
        {
            EnsurePageCollection();
            SyncActivePageButtons();

            var page = new PageSetting
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = CreateUniquePageName(string.IsNullOrWhiteSpace(name) ? $"ページ {Pages.Count + 1}" : name.Trim()),
                Buttons = CreateEmptyButtonSettings()
            };

            Pages.Add(page);
            SetActivePageCore(page);
            OnPropertyChanged(nameof(PageCount));
            SaveSettings();
            return page;
        }

        public PageSetting DuplicatePage(string pageId, string name = null)
        {
            EnsurePageCollection();
            SyncActivePageButtons();

            PageSetting sourcePage = FindPage(pageId);
            if (sourcePage == null)
            {
                return null;
            }

            string duplicateName = CreateUniquePageName(
                string.IsNullOrWhiteSpace(name) ? sourcePage.Name + " のコピー" : name.Trim());
            PageSetting duplicatePage = sourcePage.Clone(Guid.NewGuid().ToString("N"), duplicateName);
            ResizeButtonSettings(duplicatePage.Buttons, ButtonRows * ButtonColumns);

            int sourceIndex = Pages.IndexOf(sourcePage);
            Pages.Insert(sourceIndex + 1, duplicatePage);
            SetActivePageCore(duplicatePage);
            OnPropertyChanged(nameof(PageCount));
            SaveSettings();
            return duplicatePage;
        }

        public bool RenamePage(string pageId, string newName)
        {
            PageSetting page = FindPage(pageId);
            string trimmedName = newName?.Trim();
            if (page == null || string.IsNullOrWhiteSpace(trimmedName))
            {
                return false;
            }

            if (Pages.Any(other => !ReferenceEquals(other, page) &&
                string.Equals(other.Name, trimmedName, StringComparison.CurrentCultureIgnoreCase)))
            {
                return false;
            }

            if (string.Equals(page.Name, trimmedName, StringComparison.Ordinal))
            {
                return true;
            }

            page.Name = trimmedName;
            if (ReferenceEquals(page, ActivePage))
            {
                OnPropertyChanged(nameof(ActivePageName));
            }

            SaveSettings();
            return true;
        }

        public bool SetActivePage(string pageId)
        {
            PageSetting page = FindPage(pageId);
            if (page == null)
            {
                return false;
            }

            if (ReferenceEquals(page, ActivePage))
            {
                return true;
            }

            SyncActivePageButtons();
            SetActivePageCore(page);
            SaveSettings();
            return true;
        }

        public bool MoveToPreviousPage()
        {
            return MoveActivePage(-1);
        }

        public bool MoveToNextPage()
        {
            return MoveActivePage(1);
        }

        public int GetConfiguredButtonCount(string pageId)
        {
            if (string.Equals(pageId, ActivePageId, StringComparison.OrdinalIgnoreCase))
            {
                SyncActivePageButtons();
            }

            PageSetting page = FindPage(pageId);
            return page?.Buttons?.Count(button => button != null && button.ActionType != ActionType.None) ?? 0;
        }

        public bool DeletePage(string pageId)
        {
            EnsurePageCollection();
            if (Pages.Count <= 1)
            {
                return false;
            }

            SyncActivePageButtons();
            PageSetting page = FindPage(pageId);
            if (page == null)
            {
                return false;
            }

            int removedIndex = Pages.IndexOf(page);
            bool deletedActivePage = ReferenceEquals(page, ActivePage);
            Pages.Remove(page);

            if (deletedActivePage)
            {
                int nextIndex = Math.Min(removedIndex, Pages.Count - 1);
                SetActivePageCore(Pages[nextIndex]);
            }

            OnPropertyChanged(nameof(PageCount));
            SaveSettings();
            return true;
        }

        private void EnsurePageCollection()
        {
            if (Pages == null)
            {
                Pages = new ObservableCollection<PageSetting>();
                OnPropertyChanged(nameof(Pages));
            }

            if (Pages.Count == 0)
            {
                Pages.Add(new PageSetting
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = "メイン",
                    Buttons = Buttons != null && Buttons.Count > 0
                        ? SettingsService.CreateButtonSettingsList(Buttons)
                        : new List<ButtonSetting>()
                });
                OnPropertyChanged(nameof(PageCount));
            }

            if (ActivePage == null || !Pages.Contains(ActivePage))
            {
                ActivePage = Pages[0];
            }
        }

        private void SyncActivePageButtons()
        {
            if (ActivePage == null)
            {
                return;
            }

            ActivePage.Buttons = SettingsService.CreateButtonSettingsList(Buttons);
        }

        private void SetActivePageCore(PageSetting page)
        {
            ActivePage = page;
            LoadButtonsFromSettings(page.Buttons ?? new List<ButtonSetting>());
        }

        private bool MoveActivePage(int offset)
        {
            EnsurePageCollection();

            int targetIndex = ActivePageIndex + offset;
            if (offset == 0 || targetIndex < 0 || targetIndex >= PageCount)
            {
                return false;
            }

            return SetActivePage(Pages[targetIndex].Id);
        }

        private void NotifyPageNavigationChanged()
        {
            OnPropertyChanged(nameof(PageCount));
            OnPropertyChanged(nameof(ActivePageIndex));
            OnPropertyChanged(nameof(ActivePageNumber));
            OnPropertyChanged(nameof(ActivePagePositionText));
            OnPropertyChanged(nameof(HasMultiplePages));
            OnPropertyChanged(nameof(CanMoveToPreviousPage));
            OnPropertyChanged(nameof(CanMoveToNextPage));
            CommandManager.InvalidateRequerySuggested();
        }

        private PageSetting FindPage(string pageId)
        {
            if (string.IsNullOrWhiteSpace(pageId) || Pages == null)
            {
                return null;
            }

            return Pages.FirstOrDefault(page => string.Equals(
                page.Id,
                pageId,
                StringComparison.OrdinalIgnoreCase));
        }

        private string CreateUniquePageName(string requestedName)
        {
            string baseName = string.IsNullOrWhiteSpace(requestedName) ? "新しいページ" : requestedName.Trim();
            string candidate = baseName;
            int suffix = 2;

            while (Pages.Any(page => string.Equals(page.Name, candidate, StringComparison.CurrentCultureIgnoreCase)))
            {
                candidate = $"{baseName} ({suffix})";
                suffix++;
            }

            return candidate;
        }

        private static List<ButtonSetting> CreateNormalizedButtonSettings(
            IEnumerable<ActionButton> buttons,
            int totalButtons)
        {
            var normalizedButtons = new List<ButtonSetting>();
            foreach (ActionButton button in buttons ?? Enumerable.Empty<ActionButton>())
            {
                if (normalizedButtons.Count >= totalButtons)
                {
                    break;
                }

                normalizedButtons.Add(ButtonSetting.FromActionButton(button) ?? new ButtonSetting
                {
                    DisplayText = $"ボタン {normalizedButtons.Count + 1}",
                    ActionType = ActionType.None
                });
            }

            while (normalizedButtons.Count < totalButtons)
            {
                normalizedButtons.Add(new ButtonSetting
                {
                    DisplayText = $"ボタン {normalizedButtons.Count + 1}",
                    ActionType = ActionType.None
                });
            }

            return normalizedButtons;
        }

        private static List<PageSetting> CreateNormalizedPageSettings(
            IEnumerable<PageSetting> pages,
            int totalButtons)
        {
            var normalizedPages = new List<PageSetting>();
            var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var usedNames = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

            foreach (PageSetting sourcePage in pages ?? Enumerable.Empty<PageSetting>())
            {
                if (sourcePage == null)
                {
                    continue;
                }

                PageSetting page = sourcePage.Clone();
                string requestedId = page.Id?.Trim();
                if (string.IsNullOrWhiteSpace(requestedId) || !usedIds.Add(requestedId))
                {
                    do
                    {
                        requestedId = Guid.NewGuid().ToString("N");
                    }
                    while (!usedIds.Add(requestedId));
                }

                page.Id = requestedId;
                string baseName = string.IsNullOrWhiteSpace(page.Name)
                    ? $"ページ {normalizedPages.Count + 1}"
                    : page.Name.Trim();
                string uniqueName = baseName;
                int suffix = 2;
                while (!usedNames.Add(uniqueName))
                {
                    uniqueName = $"{baseName} ({suffix})";
                    suffix++;
                }

                page.Name = uniqueName;
                page.Buttons = page.Buttons ?? new List<ButtonSetting>();
                ResizeButtonSettings(page.Buttons, totalButtons);
                normalizedPages.Add(page);
            }

            if (normalizedPages.Count == 0)
            {
                var page = new PageSetting
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = "メイン",
                    Buttons = new List<ButtonSetting>()
                };
                ResizeButtonSettings(page.Buttons, totalButtons);
                normalizedPages.Add(page);
            }

            return normalizedPages;
        }

        private static bool ButtonSettingsMatch(
            IList<ActionButton> buttons,
            IList<ButtonSetting> buttonSettings)
        {
            if (buttons == null || buttonSettings == null || buttons.Count != buttonSettings.Count)
            {
                return false;
            }

            for (int index = 0; index < buttons.Count; index++)
            {
                ActionButton button = buttons[index] ?? new ActionButton();
                ButtonSetting setting = buttonSettings[index] ?? new ButtonSetting();
                if (!string.Equals(button.DisplayText ?? "", setting.DisplayText ?? "", StringComparison.Ordinal) ||
                    !string.Equals(button.ImagePath ?? "", setting.ImagePath ?? "", StringComparison.Ordinal) ||
                    button.ActionType != setting.ActionType ||
                    !string.Equals(button.ShortcutKeySequence ?? "", setting.ShortcutKeySequence ?? "", StringComparison.Ordinal) ||
                    !string.Equals(button.ApplicationPath ?? "", setting.ApplicationPath ?? "", StringComparison.Ordinal) ||
                    !string.Equals(button.ApplicationArguments ?? "", setting.ApplicationArguments ?? "", StringComparison.Ordinal) ||
                    !string.Equals(button.Url ?? "", setting.Url ?? "", StringComparison.Ordinal) ||
                    button.MacroFailureBehavior != setting.MacroFailureBehavior ||
                    button.MacroRequireConfirmation != setting.MacroRequireConfirmation ||
                    !MacroActionsMatch(button.MacroActions, setting.MacroActions) ||
                    button.StateDisplayType != setting.StateDisplayType ||
                    !string.Equals(button.StateActiveDisplayText ?? "", setting.StateActiveDisplayText ?? "", StringComparison.Ordinal) ||
                    !string.Equals(button.StateActiveImagePath ?? "", setting.StateActiveImagePath ?? "", StringComparison.Ordinal) ||
                    !string.Equals(button.StateActiveBackgroundColor ?? "", setting.StateActiveBackgroundColor ?? "", StringComparison.Ordinal) ||
                    !string.Equals(button.StateInactiveBackgroundColor ?? "", setting.StateInactiveBackgroundColor ?? "", StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MacroActionsMatch(
            IList<MacroActionStep> actions,
            IList<MacroActionStep> settings)
        {
            actions = actions ?? new List<MacroActionStep>();
            settings = settings ?? new List<MacroActionStep>();
            if (actions.Count != settings.Count)
            {
                return false;
            }

            for (int index = 0; index < actions.Count; index++)
            {
                MacroActionStep action = actions[index] ?? new MacroActionStep();
                MacroActionStep setting = settings[index] ?? new MacroActionStep();
                if (action.ActionType != setting.ActionType ||
                    !string.Equals(action.ShortcutKeySequence ?? "", setting.ShortcutKeySequence ?? "", StringComparison.Ordinal) ||
                    !string.Equals(action.ApplicationPath ?? "", setting.ApplicationPath ?? "", StringComparison.Ordinal) ||
                    !string.Equals(action.ApplicationArguments ?? "", setting.ApplicationArguments ?? "", StringComparison.Ordinal) ||
                    !string.Equals(action.Url ?? "", setting.Url ?? "", StringComparison.Ordinal) ||
                    action.DelayAfterMilliseconds != setting.DelayAfterMilliseconds)
                {
                    return false;
                }
            }

            return true;
        }

        private List<ButtonSetting> CreateEmptyButtonSettings()
        {
            var buttons = new List<ButtonSetting>();
            int totalButtons = ButtonRows * ButtonColumns;
            for (int index = 0; index < totalButtons; index++)
            {
                buttons.Add(new ButtonSetting
                {
                    DisplayText = $"ボタン {index + 1}",
                    ActionType = ActionType.None
                });
            }

            return buttons;
        }

        private void ResizeInactivePagesToLayout()
        {
            if (Pages == null)
            {
                return;
            }

            int totalButtons = ButtonRows * ButtonColumns;
            foreach (PageSetting page in Pages)
            {
                if (!ReferenceEquals(page, ActivePage))
                {
                    ResizeButtonSettings(page.Buttons, totalButtons);
                }
            }
        }

        private static void ResizeButtonSettings(List<ButtonSetting> buttons, int totalButtons)
        {
            if (buttons == null)
            {
                return;
            }

            if (buttons.Count > totalButtons)
            {
                buttons.RemoveRange(totalButtons, buttons.Count - totalButtons);
            }

            while (buttons.Count < totalButtons)
            {
                buttons.Add(new ButtonSetting
                {
                    DisplayText = $"ボタン {buttons.Count + 1}",
                    ActionType = ActionType.None
                });
            }
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
                    if (ActionButton.IsRuntimeProperty(e.PropertyName)) return;
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
                        if (ActionButton.IsRuntimeProperty(e.PropertyName)) return;
                        Console.WriteLine($"新規ボタン[{capturedIndex}]のプロパティ{e.PropertyName}が変更されました - 設定を保存します");
                        SaveSettings();
                    };
                    
                    Buttons.Add(newButton);
                }
            }
        }
        
        private async void ExecuteButtonAction(ActionButton button)
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
                    break;
                case ActionType.OpenUrl:
                    _actionService.OpenUrl(button.Url);
                    break;
                case ActionType.MultiAction:
                    await ExecuteMacroActionAsync(button);
                    break;
                case ActionType.None:
                    // アクションなし - 何もしない（メッセージボックスは表示しない）
                    break;
            }
        }

        private async Task ExecuteMacroActionAsync(ActionButton button)
        {
            if (button.IsExecuting)
            {
                return;
            }

            List<MacroActionStep> actions = button.MacroActions?
                .Where(action => action != null)
                .Select(action => action.Clone())
                .ToList() ?? new List<MacroActionStep>();
            if (actions.Count == 0)
            {
                button.UpdateExecutionState(
                    MacroExecutionState.Failed,
                    errorMessage: "実行するアクションが登録されていません。");
                StatusText = $"{button.DisplayText}: アクション未設定";
                return;
            }

            if (button.MacroRequireConfirmation)
            {
                MessageBoxResult confirmation = MessageBox.Show(
                    $"「{button.DisplayText}」の{actions.Count}個のアクションを実行しますか？",
                    "マルチアクションの確認",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);
                if (confirmation != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            try
            {
                button.UpdateExecutionState(MacroExecutionState.Running, 1, actions.Count);
                StatusText = $"{button.DisplayText}: 実行中";
                MacroExecutionResult result = await _actionService.ExecuteMacroAsync(
                    actions,
                    button.MacroFailureBehavior,
                    (step, count) =>
                    {
                        button.UpdateExecutionState(MacroExecutionState.Running, step, count);
                        StatusText = $"{button.DisplayText}: {step}/{count} 実行中";
                    });

                if (result.Succeeded)
                {
                    button.UpdateExecutionState(
                        MacroExecutionState.Succeeded,
                        actions.Count,
                        actions.Count);
                    StatusText = $"{button.DisplayText}: 完了";
                    await Task.Delay(1500);
                    if (button.ExecutionState == MacroExecutionState.Succeeded)
                    {
                        button.UpdateExecutionState(MacroExecutionState.Idle);
                    }
                }
                else
                {
                    string failureMessage = result.FailedCount > 1
                        ? $"{result.FailedCount}個のアクションに失敗しました。{result.ErrorMessage}"
                        : result.ErrorMessage;
                    button.UpdateExecutionState(
                        MacroExecutionState.Failed,
                        result.AttemptedCount,
                        actions.Count,
                        failureMessage);
                    StatusText = $"{button.DisplayText}: 失敗";
                    MessageBox.Show(
                        failureMessage,
                        result.StoppedOnFailure
                            ? "マルチアクションを停止しました"
                            : "マルチアクションが完了しました（失敗あり）",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                button.UpdateExecutionState(
                    MacroExecutionState.Failed,
                    errorMessage: ex.Message);
                StatusText = $"{button.DisplayText}: 失敗";
                MessageBox.Show(
                    $"マルチアクションの実行中にエラーが発生しました。\n{ex.Message}",
                    "マルチアクションエラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
