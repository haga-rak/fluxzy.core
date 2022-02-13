// Copyright © 2021 Haga Rakotoharivelo

using System.Collections.Generic;
using System.Threading.Channels;

namespace Echoes.Helpers
{
    internal static class ChannelHelper
    {
        public static bool TryReadAll<T>(this ChannelReader<T> channel, ref List<T> refList)
        {
            bool any = false; 

            while (channel.TryRead(out var item))
            {
                refList.Add(item);
                any = true; 
            }

            return any; 
        }
    }
}