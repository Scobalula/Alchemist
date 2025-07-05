using Alchemist.InverseKinematics;
using Microsoft.Win32;
using RedFox.Graphics3D;
using RedFox.Graphics3D.Skeletal;
using RedFox.Graphics3D.Translation;
using RedFox.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace Alchemist.UI
{
    public class MainViewModel : LoggableMVVMObject
    {
        public string? FileVersion { get; } = "0.0.0.7";
        public AnimationConverter Converter { get; set; } = new();
        public MVVMItemList<Animation> Animations { get; set; } = [];
        public MVVMItemList<Part> Parts { get; set; } = [];
        public MVVMItemList<Script> Scripts { get; set; } = [];

        [JsonIgnore]
        public HashSet<Animation> SelectedAnimations { get; set; } = [];
        [JsonIgnore]
        public HashSet<Part> SelectedParts { get; set; } = [];
        public HashSet<AnimationLayer> SelectedLayers { get; set; } = [];

        public List<Animation> Clipboard { get; set; } = [];

        public ICommand AddAnimationsCommand { get; set; }
        public ICommand SaveAnimationsCommand { get; set; }
        public ICommand SetOutputFolderCommand { get; set; }
        public ICommand RemoveAnimationsCommand { get; set; }
        public ICommand ToggleLeftHandIKCommand { get; set; }
        public ICommand ToggleRightHandIKCommand { get; set; }
        public ICommand SetPoseFileCommand { get; set; }
        public ICommand SetSkeletonFileCommand { get; set; }
        public ICommand AddLayerCommand { get; set; }
        public ICommand RemoveLayerCommand { get; set; }
        public ICommand MoveLayerCommand { get; set; }
        public ICommand ImportProjectCommand { get; set; }
        public ICommand SaveProjectCommand { get; set; }
        public ICommand SaveProjectAsCommand { get; set; }
        public ICommand AddPartsCommand { get; set; }
        public ICommand RemovePartsCommand { get; set; }

        public ICommand RunScriptCommand { get; set; }

        public int SelectedIndex { get; set; }

        public MVVMItemList<string> LayerTypes { get; set; } = [..Enum.GetNames<AnimationLayerType>()];
        public MVVMItemList<string> PartTypes { get; set; } = [..Enum.GetNames<PartType>()];

        public int FileColumnWith { get => GetValue(0); set => SetValue(value); }
        public int IKSettingsColumnWith { get => GetValue(0); set => SetValue(value); }
        public int LayersColumnWith { get => GetValue(0); set => SetValue(value); }
        public int OutputColumnWith { get => GetValue(0); set => SetValue(value); }

        public Action<string> OnCommandFailure { get; set; }
        public bool EnableAnimationTrimming { get; set; }
        public string LeftIKStartBoneName { get => GetValue(string.Empty); set => SetValue(value); }
        public string LeftIKMidBoneName { get => GetValue(string.Empty); set => SetValue(value); }
        public string LeftIKEndBoneName { get => GetValue(string.Empty); set => SetValue(value); }
        public string LeftIKTargetBoneName { get => GetValue(string.Empty); set => SetValue(value); }
        public string RightIKStartBoneName { get => GetValue(string.Empty); set => SetValue(value); }
        public string RightIKMidBoneName { get => GetValue(string.Empty); set => SetValue(value); }
        public string RightIKEndBoneName { get => GetValue(string.Empty); set => SetValue(value); }
        public string RightIKTargetBoneName { get => GetValue(string.Empty); set => SetValue(value); }
        public string OutputPrefix { get => GetValue(string.Empty); set => SetValue(value); }
        public string OutputSuffix { get => GetValue(string.Empty); set => SetValue(value); }

        public string? CurrentProjectFile { get => GetValue<string?>(null); set => SetValue(value); }

        public string Title => $"Alchemist | {CurrentProjectFile ?? "Untitled Batch"} | Version: {FileVersion ?? "Unknown"}";

        public string OutputFormat { get => GetValue(".cast"); set => SetValue(value); }

        public string[] OutputFormats { get; } = [ ".seanim", ".cast" ];

        public Visibility IsVisible { get => GetValue(Visibility.Visible); set => SetValue(value); }

        public bool MatchOldCallOfDuty { get => GetValue(false); set => SetValue(value); }

        public MainViewModel(Action<string> onCommandFailure, string appSettingsDirectory)
        {
            // Scripts
            Scripts.Add(new GenerateSprintAnimsScript());
            // Columns
            AutoAdjustColumns(1200);
            // Binds
            AddBinds(nameof(CurrentProjectFile), nameof(Title));
            // Callbacks
            OnCommandFailure = onCommandFailure;
            // Data
            EnableAnimationTrimming = false;
            LeftIKStartBoneName     = "j_shoulder_le";
            LeftIKMidBoneName       = "j_elbow_le";
            LeftIKEndBoneName       = "j_wrist_le";
            LeftIKTargetBoneName    = "tag_ik_loc_le";
            RightIKStartBoneName    = "j_shoulder_ri";
            RightIKMidBoneName      = "j_elbow_ri";
            RightIKEndBoneName      = "j_wrist_ri";
            RightIKTargetBoneName   = "tag_ik_loc_ri";
            // IO Commands
            AddAnimationsCommand = new ButtonCommand((obj) =>
            {
                // TODO: Move to a service for multi-plat/Avalonia/etc
                var dialog = new OpenFileDialog()
                {
                    Title = "Alchemist | Add Animations",
                    Filter = "All files (*.*)|*.*",
                    Multiselect = true,
                };

                if(dialog.ShowDialog() == true)
                {
                    foreach (var file in dialog.FileNames)
                    {
                        var anim = new Animation(file);
                        Animations.Add(anim);
                    }
                }
            });
            // Parts Commands
            AddPartsCommand = new ButtonCommand((obj) =>
            {
                // TODO: Move to a service for multi-plat/Avalonia/etc
                var dialog = new OpenFileDialog()
                {
                    Title = "Alchemist | Add Parts",
                    Filter = "All files (*.*)|*.*",
                    Multiselect = true,
                };

                if (dialog.ShowDialog() == true)
                {
                    foreach (var file in dialog.FileNames)
                    {
                        var model = new Part(this, file);
                        Parts.Add(model);
                    }
                }
            });
            SaveAnimationsCommand = new ButtonCommand((obj) =>
            {
                //var worker = new BackgroundWorker()
                //{
                //    WorkerSupportsCancellation = true,
                //    WorkerReportsProgress = true,
                //};

                //worker.ProgressChanged += Worker_ProgressChanged;
                //worker.DoWork += Worker_DoWork;
                //worker.RunWorkerAsync(Animations);

                var skeleton = AnimationConverter.LoadSkeletonFromParts(Parts, MatchOldCallOfDuty);

                AnimationConverter.TranslatorFactory.Save("Test.semodel", skeleton);

                foreach (var anim in Animations)
                {
                    IKSettings leftHandIKSettings = new(
                        LeftIKStartBoneName,
                        LeftIKMidBoneName,
                        LeftIKEndBoneName,
                        LeftIKTargetBoneName);
                    IKSettings rightHandIKSettings = new(
                        RightIKStartBoneName,
                        RightIKMidBoneName,
                        RightIKEndBoneName,
                        RightIKTargetBoneName);

                    if (!string.IsNullOrWhiteSpace(anim.LeftIKTargetBoneName))
                        leftHandIKSettings.TargetBoneName = anim.LeftIKTargetBoneName;
                    if (!string.IsNullOrWhiteSpace(anim.RightIKTargetBoneName))
                        rightHandIKSettings.TargetBoneName = anim.RightIKTargetBoneName;


                    AnimationConverter.Convert(skeleton, anim, leftHandIKSettings, rightHandIKSettings, OutputPrefix, OutputSuffix, OutputFormat, MatchOldCallOfDuty);
                }

                MessageBox.Show("Animation Export Complete", "Alchemist | Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            });
            RemoveAnimationsCommand = new ButtonCommand((obj) =>
            {
                var removal = new List<Animation>();

                foreach (var anim in SelectedAnimations)
                    removal.Add(anim);

                removal.ForEach(x => Animations.Remove(x));
            });
            RemovePartsCommand = new ButtonCommand((obj) =>
            {
                var removal = new List<Part>();

                foreach (var part in SelectedParts)
                    removal.Add(part);

                removal.ForEach(x => Parts.Remove(x));
            });
            SetOutputFolderCommand = new ButtonCommand((obj) =>
            {
                var outputDialog = new OpenFolderDialog()
                {
                    Title = "Alchemist | Select Output Folder",
                };

                if (outputDialog.ShowDialog() == false)
                    return;

                foreach (var anim in SelectedAnimations)
                    anim.OutputFolder = outputDialog.FolderName;
            });
            // IK Commands
            ToggleLeftHandIKCommand = new ButtonCommand((obj) =>
            {
                var value = "Enable".Equals(obj);

                foreach (var anim in SelectedAnimations)
                    anim.EnableLeftHandIK = value;
            });
            ToggleRightHandIKCommand = new ButtonCommand((obj) =>
            {
                var value = "Enable".Equals(obj);

                foreach (var anim in SelectedAnimations)
                    anim.EnableRightHandIK = value;
            });
            SetPoseFileCommand = new ButtonCommand((obj) =>
            {
                // TODO: Move to a service for multi-plat/Avalonia/etc
                var dialog = new OpenFileDialog()
                {
                    Title = $"Alchemist | Select {obj} Pose File",
                    Filter = "All files (*.*)|*.*",
                    Multiselect = false,
                };

                if(dialog.ShowDialog() == true)
                {
                    var isLeft = "Left".Equals(obj);

                    foreach (var anim in SelectedAnimations)
                    {
                        if(isLeft)
                        {
                            anim.LeftHandPoseFile = dialog.FileName;
                        }
                        else
                        {
                            anim.RightHandPoseFile = dialog.FileName;
                        }
                    }
                }
            });
            // Layers
            AddLayerCommand = new ButtonCommand((obj) =>
            {
                // TODO: Move to a service for multi-plat/Avalonia/etc
                var dialog = new OpenFileDialog()
                {
                    Title = "Alchemist | Add Animations",
                    Filter = "All files (*.*)|*.*",
                    Multiselect = true,
                };

                if (dialog.ShowDialog() == true)
                {
                    foreach (var file in dialog.FileNames)
                    {
                        foreach (var anim in SelectedAnimations)
                        {
                            anim.Layers.Add(new(file, anim));
                        }
                    }
                }
            });
            RemoveLayerCommand = new ButtonCommand((obj) =>
            {
                var removal = new List<AnimationLayer>();

                foreach (var anim in SelectedLayers)
                {
                    if (anim.Owner is null)
                        continue;

                    anim.Owner.Layers.Remove(anim);
                    removal.Add(anim);
                }

                removal.ForEach(x => SelectedLayers.Remove(x));
            });
            MoveLayerCommand = new ButtonCommand((obj) =>
            {
                var value = "Up".Equals(obj);

                // Cannot move multiple
                if (SelectedLayers.Count > 1)
                    return;

                foreach (var anim in SelectedLayers)
                {
                    if (anim.Owner is null)
                        continue;

                    var idx = anim.Owner.Layers.IndexOf(anim);

                    if (idx == 0 && value)
                        continue;
                    if (idx == anim.Owner.Layers.Count - 1 && !value)
                        continue;

                    var newIdx = value ? idx - 1 : idx + 1;
                    var currAnim = anim.Owner.Layers[newIdx];

                    anim.Owner.Layers[newIdx] = anim;
                    anim.Owner.Layers[idx] = currAnim;
                }
            });
            ImportProjectCommand = new ButtonCommand((obj) =>
            {
                // TODO: Move to a service for multi-plat/Avalonia/etc
                var dialog = new OpenFileDialog()
                {
                    Title = "Alchemist | Import Project",
                    Filter = "All files (*.*)|*.*",
                    Multiselect = true,
                };

                if (dialog.ShowDialog() == true)
                {
                    LoadProjectFile(dialog.FileName);
                }
            });
            SaveProjectCommand = new ButtonCommand((obj) =>
            {
                if (string.IsNullOrWhiteSpace(CurrentProjectFile) || (obj is bool saveAs && saveAs))
                {
                    // TODO: Move to a service for multi-plat/Avalonia/etc
                    var dialog = new SaveFileDialog()
                    {
                        Title = "Alchemist | Save Project",
                        Filter = "Alchemist project files (*.aprj)|*.aprj",
                    };

                    if (dialog.ShowDialog() == false)
                        return;

                    CurrentProjectFile = dialog.FileName;

                    SaveProject(this, dialog.FileName);
                }
                else
                {
                    SaveProject(this, CurrentProjectFile);
                }
            });
            SaveProjectAsCommand = new ButtonCommand((obj) =>
            {
                // TODO: Move to a service for multi-plat/Avalonia/etc
                var dialog = new SaveFileDialog()
                {
                    Title = "Alchemist | Save Project",
                    Filter = "Alchemist project files (*.aprj)|*.aprj",
                };

                if (dialog.ShowDialog() == false)
                    return;

                CurrentProjectFile = dialog.FileName;

                SaveProject(this, dialog.FileName);
            });
            RunScriptCommand = new ButtonCommand((obj) =>
            {
                if (obj is not Script script)
                    return;

                script.Run(this);
            });
        }

        public void LoadProjectFile(string filePath)
        {
            Logging.Logger.Info($"Loading project file: {filePath}");

            var prjFile = JsonSerializer.Deserialize<AlchemistProjectFile>(File.ReadAllText(filePath), new JsonSerializerOptions()
            {
                WriteIndented = true,
                ReferenceHandler = ReferenceHandler.Preserve
            });

            if (prjFile is null)
                return;

            EnableAnimationTrimming = prjFile.EnableAnimationTrimming;
            LeftIKStartBoneName     = prjFile.LeftIKStartBoneName;
            LeftIKMidBoneName       = prjFile.LeftIKMidBoneName;
            LeftIKEndBoneName       = prjFile.LeftIKEndBoneName;
            LeftIKTargetBoneName    = prjFile.LeftIKTargetBoneName;
            RightIKStartBoneName    = prjFile.RightIKStartBoneName;
            RightIKMidBoneName      = prjFile.RightIKMidBoneName;
            RightIKEndBoneName      = prjFile.RightIKEndBoneName;
            RightIKTargetBoneName   = prjFile.RightIKTargetBoneName;
            OutputPrefix            = prjFile.OutputPrefix;
            OutputSuffix            = prjFile.OutputSuffix;
            OutputFormat            = prjFile.OutputFormat;
            MatchOldCallOfDuty      = prjFile.MatchOldCallOfDuty;

            CurrentProjectFile = filePath;

            Animations.Clear();
            Parts.Clear();

            if (prjFile.Animations is not null)
            {
                foreach (var anim in prjFile.Animations)
                {
                    Animations.Add(anim);
                }
            }

            if (prjFile.Parts is not null)
            {
                foreach (var part in prjFile.Parts)
                {
                    Parts.Add(part);

                    part.Owner = this;
                }
            }

            Logging.Logger.Info($"Loaded project file: {filePath}");
        }

        private void Worker_DoWork(object? sender, DoWorkEventArgs e)
        {
            if (sender is not BackgroundWorker worker)
                return;
            if (e.Argument is not MVVMItemList<Animation> animations)
                return;

            // Build IK Settings
            IKSettings leftHandIKSettings = new(
                LeftIKStartBoneName,
                LeftIKMidBoneName,
                LeftIKEndBoneName,
                LeftIKTargetBoneName);
            IKSettings rightHandIKSettings = new(
                RightIKStartBoneName,
                RightIKMidBoneName,
                RightIKEndBoneName,
                RightIKTargetBoneName);


            //foreach (var anim in animations)
            //{
            //    Converter.Convert(anim, leftHandIKSettings, rightHandIKSettings, OutputPrefix, OutputSuffix, OutputFormat);
            //    worker.ReportProgress(0);
            //}
        }

        private void Worker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            Debug.WriteLine(e.ProgressPercentage);
        }

        public static void SaveProject(MainViewModel viewModel, string? fileName)
        {
            Logging.Logger.Info($"Saving project file: {fileName}");

            if (fileName is null)
                return;

            var prjFile = new AlchemistProjectFile()
            {
                EnableAnimationTrimming = viewModel.EnableAnimationTrimming,
                LeftIKStartBoneName = viewModel.LeftIKStartBoneName,
                LeftIKMidBoneName = viewModel.LeftIKMidBoneName,
                LeftIKEndBoneName = viewModel.LeftIKEndBoneName,
                LeftIKTargetBoneName = viewModel.LeftIKTargetBoneName,
                RightIKStartBoneName = viewModel.RightIKStartBoneName,
                RightIKMidBoneName = viewModel.RightIKMidBoneName,
                RightIKEndBoneName = viewModel.RightIKEndBoneName,
                RightIKTargetBoneName = viewModel.RightIKTargetBoneName,
                OutputPrefix = viewModel.OutputPrefix,
                OutputSuffix = viewModel.OutputSuffix,
                OutputFormat = viewModel.OutputFormat,
                MatchOldCallOfDuty = viewModel.MatchOldCallOfDuty,
                Animations = [.. viewModel.Animations],
                Parts = [.. viewModel.Parts]
            };

            File.WriteAllText(fileName, JsonSerializer.Serialize(prjFile, new JsonSerializerOptions()
            {
                WriteIndented = true,
                ReferenceHandler = ReferenceHandler.Preserve
            }));

            Logging.Logger.Info($"Saved project file: {fileName}");
        }

        public void AutoAdjustColumns(double width)
        {
            Logging.Logger.Info($"Auto-adjusting columns for width: {width}");

            FileColumnWith = (int)(width * 0.26);
            IKSettingsColumnWith = (int)(width * 0.13);
            LayersColumnWith = (int)(width * 0.33);
            OutputColumnWith = (int)(width * 0.24);
        }

        public void Undo()
        {

        }

        public void Redo()
        {

        }

        public bool TryFindAnimation(string searchString, [NotNullWhen(true)] out Animation? animation)
        {
            animation = null;

            foreach (var anim in Animations)
            {
                if (FileSystemName.MatchesSimpleExpression(searchString, anim.Name, true))
                {
                    animation = anim;
                    return true;
                }
            }

            return false;
        }
    }
}
