using System.Collections.Generic;

namespace Fluxzy.Clipboard
{
    /// <summary>
    ///  
    /// </summary>
    public class CopyPolicy
    {
        public CopyPolicy(CopyOptionType type, long? maxSize, List<string>? disallowedExtensions)
        {
            Type = type;
            MaxSize = maxSize;
            DisallowedExtensions = disallowedExtensions;
        }

        /// <summary>
        ///  The copy option type
        /// </summary>
        public CopyOptionType Type { get; }

        /// <summary>
        ///  MaxSize of assets to copy. This is only relevant when the copy option type is memory and doesn't affect headers. 
        /// </summary>
        public long? MaxSize { get; }

        /// <summary>
        ///   Extensions that are not allowed to be copied. Extensions shall not contain the trailing dot.
        /// </summary>
        public List<string>? DisallowedExtensions { get; }
    }
}