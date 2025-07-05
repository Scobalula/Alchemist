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
    public class IKTwoBoneSolverNative(
        string name,
        SkeletonBone start,
        SkeletonBone mid,
        SkeletonBone end,
        SkeletonBone target,
        Vector3 poleVector) : AnimationSamplerSolver(name)
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
        public float DefaultWeight { get; set; } = 1.0f;

        /// <summary>
        /// Gets or Sets the Weights Cursor
        /// </summary>
        public int CurrentWeightsCursor { get; set; }

        /// <summary>
        /// Gets or Sets the Pole Vector
        /// </summary>
        public Vector3 PoleVector { get; set; } = poleVector;

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

            if (weight == 0)
                return;




            // Solve at the current frame
            SkellyTwoBoneIKSolver_Solve(
                StartBone.WorldTranslation.X,
                StartBone.WorldTranslation.Y,
                StartBone.WorldTranslation.Z,

                MiddleBone.WorldTranslation.X,
                MiddleBone.WorldTranslation.Y,
                MiddleBone.WorldTranslation.Z,

                EndBone.WorldTranslation.X,
                EndBone.WorldTranslation.Y,
                EndBone.WorldTranslation.Z,

                TargetBone.WorldTranslation.X,
                TargetBone.WorldTranslation.Y,
                TargetBone.WorldTranslation.Z,

                PoleVector.X,
                PoleVector.Y,
                PoleVector.Z,

                0,

                out var sX,
                out var sY,
                out var sZ,
                out var sW,
                out var mX,
                out var mY,
                out var mZ,
                out var mW);


            var startQuat = new Quaternion(
                (float)sX,
                (float)sY,
                (float)sZ,
                (float)sW);
            var midQuat = new Quaternion(
                (float)mX,
                (float)mY,
                (float)mZ,
                (float)mW);

            var midRotationResult = midQuat * MiddleBone.WorldRotation;
            var startRotationResult = startQuat * StartBone.WorldRotation;

            MiddleBone.WorldRotation = Quaternion.Slerp(MiddleBone.WorldRotation, midRotationResult, weight);
            MiddleBone.GenerateCurrentLocalTransform();
            MiddleBone.GenerateCurrentWorldTransforms();
            StartBone.WorldRotation = Quaternion.Slerp(StartBone.WorldRotation, startRotationResult, weight);
            StartBone.GenerateCurrentLocalTransform();
            StartBone.GenerateCurrentWorldTransforms();

            //// End bone will inherit from the target
            //// if (TargetConstrained)
            EndBone.WorldRotation = Quaternion.Slerp(EndBone.WorldRotation, TargetBone.WorldRotation, weight);
            EndBone.GenerateCurrentLocalTransform();
        }

        /// <summary>
        /// Solves the IK using Maya's Two Bone implementation (requires Maya's SDK)
        /// </summary>
        [DllImport("Skelly.TwoBoneIKSolver.dll")]
        extern static void SkellyTwoBoneIKSolver_Solve(
            double startJointPosX,
            double startJointPosY,
            double startJointPosZ,

            double midJointPosX,
            double midJointPosY,
            double midJointPosZ,

            double effectorPosX,
            double effectorPosY,
            double effectorPosZ,

            double handlePosX,
            double handlePosY,
            double handlePosZ,

            double poleVectorX,
            double poleVectorY,
            double poleVectorZ,

            double twistValue,

            out double qStartOutX,
            out double qStartOutY,
            out double qStartOutZ,
            out double qStartOutW,
            out double qMidOutX,
            out double qMidOutY,
            out double qMidOutZ,
            out double qMidOutW);
    }
}
