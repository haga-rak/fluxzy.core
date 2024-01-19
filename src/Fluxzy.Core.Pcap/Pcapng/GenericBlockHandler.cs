// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Core.Pcap.Pcapng.Structs;

namespace Fluxzy.Core.Pcap.Pcapng
{
    internal class GenericBlockHandler : IBlockWriter
    {
        private readonly Stream _outStream;
        private readonly IEnumerable<FileInfo> _nssKeyFileInfos;

        private readonly HashSet<string> _shbUserApplSet = new(StringComparer.OrdinalIgnoreCase); 
        private readonly HashSet<string> _shbOsSet = new(StringComparer.OrdinalIgnoreCase); 
        private readonly HashSet<string> _shbHardware = new(StringComparer.OrdinalIgnoreCase);

        private PcapngStreamWriter? _writer;

        public GenericBlockHandler(Stream outStream, IEnumerable<FileInfo> nssKeyFileInfos)
        {
            _outStream = outStream;
            _nssKeyFileInfos = nssKeyFileInfos;
        }

        private PcapngStreamWriter CreateWriter()
        {
            var info = new PcapngGlobalInfo(
                string.Join(", ", _shbUserApplSet),
                string.Join(", ", _shbOsSet),
                string.Join(", ", _shbHardware));

            var result = new PcapngStreamWriter(info);

            result.WriteSectionHeaderBlock(_outStream);

            foreach (var nssKeyInfoFile in _nssKeyFileInfos) {
                result.WriteNssKey(_outStream, File.ReadAllText(nssKeyInfoFile.FullName));
            }

            return result; 
        }

        public void Write(ref DataBlock content)
        {
            if (_writer == null) {
                _writer = CreateWriter();
            }

            _outStream.Write(content.Data.Span);
        }

        public bool NotifyNewBlock(uint blockType, ReadOnlySpan<byte> buffer)
        {
            if (blockType == SectionHeaderBlock.BlockTypeValue) {
                // Section block 
                
                var stringOp = SectionHeaderBlock.Parse(buffer);

                foreach (var (op, value) in stringOp) {
                    switch (op) {
                        case OptionBlockCode.Shb_Hardware:
                            _shbHardware.Add(value);
                            break;
                        case OptionBlockCode.Shb_Os:
                            _shbOsSet.Add(value);
                            break;
                        case OptionBlockCode.Shb_UserAppl:
                            _shbUserApplSet.Add(value);
                            break;
                    }
                }
            }
            if (
                blockType == NssDecryptionSecretsBlock.BlockTypeValue
                || blockType == InterfaceDescriptionBlock.BlockTypeValue
                ) {
                _outStream.Write(buffer);
            }

            return true;
        }
    }
}
