namespace Fluxzy.Core.Pcap.Pcapng.Merge
{
    internal class BlockComparer : IComparer<IBlockReader>
    {
        public static readonly BlockComparer Instance = new();

        private BlockComparer()
        {

        }

        public int Compare(IBlockReader? x, IBlockReader? y)
        {
            var xTimeStamp = x!.NextTimeStamp;

            if (xTimeStamp == long.MaxValue)
            {
                return 1;
            }

            var yTimeStamp = y!.NextTimeStamp;

            if (yTimeStamp == long.MaxValue)
            {
                return -1;
            }

            return xTimeStamp.CompareTo(yTimeStamp);
        }
    }
}