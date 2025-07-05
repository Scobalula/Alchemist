using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alchemist.UI
{
    public class AlchemistProjectFile
    {
        public required bool EnableAnimationTrimming { get; set; }
        public required string LeftIKStartBoneName { get; set; }
        public required string LeftIKMidBoneName { get; set; }
        public required string LeftIKEndBoneName { get; set; }
        public required string LeftIKTargetBoneName { get; set; }
        public required string RightIKStartBoneName { get; set; }
        public required string RightIKMidBoneName { get; set; }
        public required string RightIKEndBoneName { get; set; }
        public required string RightIKTargetBoneName { get; set; }
        public required string OutputPrefix { get; set; }
        public required string OutputSuffix { get; set; }
        public required string OutputFormat { get; set; }
        public bool MatchOldCallOfDuty { get; set; }
        public List<Animation>? Animations { get; set; }
        public List<Part>? Parts { get; set; }
    }
}
