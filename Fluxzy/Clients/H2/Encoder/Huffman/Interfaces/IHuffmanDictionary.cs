// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Clients.H2.Encoder.Huffman.Interfaces
{
    public interface IHuffmanDictionary
    {
        Symbol[] Symbols { get; }

        int ShortestSymbolLength { get; }
    }
}
