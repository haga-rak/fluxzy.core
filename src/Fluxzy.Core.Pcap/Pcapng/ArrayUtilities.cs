namespace Fluxzy.Core.Pcap.Pcapng
{
    internal static class ArrayUtilities
    {
        public static void Reposition<T>(T[] sortedArrayAtIndex1, T firstElement,
            IComparer<T> comparer)
        {
            for (int i = 1; i < sortedArrayAtIndex1.Length; i++)
            {
                var current = sortedArrayAtIndex1[i];

                if (comparer.Compare(firstElement, current) <= 0) {
                    return;
                }

                sortedArrayAtIndex1[i - 1] = current;
                sortedArrayAtIndex1[i] = firstElement;
            }
        }
    }
}