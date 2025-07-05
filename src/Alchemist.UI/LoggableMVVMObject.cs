using RedFox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Alchemist.UI
{
    /// <summary>
    /// A class to hold a MVVM object that outputs logs for certain actions.
    /// </summary>
    public class LoggableMVVMObject : MVVMObject
    {
        /// <inheritdoc/>
        protected override void SetValue<T>(T newValue, [CallerMemberName] string propertyName = "")
        {
            Logging.Logger.Info($"Setting: {propertyName} to {newValue} of type: {typeof(T)}"); 
            base.SetValue(newValue, propertyName);
        }
    }
}
