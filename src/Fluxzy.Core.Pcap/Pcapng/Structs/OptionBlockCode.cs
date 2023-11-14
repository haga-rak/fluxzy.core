// // Copyright 2022 - Haga Rakotoharivelo
// 

// ReSharper disable InconsistentNaming

namespace Fluxzy.Core.Pcap.Pcapng.Structs
{
    internal enum OptionBlockCode
    {
        Shb_Hardware = 2,
        Shb_Os = 3,
        Shb_UserAppl = 4,

        Opt_EndOfOpt = 0,
        Opt_Comment = 1,

        If_Name = 2,
        If_Description = 3,
        If_Ipv4Addr = 4,
        If_Ipv6Addr = 5,
        If_MacAddr = 6,
        If_TsResol = 9,
    }
}