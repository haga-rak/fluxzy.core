using System.Collections.Generic;

namespace Fluxzy.Tests.Sandbox.Models
{
    /// <summary>
    /// Represents a global check on request 
    /// </summary>
    public class HealthCheckResult
    {
        /// <summary>
        /// Request path 
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Header sent by the caller. May include headers forwaded by the reverse proxy 
        /// </summary>
        public List<Header>? Headers { get; set; }

        /// <summary>
        /// Query string sent by the caller
        /// </summary>
        public List<QueryString>? Queries { get; set; }

        /// <summary>
        /// Protocol version  
        /// </summary>
        public string? HttpVersion { get; set; }

        /// <summary>
        /// Caller method 
        /// </summary>
        public string? Method { get; set; }

        /// <summary>
        /// Information about the payload 
        /// </summary>
        public RequestContent RequestContent { get; set; } = new RequestContent();
    }
}