// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text;

namespace Fluxzy.Clients.H2
{
    public static class H2Constants
    {
        public static readonly byte[] Preface = Encoding.ASCII.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n");
    }
}
