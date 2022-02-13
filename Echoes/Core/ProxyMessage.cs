using System;

namespace Echoes.Core
{
    internal class ProxyMessage
    {
        public ProxyMessage(ReadOnlyMemory<char> provisionHeader, Authority authority)
        {
            ProvisionHeader = provisionHeader;
            Valid = provisionHeader.Length > 0;
        }

        public ProxyMessage(bool valid)
        {
            Valid = valid;
        }


        public ReadOnlyMemory<char> ProvisionHeader { get; }

        public bool Valid { get; set; }

        
    }
}