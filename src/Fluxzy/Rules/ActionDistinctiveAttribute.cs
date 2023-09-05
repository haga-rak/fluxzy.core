// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Rules
{
    public class ActionDistinctiveAttribute : PropertyDistinctiveAttribute, IVariableHolder
    {
    }

    public class PropertyDistinctiveAttribute : Attribute
    {
        public bool Expand { get; set; }

        public bool IgnoreInDoc { get; set; } = false;

        public string Description { get; set; } = string.Empty;

        public string? DefaultValue { get; set; }

    }
}
