using RedFox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Alchemist.UI
{
    public class Part : MVVMObject
    {
        [JsonIgnore]
        public MainViewModel? Owner { get; set; }

        public string FilePath { get => GetValue(string.Empty); set => SetValue(value); }
        public string ParentBoneTag { get => GetValue(string.Empty); set => SetValue(value); }

        public PartType Type { get => GetValue(PartType.Attachment); set => SetValue(value); }

        [JsonIgnore]
        public ICommand MoveUpCommand { get; set; }
        [JsonIgnore]
        public ICommand MoveDownCommand { get; set; }
        [JsonIgnore]
        public ICommand DeleteCommand { get; set; }

        public Part()
        {
            MoveUpCommand = new ButtonCommand((param) => Owner?.Parts.MoveToPrevious(this));
            MoveDownCommand = new ButtonCommand((param) => Owner?.Parts.MoveToNext(this));
            DeleteCommand = new ButtonCommand((param) => Owner?.Parts.Remove(this));
        }

        public Part(MainViewModel? owner, string filePath) : this()
        {
            Owner = owner;
            FilePath = filePath;
        }
    }
}
