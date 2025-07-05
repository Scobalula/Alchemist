using Alchemist.InverseKinematics;
using RedFox.Graphics3D;
using RedFox.Graphics3D.Skeletal;
using RedFox.Graphics3D.Translation;
using Spectre.Console;
using System.CommandLine.Rendering;
using System.Numerics;
using System.Text;
namespace Alchemist.CLI
{
    internal class Program
    {
        public static Model LoadModergedModels(Graphics3DTranslatorFactory translatorFactory, string skeletonFolder)
        {
            if(File.Exists(skeletonFolder))
            {
                return translatorFactory.Load<Model>(skeletonFolder);
            }
            else
            {
                List<Model> loadedModels = [];
                foreach (var file in Directory.EnumerateFiles(skeletonFolder))
                {
                    loadedModels.Add(translatorFactory.Load<Model>(file));
                }

                if (!ModelMerger.TryGetRootModel(loadedModels, out Model? root))
                    throw new Exception("Failed to resolve root model for the skeleton");

                ModelMerger.Merge(root, loadedModels);

                return root;
            }
        }

        static Program() => Console.OutputEncoding = Encoding.UTF8;

        static IKTwoBoneSolver? CreateIKSolver(string name, IKSettings settings, Skeleton skeleton, AnimationPlayer player)
        {
            AnsiConsole.MarkupLine($"Creating IK solver: {name}....");

            if (!skeleton.TryGetBone(settings.StartBoneName, out var startBone))
            {
                AnsiConsole.MarkupLine($"[red]WARNING: Could not find start bone: {settings.StartBoneName} for IK solver: {name}[/]");
                return null;
            }
            if (!skeleton.TryGetBone(settings.MiddleBoneName, out var middleBone))
            {
                AnsiConsole.MarkupLine($"[red]WARNING: Could not find start bone: {settings.MiddleBoneName} for IK solver: {name}[/]");
                return null;
            }
            if (!skeleton.TryGetBone(settings.EndBoneName, out var endBone))
            {
                AnsiConsole.MarkupLine($"[red]WARNING: Could not find start bone: {settings.EndBoneName} for IK solver: {name}[/]");
                return null;
            }
            if (!skeleton.TryGetBone(settings.TargetBoneName, out var targetBone))
            {
                AnsiConsole.MarkupLine($"[red]WARNING: Could not find start bone: {settings.TargetBoneName} for IK solver: {name}[/]");
                return null;
            }

            var solver = new IKTwoBoneSolver(name, startBone, middleBone, endBone, targetBone);
            player.Solvers.Add(solver);
            AnsiConsole.MarkupLine($"Created IK solver: {name} successfully");

            return solver;
        }

        static SkeletonAnimationSampler LoadAnimation(string name, string filePath, AnimationPlayer player, Skeleton skeleton, Graphics3DTranslatorFactory factory, bool forceAdditive)
        {
            AnsiConsole.MarkupLine($"Loading animation: {filePath}....");
            var anim = factory.Load<SkeletonAnimation>(filePath);
            if (forceAdditive)
                anim.TransformType = TransformType.Additive;
            var mainSampler = new SkeletonAnimationSampler(name, anim, skeleton);
            player.AddLayer(mainSampler);
            AnsiConsole.MarkupLine($"Loaded animation: {filePath} successfully.");
            return mainSampler;
        }

