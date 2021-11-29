// Copyright © 2021 Haga Rakotoharivelo

using System.Collections.Generic;
using System.Threading.Channels;

namespace Echoes.H2.Cli.Helpers
{
    public static class ChannelHelper
    {
        public static bool TryReadAll<T>(this ChannelReader<T> channel, ref IList<T> refList)
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