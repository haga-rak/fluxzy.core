using System;
using System.Linq;
using System.Reflection;

namespace Fluxzy.Tools.DocGen
{
    internal static class ReflectionHelper
    {
        public static T GetForcedInstance<T>(Type type)
        {
            var propertyName = "BuildDefaultInstance";

            var methodInfo = 
                    type.GetMethod(propertyName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            if (methodInfo != null && methodInfo.ReturnType == type && methodInfo.GetParameters().Length == 0) {
                // If the type has a static method BuildDefaultInstance, we use it to create an instance

                return (T) methodInfo.Invoke(null, null)!;
            }

            var constructor = type.GetConstructors().OrderBy(c => c.GetParameters().Length)
                                  .First();

            var parameters = constructor.GetParameters()
                                        .Select(p => p.ParameterType == typeof(string)
                                            ? string.Empty : Activator.CreateInstance(p.ParameterType))
                                        .ToArray();

            return (T) constructor.Invoke(parameters); 
        }
    }
}