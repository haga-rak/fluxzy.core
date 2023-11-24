// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Threading.Tasks;

namespace Fluxzy.Core
{
    /// <summary>
    ///  Low level interface to substitute a request or response body.
    ///  Provided an implementation of this interface to ExchangeContext in order to substitute a request or response body.
    /// </summary>
    public interface IStreamSubstitution
    {
        /// <summary>
        /// This class is used to low level-mock request and response body.
        /// Even if this class is async for fast mocking purpose, calling async in the implementation will lead to unecessary overhead.
        /// Additionally, you must drain (read to EOF) the provided stream in order to not hang the remote connection when
        /// the original stream is coming from a remote connection.
        /// </summary>
        /// <param name="originalStream">The original stream (must be drained and disposed) </param>
        /// <returns>The stream that will be send to the client</returns>
        ValueTask<Stream> Substitute(Stream originalStream);
    }

}