        static void ProcessAnimations(IKSettings leftHandIKSettings, IKSettings rightHandIKSettings, ConversionSettings conversionSettings)
        {
            if (conversionSettings.InputAnimationFile is null)
                throw new ArgumentException("No input file was provided to alchemist for main animation. Pass -h/-?/--help for more info.");
            if (conversionSettings.SkeletonPath is null)
                throw new ArgumentException("No file/directory path was provided to alchemist for skeleton. Pass -h/-?/--help for more info.");
            if (conversionSettings.OutputFile is null)
                throw new ArgumentException("No output file was provided to alchemist for output animation. Pass -h/-?/--help for more info.");

            var translatorFactory = new Graphics3DTranslatorFactory().WithDefaultTranslators();

            AnsiConsole.MarkupLine("Starting up the processor.... :pleading_face:");

            var model = LoadModergedModels(translatorFactory, conversionSettings.SkeletonPath);
            var skeleton = model.Skeleton ?? throw new ArgumentException("Failed to locate a skeleton in the loaded model. Please provide model/s with a skeleton/bones.");
            var player = new AnimationPlayer("Player");

            AnimationSampler? plSampler = null;
            AnimationSampler? prSampler = null;
            AnimationSampler? glSampler = null;
            AnimationSampler? grSampler = null;

            AnimationSamplerSolver? lSolver = null;
            AnimationSamplerSolver? rSolver = null;

            var mainSampler = LoadAnimation("Main", conversionSettings.InputAnimationFile, player, skeleton, translatorFactory, false);

            // Load optional animations
            if (conversionSettings.EnableLeftHandPose && conversionSettings.LeftHandPoseFile is not null)
                plSampler = LoadAnimation("PLLayer", conversionSettings.LeftHandPoseFile, player, skeleton, translatorFactory, conversionSettings.ForceAdditive);
            if (conversionSettings.EnableRightHandPose && conversionSettings.RightHandPoseFile is not null)
                prSampler = LoadAnimation("PRLayer", conversionSettings.RightHandPoseFile, player, skeleton, translatorFactory, conversionSettings.ForceAdditive);
            if (conversionSettings.GestureLayerFile is not null)
                glSampler = LoadAnimation("GLLayer", conversionSettings.GestureLayerFile, player, skeleton, translatorFactory, false);
            if (conversionSettings.GestureLayerRightFile is not null)
                grSampler = LoadAnimation("GRLayer", conversionSettings.GestureLayerRightFile, player, skeleton, translatorFactory, conversionSettings.ForceAdditive);

            // Build additive layers if we have any
            if (conversionSettings.AdditiveLayerFiles is not null)
            {
                foreach (var additiveLayerFile in conversionSettings.AdditiveLayerFiles)
                {
                    AnsiConsole.MarkupLine($"Loading extra additive layer animation: {additiveLayerFile}....");
                    var additiveAnimation = translatorFactory.Load<SkeletonAnimation>(additiveLayerFile);
                    if (conversionSettings.ForceAdditive)
                        additiveAnimation.TransformType = TransformType.Additive;
                    player.AddLayer(new SkeletonAnimationSampler("!ADDITIVE!" + additiveLayerFile, additiveAnimation, skeleton));
                    AnsiConsole.MarkupLine($"Loaded animation: {additiveLayerFile} successfully.");
                }
            }

            AnsiConsole.MarkupLine($"Creating IK solvers....");

            if (conversionSettings.EnableLeftHandIK)
                lSolver = CreateIKSolver("LSolver", leftHandIKSettings, skeleton, player);
            if (conversionSettings.EnableRightHandIK)
                rSolver = CreateIKSolver("RSolver", rightHandIKSettings, skeleton, player);

            AnsiConsole.MarkupLine($"Building weights from notetracks...");

            foreach (var sampler in player.Layers)
            {
                if (sampler.Animation.Actions is null)
                    continue;

                foreach (var note in sampler.Animation.Actions)
                {
                    if (note.Name == "gesture_blend_in")
                    {
                        glSampler?.Weights.Add(new(0, 0));
                        grSampler?.Weights.Add(new(0, 0));
                    }
                }

                foreach (var note in sampler.Animation.Actions)
                {
                    switch (note.Name)
                    {
                        case "ik_out_start_left_hand":
                        case "ik_in_end_left_hand":
                            note.KeyFrames.ForEach(x => lSolver?.Weights.Add(new(x.Frame, 1)));
                            break;
                        case "ik_in_start_left_hand":
                        case "ik_out_end_left_hand":
                            note.KeyFrames.ForEach(x => lSolver?.Weights.Add(new(x.Frame, 0)));
                            break;
                        case "ik_out_start_right_hand":
                        case "ik_in_end_right_hand":
                            note.KeyFrames.ForEach(x => rSolver?.Weights.Add(new(x.Frame, 1)));
                            break;
                        case "ik_in_start_right_hand":
                        case "ik_out_end_right_hand":
                            note.KeyFrames.ForEach(x => rSolver?.Weights.Add(new(x.Frame, 0)));
                            break;
                        case "fingers_out_start_left_hand":
                        case "fingers_in_end_left_hand":
                            note.KeyFrames.ForEach(x => plSampler?.Weights.Add(new(x.Frame, 1)));
                            break;
                        case "fingers_in_start_left_hand":
                        case "fingers_out_end_left_hand":
                            note.KeyFrames.ForEach(x => plSampler?.Weights.Add(new(x.Frame, 0)));
                            break;
                        case "fingers_out_start_right_hand":
                        case "fingers_in_end_right_hand":
                            note.KeyFrames.ForEach(x => prSampler?.Weights.Add(new(x.Frame, 1)));
                            break;
                        case "fingers_in_start_right_hand":
                        case "fingers_out_end_right_hand":
                            note.KeyFrames.ForEach(x => prSampler?.Weights.Add(new(x.Frame, 0)));
                            break;
                        case "gesture_blend_in":
                            note.KeyFrames.ForEach(x =>
                            {
                                glSampler?.Weights.Add(new(x.Frame, 1));
                                grSampler?.Weights.Add(new(x.Frame, 1));
                            });
                            break;
                        case "gesture_blend_out_start":
                            note.KeyFrames.ForEach(x =>
                            {
                                glSampler?.Weights.Add(new(x.Frame, 1));
                                grSampler?.Weights.Add(new(x.Frame, 1));
                            });
                            break;
                        case "gesture_blend_out_end":
                            note.KeyFrames.ForEach(x =>
                            {
                                glSampler?.Weights.Add(new(x.Frame, 0));
                                grSampler?.Weights.Add(new(x.Frame, 0));
                            });
                            break;
                    }
                }
            }

            AnsiConsole.MarkupLine($"Built weights from notetracks successfully");


            plSampler?.Weights.Sort((x, y) => x.Frame.CompareTo(y.Frame));
            prSampler?.Weights.Sort((x, y) => x.Frame.CompareTo(y.Frame));
            grSampler?.Weights.Sort((x, y) => x.Frame.CompareTo(y.Frame));
            glSampler?.Weights.Sort((x, y) => x.Frame.CompareTo(y.Frame));
            lSolver?.Weights.Sort((x, y) => x.Frame.CompareTo(y.Frame));
            rSolver?.Weights.Sort((x, y) => x.Frame.CompareTo(y.Frame));

            AnsiConsole.MarkupLine($"Creating final animation....");
            AnsiConsole.MarkupLine($"Key frames: {player.FrameCount}");

            var newAnim = new SkeletonAnimation(conversionSettings.OutputFile, skeleton)
            {
                TransformType = TransformType.Absolute,
                Framerate = conversionSettings.FrameRate,
            };

            for (int i = 0; i < player.FrameCount; i++)
            {
                skeleton.InitializeAnimationTransforms();
                player.Update(i, AnimationSampleType.AbsoluteFrameTime);

                for (int k = 0; k < skeleton.Bones.Count; k++)
                {
                    newAnim.Targets[k].AddTranslationFrame(i, skeleton!.Bones[k].LocalTranslation);
                    newAnim.Targets[k].AddRotationFrame(i, skeleton!.Bones[k].LocalRotation);
                }
            }

            AnsiConsole.MarkupLine($"Saving animation to: {conversionSettings.OutputFile}....");

            translatorFactory.Save("test.semodel", model);
            translatorFactory.Save(conversionSettings.OutputFile, newAnim);

            AnsiConsole.MarkupLine($"Saved animation to: {conversionSettings.OutputFile} successfully");
        }

