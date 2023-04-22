// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Desktop.Services.Attributes;

namespace Fluxzy.Desktop.Ui.ViewModels
{
    [Exportable]
    public class DescriptionInfo
    {
        public DescriptionInfo(string description)
        {
            Description = description;
        }

        public string Description { get; init; }
    }
}
