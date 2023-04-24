﻿// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Rules.Filters
{
    public abstract class HeaderFilter : StringFilter
    {
        protected HeaderFilter(string pattern, string headerName)
            : base(pattern)
        {
            HeaderName = headerName;
        }

        protected HeaderFilter(string pattern, StringSelectorOperation operation, string headerName)
            : base(pattern, operation)
        {
            HeaderName = headerName;
        }

        public string HeaderName { get; set; }

        public override string AutoGeneratedName => $"{HeaderName} : {base.AutoGeneratedName}";
    }
}