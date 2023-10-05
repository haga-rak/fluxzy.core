// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;

namespace Fluxzy.Cli.Commands.Dissects
{
    internal class DissectionOptions
    {
        public DissectionOptions(bool mustBeUnique, 
            IReadOnlyCollection<IDissectionFilter> filters,
            IReadOnlyCollection<IDissectionFormatter> formatters)
        {
            MustBeUnique = mustBeUnique;
            Filters = filters;
            Formatters = formatters;
        }

        public string Format { get; }

        public bool MustBeUnique { get;  }

        public IReadOnlyCollection<IDissectionFilter> Filters { get;  }

        public IReadOnlyCollection<IDissectionFormatter> Formatters { get; }
    }
}
