// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Core.Pcap.Pcapng
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TArgs"></typeparam>
    internal class BlockMerger<TArgs> where TArgs : notnull
    {
        public void Merge(IBlockWriter writer,
            Func<TArgs, IBlockReader> blockFactory,
            params TArgs[] items)
        {
            if (!items.Any())
                return;

            var array = items.Select(blockFactory).ToArray();

            Array.Sort(array, BlockComparer.Instance);

            while (true)
            {
                if (!array[0].Dequeue(out var block))
                    break; // No more block to read
                
                writer.Write(ref block);

                ArrayUtilities.Reposition(array, array[0], BlockComparer.Instance);
            }

            foreach (var resource in array)
            {
                resource.Dispose();
            }
        }
    }


    public static class PcapMerge
    {
        public static void Merge(
            IEnumerable<FileInfo> pcapFiles,
            IEnumerable<FileInfo> nssKeyLogs, Stream outStream, 
            int maxConcurrentOpenFile = 20)
        {
            var merger = new BlockMerger<FileInfo>();

            var blockHandler = new GenericBlockHandler(outStream, nssKeyLogs);
            var streamLimiter = new StreamLimiter(maxConcurrentOpenFile);

            merger.Merge(blockHandler, f => new EnhancedBlockReader(blockHandler, streamLimiter, f.OpenRead), pcapFiles.ToArray());
        }
    }
}
