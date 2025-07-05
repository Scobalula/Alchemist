using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alchemist.UI
{
    internal class Logging
    {
        public static readonly log4net.ILog Logger = log4net.LogManager.GetLogger("Alchemist");
    }
}
