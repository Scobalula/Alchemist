using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

namespace Alchemist.UI
{
    public class FileDialogService(Func<string, string, string?, IEnumerable<string>> onOpenFileDialog)
    {
        public Func<string, string, string?, IEnumerable<string>> OnOpenFileDialog { get; private set; } = onOpenFileDialog;

        public IEnumerable<string> OpenFileDialog(string title, string filter, string? defaultDirectory) => OnOpenFileDialog(title, filter, defaultDirectory);
    }
}
