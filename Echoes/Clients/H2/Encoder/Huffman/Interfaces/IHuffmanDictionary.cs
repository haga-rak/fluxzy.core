﻿namespace Echoes.H2.Encoder.Huffman.Interfaces
{
    public interface IHuffmanDictionary
    {
        Symbol [] Symbols { get;  }

        int ShortestSymbolLength { get;  }
    }
}