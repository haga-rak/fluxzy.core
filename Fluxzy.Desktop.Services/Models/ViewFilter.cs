// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Rules.Filters;

namespace Fluxzy.Desktop.Services.Models
{
    public class ViewFilter
    {
        public ViewFilter(Filter filter)
        {
            Filter = filter;
        }

        public Filter Filter { get; }
    }
}