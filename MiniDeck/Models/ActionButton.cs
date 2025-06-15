using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input; // ICommandのため

namespace MiniDeck.Models
{
    public class ActionButton : INotifyPropertyChanged
    {
        private string _displayText;
        public string DisplayText
        {
            get => _displayText;
            set { _displayText = value; OnPropertyChanged(); }
        }

        private string _imagePath;        
        public string ImagePath // ボタンの画像パス (例: "/Resources/Icons/my_icon.png")
        {
            get => _imagePath;
            set { _imagePath = value; OnPropertyChanged(); }
        }        private ActionType _actionType;
        public ActionType ActionType 
        { 
            get => _actionType;
            set { _actionType = value; OnPropertyChanged(); }
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

        public ICommand ClickCommand { get; set; }        public event PropertyChangedEventHandler PropertyChanged;
        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}