// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Threading.Channels;

namespace Fluxzy.Misc
{
    public static class ChannelThreadingExtensions
    {
        public static bool TryReadAll<T>(this ChannelReader<T> channel, List<T> refList)
        {
            var any = false;

            while (channel.TryRead(out var item)) {
                refList.Add(item);
                any = true;
            }

            return any;
        }
    }
}
