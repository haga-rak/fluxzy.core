﻿// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using Fluxzy.Misc;

namespace Fluxzy.Rules.Filters.ResponseFilters
{
    /// <summary>
    ///     Select exchange that HTTP status code indicates a successful request (2XX)
    /// </summary>
    [FilterMetaData(
        LongDescription = "Select exchange that HTTP status code indicates a successful request (2XX)."
    )]
    public class StatusCodeSuccessFilter : Filter
    {
        public override Guid Identifier => (GetType().Name + Inverted).GetMd5Guid();

        public override FilterScope FilterScope => FilterScope.ResponseHeaderReceivedFromRemote;

        public override string AutoGeneratedName => "Success status code (2XX)";

        public override string GenericName => "Status code success (2XX)";

        public override string ShortName => "2XX";

        public override bool PreMadeFilter => true;

        public override bool Common { get; set; } = true;

        protected override bool InternalApply(
            IAuthority authority, IExchange? exchange,
            IFilteringContext? filteringContext)
        {
            if (exchange == null)
                return false;

            var statusCode = exchange.StatusCode;

            return statusCode is >= 200 and < 300;
        }
    }
}
