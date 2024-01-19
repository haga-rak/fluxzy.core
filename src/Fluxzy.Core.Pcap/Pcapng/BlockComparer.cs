namespace Fluxzy.Core.Pcap.Pcapng
{
    internal class BlockComparer<T> : IComparer<IBlockReader<T>>
        where T : struct
    {
        public static readonly BlockComparer<T> Instance = new();

        private BlockComparer()
        {

        }

        public int Compare(IBlockReader<T>? x, IBlockReader<T>? y)
        {
            var xTimeStamp = x!.NextTimeStamp;

            if (xTimeStamp == -1)
            {
                return 1;
            }

            var yTimeStamp = y!.NextTimeStamp; 

            if (yTimeStamp == -1) {
                return -1;
            }

            return xTimeStamp.CompareTo(yTimeStamp);
        }
    }
}