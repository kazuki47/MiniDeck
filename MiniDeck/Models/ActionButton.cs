using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input; // ICommandのため
using System.Windows.Media;

namespace MiniDeck.Models
{
    public class ActionButton : INotifyPropertyChanged
    {
        private string _displayText;
        public string DisplayText
        {
            get => _displayText;
            set
            {
                _displayText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EffectiveDisplayText));
            }
        }

        private string _imagePath;        
        public string ImagePath // ボタンの画像パス (例: "/Resources/Icons/my_icon.png")
        {
            get => _imagePath;
            set
            {
                _imagePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EffectiveImagePath));
            }
        }        private ActionType _actionType;
        public ActionType ActionType 
        { 
            get => _actionType;
            set
            {
                _actionType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ActionTypeDisplayName));
            }
        }

        public string ActionTypeDisplayName
        {
            get
            {
                switch (ActionType)
                {
                    case ActionType.KeyPress:
                        return "キーボードショートカット";
                    case ActionType.LaunchApplication:
                        return "アプリ・ファイルを開く";
                    case ActionType.OpenUrl:
                        return "URLを開く";
                    default:
                        return "なし";
                }
            }
        }

        // KeyPress
        private string _shortcutKeySequence;
        public string ShortcutKeySequence 
        { 
            get => _shortcutKeySequence;
            set { _shortcutKeySequence = value; OnPropertyChanged(); }
        }

        // LaunchApplication
        private string _applicationPath;
        public string ApplicationPath 
        { 
            get => _applicationPath;
            set { _applicationPath = value; OnPropertyChanged(); }
        }
        
        private string _applicationArguments;
        public string ApplicationArguments 
        { 
            get => _applicationArguments;
            set { _applicationArguments = value; OnPropertyChanged(); }
        }

        // OpenUrl
        private string _url;
        public string Url
        {
            get => _url;
            set { _url = value; OnPropertyChanged(); }
        }

        private ButtonStateDisplayType _stateDisplayType;
        public ButtonStateDisplayType StateDisplayType
        {
            get => _stateDisplayType;
            set
            {
                if (_stateDisplayType == value)
                {
                    return;
                }

                _stateDisplayType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StateDisplayTypeDisplayName));
                UpdateRuntimeState(ButtonRuntimeState.Unknown, "");
            }
        }

        public string StateDisplayTypeDisplayName
        {
            get
            {
                switch (StateDisplayType)
                {
                    case ButtonStateDisplayType.ApplicationRunning:
                        return "アプリの起動状態";
                    case ButtonStateDisplayType.MicrophoneMuted:
                        return "マイクのミュート状態";
                    case ButtonStateDisplayType.SystemAudioMuted:
                        return "システム音声のミュート状態";
                    default:
                        return "状態表示なし";
                }
            }
        }

        private string _stateActiveDisplayText = "";
        public string StateActiveDisplayText
        {
            get => _stateActiveDisplayText;
            set
            {
                _stateActiveDisplayText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EffectiveDisplayText));
            }
        }

        private string _stateActiveImagePath = "";
        public string StateActiveImagePath
        {
            get => _stateActiveImagePath;
            set
            {
                _stateActiveImagePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EffectiveImagePath));
            }
        }

        private string _stateActiveBackgroundColor = "#CC2E7D32";
        public string StateActiveBackgroundColor
        {
            get => _stateActiveBackgroundColor;
            set
            {
                _stateActiveBackgroundColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EffectiveBackgroundBrush));
            }
        }

        private string _stateInactiveBackgroundColor = "#403F3F46";
        public string StateInactiveBackgroundColor
        {
            get => _stateInactiveBackgroundColor;
            set
            {
                _stateInactiveBackgroundColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EffectiveBackgroundBrush));
            }
        }

        private ButtonRuntimeState _runtimeState = ButtonRuntimeState.Unknown;
        private string _stateErrorMessage = "";

        public ButtonRuntimeState RuntimeState => _runtimeState;
        public bool IsStateActive => _runtimeState == ButtonRuntimeState.Active;

        public string EffectiveDisplayText => IsStateActive && !string.IsNullOrWhiteSpace(StateActiveDisplayText)
            ? StateActiveDisplayText
            : DisplayText;

        public string EffectiveImagePath => IsStateActive && !string.IsNullOrWhiteSpace(StateActiveImagePath)
            ? StateActiveImagePath
            : ImagePath;

        public Brush EffectiveBackgroundBrush
        {
            get
            {
                if (StateDisplayType == ButtonStateDisplayType.None || RuntimeState == ButtonRuntimeState.Unknown)
                {
                    return Brushes.Transparent;
                }

                string colorValue = RuntimeState == ButtonRuntimeState.Active
                    ? StateActiveBackgroundColor
                    : StateInactiveBackgroundColor;
                try
                {
                    Color color = (Color)ColorConverter.ConvertFromString(colorValue);
                    var brush = new SolidColorBrush(color);
                    brush.Freeze();
                    return brush;
                }
                catch
                {
                    return Brushes.Transparent;
                }
            }
        }

        public string StateStatusText
        {
            get
            {
                if (StateDisplayType == ButtonStateDisplayType.None)
                {
                    return null;
                }

                if (RuntimeState == ButtonRuntimeState.Unknown)
                {
                    return string.IsNullOrWhiteSpace(_stateErrorMessage)
                        ? $"{StateDisplayTypeDisplayName}: 確認中"
                        : $"{StateDisplayTypeDisplayName}: 取得できません（{_stateErrorMessage}）";
                }

                string stateLabel;
                switch (StateDisplayType)
                {
                    case ButtonStateDisplayType.ApplicationRunning:
                        stateLabel = RuntimeState == ButtonRuntimeState.Active ? "起動中" : "停止中";
                        break;
                    default:
                        stateLabel = RuntimeState == ButtonRuntimeState.Active ? "ミュート" : "ミュート解除";
                        break;
                }

                return $"{StateDisplayTypeDisplayName}: {stateLabel}";
            }
        }

        public void UpdateRuntimeState(ButtonRuntimeState state, string errorMessage = null)
        {
            string normalizedError = errorMessage ?? "";
            if (_runtimeState == state && _stateErrorMessage == normalizedError)
            {
                return;
            }

            _runtimeState = state;
            _stateErrorMessage = normalizedError;
            OnPropertyChanged(nameof(RuntimeState));
            OnPropertyChanged(nameof(IsStateActive));
            OnPropertyChanged(nameof(EffectiveDisplayText));
            OnPropertyChanged(nameof(EffectiveImagePath));
            OnPropertyChanged(nameof(EffectiveBackgroundBrush));
            OnPropertyChanged(nameof(StateStatusText));
        }

        public static bool IsRuntimeProperty(string propertyName)
        {
            return propertyName == nameof(RuntimeState) ||
                   propertyName == nameof(IsStateActive) ||
                   propertyName == nameof(EffectiveDisplayText) ||
                   propertyName == nameof(EffectiveImagePath) ||
                   propertyName == nameof(EffectiveBackgroundBrush) ||
                   propertyName == nameof(StateStatusText);
        }

        public ActionButton Clone()
        {
            return new ActionButton
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

        public ICommand ClickCommand { get; set; }        public event PropertyChangedEventHandler PropertyChanged;
        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
