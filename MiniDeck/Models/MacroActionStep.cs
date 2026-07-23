using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace MiniDeck.Models
{
    public enum MacroFailureBehavior
    {
        Stop,
        Continue
    }

    public enum MacroExecutionState
    {
        Idle,
        Running,
        Succeeded,
        Failed
    }

    [Serializable]
    public class MacroActionStep : INotifyPropertyChanged
    {
        private ActionType _actionType = ActionType.KeyPress;
        private string _shortcutKeySequence = "";
        private string _applicationPath = "";
        private string _applicationArguments = "";
        private string _url = "";
        private int _delayAfterMilliseconds;

        [XmlElement("ActionType")]
        public ActionType ActionType
        {
            get => _actionType;
            set
            {
                if (_actionType == value) return;
                _actionType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplaySummary));
            }
        }

        [XmlElement("ShortcutKeySequence")]
        public string ShortcutKeySequence
        {
            get => _shortcutKeySequence;
            set
            {
                if (_shortcutKeySequence == value) return;
                _shortcutKeySequence = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplaySummary));
            }
        }

        [XmlElement("ApplicationPath")]
        public string ApplicationPath
        {
            get => _applicationPath;
            set
            {
                if (_applicationPath == value) return;
                _applicationPath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplaySummary));
            }
        }

        [XmlElement("ApplicationArguments")]
        public string ApplicationArguments
        {
            get => _applicationArguments;
            set
            {
                if (_applicationArguments == value) return;
                _applicationArguments = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplaySummary));
            }
        }

        [XmlElement("Url")]
        public string Url
        {
            get => _url;
            set
            {
                if (_url == value) return;
                _url = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplaySummary));
            }
        }

        [XmlElement("DelayAfterMilliseconds")]
        public int DelayAfterMilliseconds
        {
            get => _delayAfterMilliseconds;
            set
            {
                if (_delayAfterMilliseconds == value) return;
                _delayAfterMilliseconds = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplaySummary));
            }
        }

        [XmlIgnore]
        public string DisplaySummary
        {
            get
            {
                string actionSummary;
                switch (ActionType)
                {
                    case ActionType.KeyPress:
                        actionSummary = $"キー: {ShortcutKeySequence}";
                        break;
                    case ActionType.LaunchApplication:
                        actionSummary = $"開く: {ApplicationPath}";
                        break;
                    case ActionType.OpenUrl:
                        actionSummary = $"URL: {Url}";
                        break;
                    default:
                        actionSummary = "未設定のアクション";
                        break;
                }

                return DelayAfterMilliseconds > 0
                    ? $"{actionSummary} → {DelayAfterMilliseconds}ms 待機"
                    : actionSummary;
            }
        }

        public MacroActionStep Clone()
        {
            return new MacroActionStep
            {
                ActionType = ActionType,
                ShortcutKeySequence = ShortcutKeySequence,
                ApplicationPath = ApplicationPath,
                ApplicationArguments = ApplicationArguments,
                Url = Url,
                DelayAfterMilliseconds = DelayAfterMilliseconds
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
