// // Copyright 2022 - Haga Rakotoharivelo
// 

using SharpPcap;
using System.Runtime.InteropServices;

namespace Fluxzy.Interop.Pcap
{
    internal interface IRawCaptureWriter : IDisposable
    {
        long Key { get; }

        bool Faulted { get; }

        void Flush();
        
        void Register(string outFileName);
        
        void Write(PacketCapture packetCapture);
    }
}