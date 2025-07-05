using RedFox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace Alchemist.UI
{
    public class AnimationLayer : LoggableMVVMObject
    {
        public Animation? Owner { get; set; }
        public string Name { get => GetValue(string.Empty); set => SetValue(value); }
        public int? Offset { get => GetValue((int?)null); set => SetValue(value); }
        public int Color { get; set; }

        [JsonIgnore]
        public ICommand MoveUpCommand { get; set; }
        [JsonIgnore]
        public ICommand MoveDownCommand { get; set; }
        [JsonIgnore]
        public ICommand DeleteCommand { get; set; }

        public AnimationLayerType Type { get => GetValue(AnimationLayerType.Additive); set => SetValue(value); }
        [JsonIgnore]
        public Brush ColorBrush
        { 
            get
            {
                var r = Math.Clamp((byte)((Color >> 00) & 0xFF), (byte)0, (byte)128);
                var g = Math.Clamp((byte)((Color >> 08) & 0xFF), (byte)0, (byte)128);
                var b = Math.Clamp((byte)((Color >> 16) & 0xFF), (byte)0, (byte)128);

                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
            }
        } 

        public AnimationLayer()
        {
            MoveUpCommand = new ButtonCommand((param) => Owner?.Layers.MoveToPrevious(this));
            MoveDownCommand = new ButtonCommand((param) => Owner?.Layers.MoveToNext(this));
            DeleteCommand = new ButtonCommand((param) => Owner?.Layers.Remove(this));
            Color = GetHashCode();
        }

        public AnimationLayer(string name, Animation owner) : this()
        {
            Name = name;
            Owner = owner;
        }
    }
}
