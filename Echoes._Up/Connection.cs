using System;

namespace Echoes
{
    /// <summary>
    /// Contains information about transport layer 
    /// </summary>
    public class Connection
    {
        public int ConnectionId { get; set; }

        public Authority Authority { get; set; }

        public DateTime ConnectionOpen { get; set; }

        public DateTime ConnectionClosed { get; set; }
    }
}