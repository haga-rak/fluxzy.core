// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Threading.Tasks;

namespace Fluxzy.Core
{
    /// <summary>
    ///  Substitutes a stream with another one.
    /// </summary>
    public interface IStreamSubstitution
    {
        /// <summary>
        /// This class is used by fluxzy internals to substitute a stream with another one.
        /// </summary>
        /// <param name="stream">The original stream (must be drained and disposed) </param>
        /// <returns></returns>
        ValueTask<Stream> Substitute(Stream stream);
    }

}
