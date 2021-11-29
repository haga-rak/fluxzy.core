using System;
using System.Diagnostics;
using System.Linq;

namespace Echoes.Encoding.Huffman
{
    internal class HPackDecodingTree
    {
        public static HPackDecodingTree Default { get; } = new HPackDecodingTree();

        private HPackDecodingTree()
        {
            var dictionary = HPackDictionary.Instance;


            RootNodes = new Node[dictionary.Symbols.Length];

            Span<byte> buffer = stackalloc byte[8];

            foreach (var symbol in dictionary.Symbols.OrderBy(s => s.LengthBits))
            {
                // Create root nodes 

                // Get first byte 

                var bytes = symbol.GetByteVariation(0, buffer);
                
                Debug.Assert(bytes.Length > 0);

                if (bytes.Length == 1)
                {
                    if (RootNodes[bytes[0]] == null)
                    {
                        RootNodes[bytes[0]] = new Node(bytes[0], 1);
                    }

                    RootNodes[bytes[0]].AppendSymbol(symbol);
                }
                else
                {
                    Node currentNode = null;

                    foreach (var value in bytes)
                    {
                        if (RootNodes[value] == null)
                        {
                            RootNodes[value] = (currentNode ?? new Node(symbol)); // final node 
                        }

                        currentNode = RootNodes[value];
                    }
                }
            }

            for (var index = 0; index < RootNodes.Length; index++)
            {
                var node = RootNodes[index];

                if (node == null)
                    continue; 

                node.Seal();
            }
        }

        /// <summary>
        /// Using a raw array for first byte to improve perf 
        /// </summary>
        public Node [] RootNodes { get; set; }

        public Symbol Read(ReadOnlySpan<byte> data)
        {
            var node = RootNodes[data[0]];

            if (node.HasValue)
                return node.Value;

            if (node.Match(data.Slice(1), out var result))
                return result;

            throw new InvalidOperationException("Decoding error. Dictionnary could not resolve provided data");
        }
    }
}