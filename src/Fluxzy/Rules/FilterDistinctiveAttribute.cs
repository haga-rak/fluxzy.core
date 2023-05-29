// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Rules
{
    /// <summary>
    ///     Put this attribute on a filter property to include the value to the unique hash id generation.
    /// </summary>
    public class FilterDistinctiveAttribute : Attribute, IVariableHolder
    {
        public bool IgnoreInDoc { get; set; } = false;

        public string Description { get; set; } = string.Empty;

        public string? DefaultValue { get; set; }
    }
}
