// // Copyright 2022 - Haga Rakotoharivelo
// 

using SharpPcap;

namespace Fluxzy.Core.Pcap.Writing
{
    internal interface IRawCaptureWriter : IDisposable
    {
        long Key { get; }

        bool Faulted { get; }

        void Flush();

        void Register(string outFileName);

        void Write(PacketCapture packetCapture);

        void StoreKey(string nssKey);
    }
}