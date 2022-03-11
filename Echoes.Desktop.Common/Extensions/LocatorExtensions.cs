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
