using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using RedFox.Graphics3D;
using RedFox.Graphics3D.Skeletal;

namespace Alchemist.InverseKinematics
{

    /// <summary>
    /// 
    /// </summary>
    /// <param name=""></param>
    /// <param name="start"></param>
    /// <param name="mid"></param>
    /// <param name="end"></param>
    /// <param name="target"></param>
    /// <param name="poleVector"></param>
    public class IKTwoBoneSolver(
        string name,
        SkeletonBone start,
        SkeletonBone mid,
        SkeletonBone end,
        SkeletonBone target) : AnimationSamplerSolver(name)
    {
        /// <summary>
        /// Gets or Sets the start bone .
        /// </summary>
        public SkeletonBone StartBone { get; set; } = start;

        /// <summary>
        /// Gets or Sets the start middle bone .
        /// </summary>
        public SkeletonBone MiddleBone { get; set; } = mid;

        /// <summary>
        /// Gets or Sets the end bone .
        /// </summary>
        public SkeletonBone EndBone { get; set; } = end;

        /// <summary>
        /// Gets or Sets the target bone .
        /// </summary>
        public SkeletonBone TargetBone { get; set; } = target;

        /// <summary>
        /// Gets or Sets the default weight
        /// </summary>
        public float DefaultWeight { get; set; }

        /// <summary>
        /// Gets or Sets the Weights Cursor
        /// </summary>
        public int CurrentWeightsCursor { get; set; }

        /// <summary>
        /// Gets or Sets if the target bone's rotation is constrained to the target.
        /// </summary>
        public bool TargetConstrained { get; set; }

        /// <inheritdoc/>
        public override void Update(float time)
        {
            var cursor = CurrentWeightsCursor;
            var weight = AnimationHelper.GetWeight(Weights, time, 0.0f, 1.0f, ref cursor);

            CurrentWeightsCursor = cursor;

            if (weight == 0)
                return;

            var a = StartBone.WorldTranslation;
            var b = MiddleBone.WorldTranslation;
            var c = EndBone.WorldTranslation;
            var t = TargetBone.WorldTranslation;

            // From article: https://www.theorangeduck.com/page/simple-two-joint
            float lab = (b - a).Length();
            float lcb = (b - c).Length();
            float lat = Math.Clamp((t - a).Length(), float.Epsilon, lab + lcb - float.Epsilon);

            float ac_ab_0 = MathF.Acos(Math.Clamp(Vector3.Dot(Vector3.Normalize(c - a), Vector3.Normalize(b - a)), -1, 1));
            float ba_bc_0 = MathF.Acos(Math.Clamp(Vector3.Dot(Vector3.Normalize(a - b), Vector3.Normalize(c - b)), -1, 1));
            float ac_at_0 = MathF.Acos(Math.Clamp(Vector3.Dot(Vector3.Normalize(c - a), Vector3.Normalize(t - a)), -1, 1));

            float ac_ab_1 = MathF.Acos(Math.Clamp((lcb * lcb - lab * lab - lat * lat) / (-2 * lab * lat), -1, 1));
            float ba_bc_1 = MathF.Acos(Math.Clamp((lat * lat - lab * lab - lcb * lcb) / (-2 * lab * lcb), -1, 1));

            Vector3 axis0 = Vector3.Normalize(Vector3.Cross(c - a, b - a));
            Vector3 axis1 = Vector3.Normalize(Vector3.Cross(c - a, t - a));

            // TODO: Handle IK handle that can't be reached, not a problem for what we do tho

            Quaternion r0 = Quaternion.CreateFromAxisAngle(Vector3.Transform(axis0, Quaternion.Inverse(StartBone.WorldRotation)), ac_ab_1 - ac_ab_0);
            Quaternion r1 = Quaternion.CreateFromAxisAngle(Vector3.Transform(axis0, Quaternion.Inverse(MiddleBone.WorldRotation)), ba_bc_1 - ba_bc_0);
            Quaternion r2 = Quaternion.CreateFromAxisAngle(Vector3.Transform(axis1, Quaternion.Inverse(StartBone.WorldRotation)), ac_at_0);

            var a_lr = r0 * r2;
            var b_lr = r1;

            StartBone.LocalRotation = Quaternion.Slerp(StartBone.LocalRotation, StartBone.LocalRotation * a_lr, weight);
            MiddleBone.LocalRotation = Quaternion.Slerp(MiddleBone.LocalRotation, MiddleBone.LocalRotation * b_lr, weight);
            StartBone.GenerateCurrentWorldTransforms();
            MiddleBone.GenerateCurrentWorldTransforms();

            // EndBone.WorldTranslation = Vector3.Lerp(EndBone.WorldTranslation, TargetBone.WorldTranslation, weight);
            EndBone.WorldRotation = Quaternion.Slerp(EndBone.WorldRotation, TargetBone.WorldRotation, weight);
            EndBone.GenerateCurrentLocalTransform();
            EndBone.GenerateCurrentWorldTransforms();
        }
    }
}
