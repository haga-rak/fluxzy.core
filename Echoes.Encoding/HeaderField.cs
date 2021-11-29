using System;
using Echoes.Encoding.Utils.Interfaces;

namespace Echoes.Encoding
{
    public readonly struct HeaderField
    {
        /// <summary>
        /// Create a new HeaderField from a string name and a string value 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public HeaderField(string name, string value)
        : this (name.AsMemory(), value.AsMemory())
        {
        }

        /// <summary>
        /// Create a new HeaderField with a string name. Value is empty. 
        /// </summary>
        /// <param name="name"></param>
        public HeaderField(string name)
            : this(name.AsMemory())
        {
        }

        /// <summary>
        /// Create HeaderField from a ReadOnlyMemory name and value. 
        /// </summary>
        /// <param name="memoryName"></param>
        /// <param name="memoryValue"></param>
        public HeaderField(ReadOnlyMemory<char> memoryName, ReadOnlyMemory<char> memoryValue)
        {
            Name = memoryName;
            Value = memoryValue;
        }

        /// <summary>
        /// Create HeaderField from a ReadOnlyMemory name. Value is empty. 
        /// </summary>
        /// <param name="memoryName"></param>
        public HeaderField(ReadOnlyMemory<char> memoryName)
            : this(memoryName, default)
        {
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="memoryName"></param>
        /// <param name="memoryValue"></param>
        /// <param name="memoryProvider"></param>
        public HeaderField(ReadOnlySpan<char> memoryName, ReadOnlySpan<char> memoryValue, IMemoryProvider<char> memoryProvider)
        {
            Name = memoryProvider.Allocate(memoryName);
            Value = memoryProvider.Allocate(memoryValue);
        }

        /// <summary>
        /// Header name 
        /// </summary>
        public ReadOnlyMemory<char> Name { get; }

        public ReadOnlyMemory<char> Value { get; }
        
        public int Size => Name.Length + Value.Length + 32;

        public override string ToString()
        {
            if (Value.Length > 0)
            {
                return $"{Name} : {Value}";
            }

            return $"{Name}";
        }
        
    }
}