using System;
using System.Collections.Generic;
using System.Linq;

namespace Echoes.H2.Encoder.Huffman
{
    internal class Node
    {
        private readonly int _baseByte;
        private readonly int _column;

        private readonly List<Symbol> _listOfRawSymbols = new List<Symbol>();

        internal Node(int baseByte, int column)
        {
            _baseByte = baseByte;
            _column = column;
            ChildNodes = new Node[256];
        }

        internal Node(Symbol value)
        {
            Value = value;
            HasValue = true; 
        }

        internal void AppendSymbol(Symbol symbol)
        {
            _listOfRawSymbols.Add(symbol);
        }

        internal void Seal()
        {
            if (_listOfRawSymbols.Count == 1)
            {
                HasValue = true;
                Value = _listOfRawSymbols.First();
                return; 
            }

            Span<byte> dataBuffer = stackalloc byte[256]; 

            foreach (var symbol in _listOfRawSymbols)
            {
                var lengthBitsInColumn = symbol.GetLengthBitsInColumn(_column);

                if (lengthBitsInColumn < 8)
                {
                    var node = new Node(symbol);

                    var sb = symbol.GetByteVariation(_column, dataBuffer);

                    foreach (var @byte in sb)
                    {
                        ChildNodes[@byte] = node;
                    }
                }
                else
                {
                    var byteAtColun = symbol.GetByte(_column);
                    var cNode = ChildNodes[byteAtColun]; 

                    ChildNodes[byteAtColun] = cNode ?? new Node(byteAtColun, _column + 1);
                    ChildNodes[byteAtColun].AppendSymbol(symbol);
                }
            }

            if (ChildNodes != null)
                foreach (var childNode in ChildNodes)
                {
                    if (childNode != null)
                        childNode.Seal();
                }
        }

        public Node[] ChildNodes { get; } 
        
        public bool HasValue { get; set; }

        public Symbol Value { get; set; }
        
        public override string ToString()
        {
            if (HasValue)
                return $"Value node : {Value}  (col : {_column})";

            return $"{Convert.ToString(_baseByte, 2).PadLeft(8, '0')} (col : {_column}), ChildNodes : {ChildNodes.Length} ";
        }

        public bool Match(ReadOnlySpan<byte> @data, out Symbol value)
        {
            var cData = ChildNodes[@data[0]];

           if (cData != null)
            {
                if (cData.HasValue)
                {
                    value = cData.Value;
                    return true;  
                }

                return cData.Match(data.Slice(1), out value); 
            }

            value = default; 
            return false;
        }
    }
}