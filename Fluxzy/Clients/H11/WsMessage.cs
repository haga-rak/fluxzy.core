// // Copyright 2022 - Haga Rakotoharivelo
// 

using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;

namespace Fluxzy.Clients.H11
{
    public class WsMessage
    {
        public WsMessage(int id)
        {
            Id = id; 
        }

        public int Id { get; set; }

        public WsOpCode OpCode { get; set; }

        public long Length { get; set; }

        public byte[]?  Data { get; set; }

        public string? DataString { get; set; }

        public DateTime MessageStart { get; set; }

        public DateTime MessageEnd { get; set; }

        internal void ApplyXor(Span<byte> data, int mask)
        {
            if (mask == 0)
                return; 

            Span<byte> maskData = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(maskData, mask);
            for (int i = 0; i < data.Length; i++) {
                data[i] = (byte) (data[i] ^ maskData[i % 4]);

            }

            //for (int i = 0; i < data.Length;) {
                //    var currentBlock = data.Slice(i);

                //    if (currentBlock.Length >= 4) {
                //        var val = BinaryPrimitives.ReadInt32BigEndian(currentBlock);
                //        var finalVal = val ^ mask; 
                //        BinaryPrimitives.WriteInt32BigEndian(currentBlock, finalVal);
                //        i += 4;
                //        continue;
                //    }
                //    BinaryPrimitives.WriteInt32BigEndian(maskData, mask);

                //    for (int j = 0; j < currentBlock.Length; j++) {
                //        currentBlock[j] = (byte) (currentBlock[j] ^ maskData[j]); 
                //    }

                //    break; 
                //}
            }


        internal async Task AddFrame(
            WsFrame wsFrame, int maxWsMessageLengthBuffered, PipeReader pipeReader,
            Func<int, Stream> outStream)
        {
            if (wsFrame.OpCode != 0) {
                OpCode = wsFrame.OpCode; 
            }

            if (wsFrame.FinalFragment && Length == 0 && wsFrame.PayloadLength < maxWsMessageLengthBuffered) {
                // Build direct buffer for message 

                var readResult = await pipeReader.ReadAtLeastAsync((int) wsFrame.PayloadLength);

                Data = new byte[wsFrame.PayloadLength];

                readResult.Buffer.FirstSpan.Slice(0, (int)wsFrame.PayloadLength)
                          .CopyTo(Data);

                ApplyXor(Data, wsFrame.MaskedPayload);

                DataString = Encoding.UTF8.GetString(Data);

                pipeReader.AdvanceTo(readResult.Buffer.GetPosition(wsFrame.PayloadLength));
            }
            else {
                int totalWritten = 0;
                var stream = outStream(Id); 

                while (totalWritten < wsFrame.PayloadLength) {

                    // Write into file 

                    var readResult = await pipeReader.ReadAsync();

                    var writtableBufferLength = (int) Math.Min(readResult.Buffer.Length, (wsFrame.PayloadLength - totalWritten)); 

                    stream.Write(readResult.Buffer.FirstSpan.Slice(0, writtableBufferLength));

                    totalWritten += writtableBufferLength; 

                    pipeReader.AdvanceTo(readResult.Buffer.GetPosition(writtableBufferLength));
                }
            }
                
            Length += wsFrame.PayloadLength; 
        }
        

    }
}