        /// <summary>
        /// A tool for processing additive/IK animations.
        /// </summary>
        /// <param name="skeletonPath">The path to the skeleton. Can be a file path or a directory path.</param>
        /// <param name="outputFile">The file path to write the resulting animation to.</param>
        /// <param name="inputFile">The file path of the base input animation.</param>
        /// <param name="leftHandPoseFile">The file path of the left hand pose animation.</param>
        /// <param name="rightHandPoseFile">The file path of the left hand pose animation.</param>
        /// <param name="additiveLayerFile">The file paths of any additive layers to appply.</param>
        /// <param name="gestureLayerFile">The file paths of any gesture layers to appply.</param>
        /// <param name="gestureLayerRightFile">The file paths of any gesture layers to appply.</param>
        /// <param name="forceAdditive">Whether or not to force additive mode for additive layers/pose animations.</param>
        /// <param name="enableLeftHandIK">Whether or not to enable left hand IK.</param>
        /// <param name="enableRightHandIK">Whether or not to enable right hand IK.</param>
        /// <param name="enableLeftHandPose">Whether or not to enable left hand pose.</param>
        /// <param name="enableRightHandPose">Whether or not to enable right hand pose.</param>
        /// <param name="outputFrameRate">The framerate of the output file.</param>
        /// <param name="leftIKStartBoneName">The name of the starting bone in the left hand IK chain.</param>
        /// <param name="leftIKMidBoneName">The name of the middle bone in the left hand IK chain.</param>
        /// <param name="leftIKEndBoneName">The name of the end bone in the left hand IK chain.</param>
        /// <param name="leftIKTargetBoneName">The name of the target bone in the left hand IK chain.</param>
        /// <param name="rightIKStartBoneName">The name of the starting bone in the right hand IK chain.</param>
        /// <param name="rightIKMidBoneName">The name of the middle bone in the right hand IK chain.</param>
        /// <param name="rightIKEndBoneName">The name of the end bone in the right hand IK chain.</param>
        /// <param name="rightIKTargetBoneName">The name of the target bone in the right hand IK chain.</param>
        /// <returns></returns>
        static int Main(string? skeletonPath, 
                        string? outputFile,
                        string? inputFile,
                        string? leftHandPoseFile,
                        string? rightHandPoseFile,
                        string[]? additiveLayerFile,
                        string? gestureLayerFile,
                        string? gestureLayerRightFile,
                        bool forceAdditive = true,
                        bool enableLeftHandIK = true,
                        bool enableRightHandIK = true,
                        bool enableLeftHandPose = true,
                        bool enableRightHandPose = true,
                        float outputFrameRate = 30.0f,
                        string leftIKStartBoneName = "j_shoulder_le",
                        string leftIKMidBoneName = "j_elbow_le",
                        string leftIKEndBoneName = "j_wrist_le",
                        string leftIKTargetBoneName = "tag_ik_loc_le",
                        string rightIKStartBoneName = "j_shoulder_ri",
                        string rightIKMidBoneName = "j_elbow_ri",
                        string rightIKEndBoneName = "j_wrist_ri",
                        string rightIKTargetBoneName = "tag_ik_loc_ri")
        {
            AnsiConsole.Write(new FigletText("Alchemist").LeftJustified().Color(Color.Blue));

            IKSettings leftHandIKSettings = new(leftIKStartBoneName, leftIKMidBoneName, leftIKEndBoneName, leftIKTargetBoneName);
            IKSettings rightHandIKSettings = new(rightIKStartBoneName, rightIKMidBoneName, rightIKEndBoneName, rightIKTargetBoneName);

            ConversionSettings conversionSettings = new()
            {
                InputAnimationFile    = inputFile,
                OutputFile            = outputFile,
                SkeletonPath          = skeletonPath,
                LeftHandPoseFile      = leftHandPoseFile,
                RightHandPoseFile     = rightHandPoseFile,
                AdditiveLayerFiles    = additiveLayerFile,
                GestureLayerFile      = gestureLayerFile,
                GestureLayerRightFile = gestureLayerRightFile,
                ForceAdditive         = forceAdditive,
                EnableLeftHandIK      = enableLeftHandIK,
                EnableRightHandIK     = enableRightHandIK,
                EnableLeftHandPose    = enableLeftHandPose,
                EnableRightHandPose   = enableRightHandPose,
                FrameRate             = outputFrameRate
            };

            AnsiConsole.Status().Spinner(Spinner.Known.Point)
                                .SpinnerStyle(Style.Parse("green"))
                                .Start("Working...", ctx =>
                                {
                                    try
                                    {
                                        ProcessAnimations(leftHandIKSettings, rightHandIKSettings, conversionSettings);
                                    }
                                    catch (Exception e)
                                    {
                                        AnsiConsole.WriteException(e);
                                    }
                                });

            return 0;
        }
    }
}
