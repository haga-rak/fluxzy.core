using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fluxzy.Desktop.Services.Attributes
{
    /// <summary>
    /// Marks a class as exportable to typescript
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ExportableAttribute : Attribute
    {
        
    }
}
