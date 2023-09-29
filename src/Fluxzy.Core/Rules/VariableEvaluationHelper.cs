// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using Fluxzy.Core;

namespace Fluxzy.Rules
{
    /// <summary>
    ///     Helper for evaluating variables in a string.
    ///     The default syntax of a variable is ${variableName}
    /// </summary>
    public static class VariableEvaluationHelper
    {
        public static string? EvaluateVariable(
            this string? str,
            ExchangeContext? exchangeContext)
        {
            if (str == null)
                return null;

            if (exchangeContext == null)
                return str;

            if (str.AsSpan().DoesNotContainsVariable())
                return str;

            return exchangeContext.VariableContext.EvaluateVariable(str, exchangeContext.VariableBuildingContext);
        }

        /// <summary>
        ///     This is a fast method to check if a string may contain a variable.
        ///     It's to avoid the cost of parsing the string with regex if it doesn't contain any variable.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool DoesNotContainsVariable(this ReadOnlySpan<char> input)
        {
            if (input.IndexOf("${", StringComparison.Ordinal) < 0)
                return true;

            return false;
        }

        /// <summary>
        ///     Fast non reliable check if an input contains a named captured regex.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool DoesNotContainsCapturedRegex(this ReadOnlySpan<char> input)
        {
            if (input.IndexOf("(?<", StringComparison.Ordinal) < 0)
                return true;

            return false;
        }
    }
}
