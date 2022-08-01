using System;
using Echoes.Clients.H2.Encoder.Utils;

namespace Echoes.Clients.H2.Encoder
{
    /// <summary>
    /// This struct represents a name pair value of a HTTP header.
    /// RequestQuery Path and Method are always represented as HTTP/2.0 pseudo headerfields 
    /// </summary>
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
        /// Name pair value build from span
        /// </summary>
        /// <param name="memoryName"></param>
        /// <param name="memoryValue"></param>
        /// <param name="memoryProvider"></param>
        public HeaderField(ReadOnlySpan<char> memoryName, ReadOnlySpan<char> memoryValue, ArrayPoolMemoryProvider<char> memoryProvider)
        {
            Name = memoryProvider.Allocate(memoryName);
            Value = memoryProvider.Allocate(memoryValue);
        }

        /// <summary>
        /// Header name 
        /// </summary>
        public ReadOnlyMemory<char> Name { get; }

        /// <summary>
        /// Value of Header
        /// </summary>
        public ReadOnlyMemory<char> Value { get; }
        
        /// <summary>
        /// The RFC length for the dynamic table size
        /// </summary>
        public int Size => Name.Length + Value.Length + 32;


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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