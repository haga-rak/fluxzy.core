﻿namespace Echoes.Encoding.Huffman.Interfaces
{
    public interface IHuffmanDictionary
    {
        Symbol [] Symbols { get;  }

        int ShortestSymbolLength { get;  }
    }
}