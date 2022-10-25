// // Copyright 2022 - Haga Rakotoharivelo
// 

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;

namespace Fluxzy.Clients.H11
{

    public enum WsMessageDirection
    {
        Sent = 1, 
        Receive
    }

    public class WsMessage
    {
        public WsMessage(int id)
        {
            Id = id; 
        }

        public int Id { get; set; }

        public WsMessageDirection Direction { get; set; }

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

            for (int i = 0; i < data.Length;)
            {
                var currentBlock = data.Slice(i);

                if (currentBlock.Length >= 4)
                {
                    var val = BinaryPrimitives.ReadInt32BigEndian(currentBlock);
                    var finalVal = val ^ mask;
                    BinaryPrimitives.WriteInt32BigEndian(currentBlock, finalVal);
                    i += 4;
                    continue;
                }
                BinaryPrimitives.WriteInt32BigEndian(maskData, mask);

                for (int j = 0; j < currentBlock.Length; j++)
                {
                    currentBlock[j] = (byte)(currentBlock[j] ^ maskData[j]);
                }

                break;
            }
        }


        internal async Task AddFrame(
            WsFrame wsFrame, int maxWsMessageLengthBuffered, PipeReader pipeReader,
            Func<int, Stream> outStream)
        {
            if (wsFrame.OpCode != 0) {
                OpCode |= wsFrame.OpCode; 
            }

            if (wsFrame.FinalFragment && Length == 0 && wsFrame.PayloadLength < maxWsMessageLengthBuffered) {
                // Build direct buffer for message 

                var readResult = await pipeReader.ReadAtLeastAsync((int) wsFrame.PayloadLength);

                Data = readResult.Buffer.ToArray();
                
                //readResult.Buffer.FirstSpan.Slice(0, (int) wsFrame.PayloadLength)
                //          .CopyTo(Data);

                ApplyXor(Data, wsFrame.MaskedPayload);

                // DataString = Encoding.UTF8.GetString(Data);

                pipeReader.AdvanceTo(readResult.Buffer.GetPosition(wsFrame.PayloadLength));
            }
            else {
                int totalWritten = 0;
                var stream = outStream(Id); 

                while (totalWritten < wsFrame.PayloadLength) {

                    // Write into file 

                    var readResult = await pipeReader.ReadAsync();

                   // readResult.Buffer.Slice()

                    var effectiveBufferLength = (int) Math.Min(readResult.Buffer.Length, (wsFrame.PayloadLength - totalWritten));

                    var totalWriteLength = 0; 
                    
                    foreach (var sequence in readResult.Buffer)
                    {
                        var memory = sequence.Slice(0,
                            Math.Min(sequence.Length, effectiveBufferLength - totalWriteLength));
                        
                        if (wsFrame.MaskedPayload != 0)
                        {
                            var copyBuffer = ArrayPool<byte>.Shared.Rent(memory.Length);

                            try
                            {
                                memory.CopyTo(copyBuffer);

                                ApplyXor(new Span<byte>(copyBuffer, 0, memory.Length),
                                        wsFrame.MaskedPayload);

                                stream.Write(copyBuffer, 0, memory.Length);
                            }
                            finally
                            {
                                ArrayPool<byte>.Shared.Return(copyBuffer);

                            }
                        }
                        else
                        {
                            stream.Write(memory.Span);
                        }
                        
                        totalWriteLength += memory.Length;
                    }
                   
                    totalWritten += effectiveBufferLength; 

                    pipeReader.AdvanceTo(readResult.Buffer.GetPosition(effectiveBufferLength));
                }
            }
                
            Length += wsFrame.PayloadLength; 
        }
        

    }
}