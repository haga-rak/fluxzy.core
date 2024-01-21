// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Core.Pcap.Pcapng.Merge
{
    internal static class FilterConnectionHelper
    {
        public static bool CheckInList(string fileName, HashSet<int> connectionIds)
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

            if (!int.TryParse(fileNameWithoutExtension, out var connectionId)) {
                return false;
            }

            return connectionIds.Contains(connectionId);
        }
    }
}
