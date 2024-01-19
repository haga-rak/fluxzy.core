// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Buffers.Binary;

namespace Fluxzy.Core.Pcap.Pcapng.Structs
{
    internal static class OptionBlockFactory
    {
        public static (bool Success, int read) TryParse(ReadOnlySpan<byte> buffer, out IOptionBlock? block)
        {
            if (buffer.Length < 4) {
                throw new InvalidOperationException("Buffer too small"); 
            }

            var optionCode = (OptionBlockCode) BinaryPrimitives.ReadUInt16LittleEndian(buffer);
            var optionLength = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(2));

            if (buffer.Length < optionLength + 4) {
                block = default!;
                return (false, 0); // EOF 
            }

            if (optionCode == OptionBlockCode.Opt_EndOfOpt) {
                block = new EndOfOption();
                return (true, 4 + optionLength);
            }

            if (optionCode == OptionBlockCode.If_MacAddr) {
                block = IfMacAddressOption.Parse(buffer.Slice(4, optionLength));
                return (true, 4 + optionLength);
            }

            if (optionCode == OptionBlockCode.If_TsResol) {
                block = TsResolOption.Parse(buffer.Slice(4, optionLength)); 
                return (true, 4 + optionLength);
            }

            if (
                optionCode == OptionBlockCode.Shb_UserAppl
                || optionCode == OptionBlockCode.Shb_Os
                || optionCode == OptionBlockCode.Shb_Hardware
                ) {
                block = StringOptionBlock.Parse(buffer.Slice(4, optionLength));
                return (true, 4 + optionLength);
            }

            // Unknown option, we skip it any way

            block = default!;
            return (true, 4 + optionLength);
        }
    }
}
