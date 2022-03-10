using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Splat;

namespace Echoes.Desktop.Common.Extensions
{
    public static class LocatorExtensions
    {
        public static T GetRequiredService<T>(this IReadonlyDependencyResolver solver)
        {
            return solver.GetService<T>()!;
        }
    }
}
