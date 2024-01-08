// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fluxzy.Rules;
using Fluxzy.Rules.Filters;
using Xunit;
using Action = Fluxzy.Rules.Action;

namespace Fluxzy.Tests.UnitTests.Rules
{
    public class DefaultActionSettings
    {
        public static IEnumerable<object[]> GetActionTypes()
        {
            return typeof(Filter).Assembly
                                 .GetTypes()
                                 .Where(t => t.IsSubclassOf(typeof(Action)))
                                 .Where(t => !t.IsAbstract)
                                 .Where(t => t.GetCustomAttribute<ActionMetadataAttribute>()  != null)
                                 // having a default constructor is a requirement
                                 .Where(t => t.GetConstructors().Any(c => c.IsPublic))
                                 .Select(s => new object[] { s.FullName! })
                                 .ToList();
        }

        private static Action GetInstance(string filterName)
        {
            var type = typeof(Action).Assembly.GetType(filterName)!;

            var constructor = type.GetConstructors().First(c => c.IsPublic);
            var parameters = constructor.GetParameters().Select(s => s.ParameterType).ToList();

            var args = parameters.Select(s => s.IsValueType ? Activator.CreateInstance(s) : null).ToArray();

            var action = (Action)constructor.Invoke(args)!;

            return action;
        }

        [Theory]
        [MemberData(nameof(GetActionTypes))]
        public void ValidateProperties(string filterName)
        {
            var action = GetInstance(filterName);

            Assert.NotNull(action.ActionScope);
            Assert.NotNull(action.DefaultDescription);
            Assert.NotNull(action.FriendlyName);
            Assert.NotNull(action.NoEditableSetting);

            Assert.NotNull(action.ScopeId);
            Assert.NotNull(action.IsPremade());
        }

        [Theory]
        [MemberData(nameof(GetActionTypes))]
        public void ValidateExamples(string filterName)
        {
            var action = GetInstance(filterName);

            var examples = action.GetExamples()?.ToList();

            Assert.NotNull(examples);

            foreach (var example in examples!)
            {
                Assert.NotNull(example.Description);
                Assert.NotNull(example.Action);
            }
        }
    }
}
