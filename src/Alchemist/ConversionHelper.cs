using Alchemist.InverseKinematics;
using RedFox.Graphics3D.Skeletal;
using RedFox.Graphics3D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Alchemist
{
    public class ConversionHelper
    {
        /// <summary>
        /// Initializes the IK Notetracks
        /// </summary>
        /// <param name="item">Item we are converting</param>
        /// <param name="sampler">The sampler</param>
        /// <param name="mainAnimation">Main file</param>
        /// <param name="leftHandSettings">Left hand settings</param>
        /// <param name="rightHandSettings">Right hand settings</param>
        public static void InitializeIK(SkeletonAnimation? leftHandPose, SkeletonAnimation? rightHandPose, Skeleton skeleton, AnimationPlayer player, List<AnimationAction>? notetracks, float startingWeight)
        {
            AnimationSampler? lSampler = null;
            AnimationSampler? rSampler = null;
            AnimationSamplerSolver? lSolver = null;
            AnimationSamplerSolver? rSolver = null;

            // If we have poses, apply them
            if (leftHandPose is not null)
            {
                lSampler = new SkeletonAnimationSampler("LPose", leftHandPose, skeleton);
                player.Layers.Add(lSampler);
            }
            if (rightHandPose is not null)
            {
                rSampler = new SkeletonAnimationSampler("RPose", rightHandPose, skeleton);
                player.Layers.Add(rSampler);
            }
            // If we have the required bones, we'll do IK
            if (skeleton.TryGetBone("j_shoulder_le", out var lStartBone) &&
                skeleton.TryGetBone("j_elbow_le", out var lMiddleBone) &&
                skeleton.TryGetBone("j_wrist_le", out var lEndBone) &&
                skeleton.TryGetBone("tag_ik_loc_le_foregrip_vertgrip03", out var lTargetBone))
            {
                lSolver = new IKTwoBoneSolver("LSolver", lStartBone, lMiddleBone, lEndBone, lTargetBone);
                player.Solvers.Add(lSolver);
            }
            // If we have the required bones, we'll do IK
            if (skeleton.TryGetBone("j_shoulder_ri", out var rStartBone) &&
                skeleton.TryGetBone("j_elbow_ri", out var rMiddleBone) &&
                skeleton.TryGetBone("j_wrist_ri", out var rEndBone) &&
                skeleton.TryGetBone("tag_ik_loc_ri", out var rTargetBone))
            {
                rSolver = new IKTwoBoneSolver("RSolver", rStartBone, rMiddleBone, rEndBone, rTargetBone);
                player.Solvers.Add(rSolver);
            }


            // Quick exit for no actions
            if (notetracks is null)
                return;

            //lSampler?.Weights.Add(new(0, startingWeight));
            //rSampler?.Weights.Add(new(0, startingWeight));
            //lSolver?.Weights.Add(new(0, startingWeight));
            //rSolver?.Weights.Add(new(0, startingWeight));

            foreach (var note in notetracks)
            {
                switch (note.Name)
                {
                    case "ik_out_start_left_hand":
                    case "ik_in_end_left_hand":
                        foreach (var frame in note.KeyFrames)
                            lSolver?.Weights.Add(new(frame.Frame, 1));
                        break;
                    case "ik_in_start_left_hand":
                    case "ik_out_end_left_hand":
                        foreach (var frame in note.KeyFrames)
                            lSolver?.Weights.Add(new(frame.Frame, 0));
                        break;
                    case "ik_out_start_right_hand":
                    case "ik_in_end_right_hand":
                        foreach (var frame in note.KeyFrames)
                            rSolver?.Weights.Add(new(frame.Frame, 1));
                        break;
                    case "ik_in_start_right_hand":
                    case "ik_out_end_right_hand":
                        foreach (var frame in note.KeyFrames)
                            rSolver?.Weights.Add(new(frame.Frame, 0));
                        break;
                    case "fingers_out_start_left_hand":
                    case "fingers_in_end_left_hand":
                        foreach (var frame in note.KeyFrames)
                            lSampler?.Weights.Add(new(frame.Frame, 1));
                        break;
                    case "fingers_in_start_left_hand":
                    case "fingers_out_end_left_hand":
                        foreach (var frame in note.KeyFrames)
                            lSampler?.Weights.Add(new(frame.Frame, 0));
                        break;
                }
            }

            lSampler?.Weights.Sort((x, y) => x.Frame.CompareTo(y.Frame));
            rSampler?.Weights.Sort((x, y) => x.Frame.CompareTo(y.Frame));
            lSolver?.Weights.Sort((x, y) => x.Frame.CompareTo(y.Frame));
            rSolver?.Weights.Sort((x, y) => x.Frame.CompareTo(y.Frame));


            //if (lSolver != null)
            //{
            //    lSolver.DefaultWeight = item.LeftHandIK ? 1.0f : 0.0f;
            //    lSolver.Weights.Sort((x, y) => x.Time.CompareTo(y.Time));
            //}
            //if (rSolver != null)
            //{
            //    rSolver.DefaultWeight = item.RightHandIK ? 1.0f : 0.0f;
            //    rSolver.Weights.Sort((x, y) => x.Time.CompareTo(y.Time));
            //}
            //if (leftHandLayer != null)
            //{
            //    leftHandLayer.DefaultWeight = item.LeftHandIK ? 1.0f : 0.0f;
            //    leftHandLayer.Weights.Sort((x, y) => x.Time.CompareTo(y.Time));
            //}
            //if (rightHandLayer != null)
            //{
            //    rightHandLayer.DefaultWeight = item.RightHandIK ? 1.0f : 0.0f;
            //    rightHandLayer.Weights.Sort((x, y) => x.Time.CompareTo(y.Time));
            //}
        }

        public static SkeletonAnimation BakeFromPlayer(Skeleton skeleton, AnimationPlayer player, string outputFile)
        {
            var newAnim = new SkeletonAnimation(outputFile)
            {
                TransformType = TransformType.Absolute
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

            return newAnim;
        }
    }
}
