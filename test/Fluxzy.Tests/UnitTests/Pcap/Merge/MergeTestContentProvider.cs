// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Text;

namespace Fluxzy.Tests.UnitTests.Pcap.Merge
{
    internal static class MergeTestContentProvider
    {
        public static readonly string _200Elements = @"
0132,0020,0143,0048,0028,0113,0114,0021,0002,0039,0081,0175
0064,0106,0008,0171,0065,0154,0135,0178,0069,0071,0100,0187,0013
0035,0155,0052,0070,0023,0109,0090
0098,0033,0041,0088,0077,0102,0082,0104,0146,0027,0042,0004,0157,0131
0014,0163,0096,0043,0117,0165,0150,0107,0025,0094,0180,0168,0198
0053,0161,0176,0105,0195,0029,0173
0137,0184,0119,0141,0112
0192,0068,0197,0177,0055,0138,0074
0167,0122,0046,0156,0049,0089
0040,0005,0000,0120,0116
0179,0050,0001,0003,0145,0036,0011,0130,0097,0030,0022,0126,0037
0185,0078,0199,0128,0139
0099,0189,0125,0191,0017,0066
0147,0024,0110,0196,0009,0087,0047,0091,0067,0092
0152,0083,0149,0129,0016,0140,0015,0044
0142,0186,0086,0026,0061,0115,0159,0072,0193,0164,0174,0166,0190
0134,0045,0111,0085,0194,0095,0060,0127,0063
0162,0051,0034,0076,0101,0075,0019,0160,0012,0031,0144,0170,0136
0188,0073,0153,0079,0182,0084,0057,0038,0118,0148,0058,0103,0183,0080
0032,0169,0018,0124,0010,0062
0172,0093,0181,0108,0006,0056,0123,0059,0007,0133,0158
0121,0151,0054
";

        public static readonly string _20Elements = @"
0002,0008
0004,0013
0000,0005,0014
0001,0003
0009,0011,0017
0015,0016
0010,0012,0018,0019
0006,0007
";

        public static string GetTestData(int elementCount, int seed = 9)
        {
            var random = new Random(seed);
            var totalNumberCount = elementCount;

            var allNumbers = Enumerable.Range(0, totalNumberCount)
                                       .Select(t => t)
                                       .OrderBy(t => random.Next()).ToList();

            var sliceMin = 2;
            var sliceMax = 5;
            var currentIndex = 0;

            var result = new StringBuilder(); 

            while (currentIndex < totalNumberCount)
            {
                var remaining = totalNumberCount - currentIndex;

                var nextSlice = random.Next(sliceMin, sliceMax);

                if (remaining < nextSlice)
                {
                    nextSlice = remaining;
                }

                var slice = allNumbers.Slice(currentIndex, nextSlice)
                                      .OrderBy(r => r).ToList();

                currentIndex += nextSlice;

                result.AppendLine(string.Join(",", slice.Select(t => t.ToString("0000"))));
            }

            return result.ToString(); 
        }

    }
}
