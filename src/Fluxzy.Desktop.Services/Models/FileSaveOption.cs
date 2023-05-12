// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Desktop.Services.Models
{
    public class FileSaveOption
    {
        public FileSaveOption(FileSaveOptionType saveOptionType, int[]? selectedExchangeIds)
        {
            SaveOptionType = saveOptionType;
            SelectedExchangeIds = selectedExchangeIds;
        }

        public FileSaveOptionType SaveOptionType { get; }

        public int[]? SelectedExchangeIds { get; }
    }
}
