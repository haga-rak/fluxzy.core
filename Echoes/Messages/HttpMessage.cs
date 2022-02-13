using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Echoes
{
    public abstract partial class HttpMessage
    {
        [JsonProperty]
        public Guid Id { get; internal set; } = Guid.NewGuid();

        [JsonProperty]
        internal bool NoBody { get; set; }

        [JsonProperty]
        internal bool ShouldCloseConnection { get; set; }

        /// <summary>
        /// Internal usage only. May be free prematurely if used directly.  Prefere ReadBodyAsStream()
        /// </summary>
        [JsonIgnore]
        internal byte [] Body { get; set; }

        [JsonIgnore]
        internal EchoesArchiveFile ArchiveReference { get; set; }
        
        internal void AddError(string errorMessage, HttpProxyErrorType? errorType = null, string exceptionInformation = null)
        {
            Valid = false;
            ShouldCloseConnection = true;
            ((List<HttpProxyError>)Errors).Add(new HttpProxyError(errorMessage, errorType, exceptionInformation));
        }
    }

    
}