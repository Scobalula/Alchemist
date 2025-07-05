using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alchemist.InverseKinematics
{
    public struct IKSettings(string startBoneName,
                            string middleBoneName,
                            string endBoneName,
                            string targetBoneName)
    {
        public string StartBoneName { get; set; } = startBoneName;

        public string MiddleBoneName { get; set; } = middleBoneName;

        public string EndBoneName { get; set; } = endBoneName;

        public string TargetBoneName { get; set; } = targetBoneName;
    }
}
