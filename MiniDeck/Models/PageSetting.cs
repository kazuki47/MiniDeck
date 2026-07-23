using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace MiniDeck.Models
{
    [Serializable]
    public class PageSetting : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString("N");

        [XmlElement("Id")]
        public string Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _name = "新しいページ";

        [XmlElement("Name")]
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        [XmlArray("Buttons")]
        [XmlArrayItem("ButtonSetting")]
        public List<ButtonSetting> Buttons { get; set; } = new List<ButtonSetting>();

        public PageSetting Clone(string id = null, string name = null)
        {
            return new PageSetting
            {
                Id = id ?? Id,
                Name = name ?? Name,
                Buttons = (Buttons ?? new List<ButtonSetting>())
                    .Select(button => button?.Clone() ?? new ButtonSetting())
                    .ToList()
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
