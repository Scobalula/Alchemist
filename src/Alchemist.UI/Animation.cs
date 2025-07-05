using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RedFox.UI;

namespace Alchemist.UI
{
    public class Animation : LoggableMVVMObject
    {
        public float OutputFramerate { get => GetValue(30.0f); set => SetValue(value); }

        public string Name { get => GetValue(string.Empty); set => SetValue(value); }
        public string OutputName { get => GetValue(string.Empty); set => SetValue(value); }
        public string OutputFolder { get => GetValue(string.Empty); set => SetValue(value); }

        public bool EnableLeftHandIK { get => GetValue(true); set => SetValue(value); }
        public bool EnableRightHandIK { get => GetValue(true); set => SetValue(value); }
        public bool UseExperimentalFeatures { get => GetValue(true); set => SetValue(value); }

        public string LeftHandPoseFile { get => GetValue(string.Empty); set => SetValue(value); }
        public string RightHandPoseFile { get => GetValue(string.Empty); set => SetValue(value); }

        public string LeftIKTargetBoneName { get => GetValue(string.Empty); set => SetValue(value); }
        public string RightIKTargetBoneName { get => GetValue(string.Empty); set => SetValue(value); }

        public ObservableCollection<AnimationLayer> Layers { get; set; } = [];

        public Animation()
        {
            
        }

        public Animation(string name)
        {
            Name = name;
            OutputName = Path.GetFileNameWithoutExtension(Name);
            OutputFolder = Path.GetDirectoryName(Name) ?? string.Empty;
        }

        public override string ToString() => Path.GetFileNameWithoutExtension(OutputName);

        public Animation Clone()
        {
            var newAnim = new Animation()
            {
                Name = Name,
                OutputName = OutputName,
                OutputFolder = OutputFolder,
                EnableLeftHandIK  = EnableLeftHandIK,
                EnableRightHandIK = EnableLeftHandIK,
                UseExperimentalFeatures = UseExperimentalFeatures,
                LeftHandPoseFile = LeftHandPoseFile,
                RightHandPoseFile = RightHandPoseFile,
                OutputFramerate = OutputFramerate,
            };

            foreach (var layer in Layers)
            {
                newAnim.Layers.Add(new()
                {
                    Name = layer.Name,
                    Owner = newAnim,
                    Type = layer.Type
                });
            }

            return newAnim;
        }
    }
}
