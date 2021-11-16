using System;
using System.IO;
using Echoes.H2.Cli.IO;

namespace Echoes.H2.Cli
{
    public interface IWriteJob
    {
        Stream Stream { get; }

        int? Length { get; }
    }

    public readonly struct WriteJob : IWriteJob
    {
        public WriteJob(Memory<byte> data)
        {
            Stream = new ReadonlyMemoryStream(data);
            Length = data.Length;
        }

        public WriteJob(Stream stream) 
            : this(stream, null)
        {

        }

        public WriteJob(Stream stream, int? length)
        {
            Stream = stream;
            Length = length;
        }

        public Stream Stream { get;  }

        public int ? Length { get;  }
    }
}