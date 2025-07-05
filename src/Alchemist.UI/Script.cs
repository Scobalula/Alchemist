using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Alchemist.UI
{
    public abstract class Script
    {
        public abstract string Name { get; }

        public Guid ID { get; } = Guid.NewGuid();

        public abstract void Run(MainViewModel viewModel);

        public override string ToString() => Name;
    }
}
