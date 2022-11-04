// // Copyright 2022 - Haga Rakotoharivelo
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fluxzy.Rules.Filters;
using Action = Fluxzy.Rules.Action;

namespace Fluxzy.Tests.Configurations
{
    internal static class ReflectionHelper
    {
        public static PropertyInfo[] GetSettableProperties(Type type)
        {
            return type.GetProperties(BindingFlags.GetProperty | BindingFlags.SetProperty
                                                               | BindingFlags.Public | BindingFlags.Instance)
                       .Where(p => p.Name != nameof(Filter.Identifier)).ToArray(); 
        }

        public static object?  GetPropertyValue(this object obj,PropertyInfo propertyInfo)
        {
            return propertyInfo.GetValue(obj);
        }
    }

    /// <summary>
    /// This greed equality comparer should be used only for testing 
    /// </summary>
    public class GreedyFilterComparer : IEqualityComparer<Filter>
    {
        public bool Equals(Filter x, Filter y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            if (x.GetType() != y.GetType())
                return false;

            if (x is FilterCollection colX && y is FilterCollection colY) {

                if (colX.Children.Count != colY.Children.Count)
                    return false;

                if (colX.Inverted != colY.Inverted)
                    return false;

                if (colX.Operation != colY.Operation)
                    return false;

                for (int i = 0 ; i < colX.Children.Count ; i ++) {
                    if (!Equals(colX.Children[i], colY.Children[i]))
                        return false;
                }
            }
            else {

                var properties = ReflectionHelper.GetSettableProperties(x.GetType());

                foreach (var propertyInfo in properties) {
                    var valX = x.GetPropertyValue(propertyInfo);
                    var valY = y.GetPropertyValue(propertyInfo);

                    if (!Equals(valX, valY))
                        return false; 
                }
            }

            return true; 
        }

        public int GetHashCode(Filter obj)
        {
            // No hashcode eval force equals 
            return 0;
        }
    }
   
    public class GreedyActionComparer : IEqualityComparer<Action>
    {
        public bool Equals(Action x, Action y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            if (x.GetType() != y.GetType())
                return false;

            var properties = ReflectionHelper.GetSettableProperties(x.GetType());

            foreach (var propertyInfo in properties) {
                var valX = x.GetPropertyValue(propertyInfo);
                var valY = y.GetPropertyValue(propertyInfo);

                if (!Equals(valX, valY))
                    return false; 
            }

            return true; 
        }

        public int GetHashCode(Action obj)
        {
            // No hashcode eval force equals 
            return 0;
        }
    }
}