using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Echoes
{
    //public abstract partial class HttpMessage
    //{
    //    [JsonProperty]
    //    public HrmVersion Version { get; internal set; }

    //    /// <summary>
    //    /// Header OriginalContent
    //    /// </summary>
    //    [JsonProperty]
    //    public byte[] FullOriginalHeader { get; internal set; }

    //    /// <summary>
    //    /// The original header received from downstream
    //    /// </summary>
    //    [JsonIgnore]
    //    public string FullOriginalHeaderString
    //    {
    //        get
    //        {
    //            if (FullOriginalHeader == null)
    //                return string.Empty;

    //            return Encoding.ASCII.GetString(FullOriginalHeader);
    //        }
    //    }

    //    /// <summary>
    //    /// Header actually sent to the server. 
    //    /// </summary>
    //    [JsonProperty]
    //    public byte [] ForwardableHeader { get; internal set; }

    //    /// <summary>
    //    /// List of Headers. The Dictionary is configured to be key case insensitive
    //    /// </summary>
    //    [JsonIgnore]
    //    public IDictionary<string, List<string>> Headers { get; internal set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);


    //    /// <summary>
    //    /// Only used by newtonsof for serial/desierial process
    //    /// </summary>
    //    [JsonProperty]
    //    internal IDictionary<string, List<string>> RawHeaders
    //    {
    //        get
    //        {
    //            return Headers; 
    //        }
    //        set
    //        {
    //            Headers = new Dictionary<string, List<string>>(value, StringComparer.OrdinalIgnoreCase);
    //        }
    //    }

    //    /// <summary>
    //    /// This is the body size found in "Content-length" header. -1 if not specified
    //    /// </summary>
    //    [JsonProperty]
    //    public long ContentLength { get; internal set; } = -1;

    //    /// <summary>
    //    /// This is the actual downloaded BodySize
    //    /// </summary>
    //    [JsonProperty]
    //    public long OnWireContentLength { get; internal set; }

    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    [JsonProperty]
    //    public bool IsWebSocket { get; internal set; }


    //    [JsonProperty]
    //    public bool Valid { get; internal set; } = true;


    //    [JsonProperty]
    //    public bool Encrypted { get; internal set; } = false;


    //    [JsonProperty]
    //    public IReadOnlyCollection<HttpProxyError> Errors { get; internal set; } = new List<HttpProxyError>();

    //    [JsonProperty]
    //    public bool IsChunkedTransfert { get; internal set; }


    //    private HttpCompressionMode? _compressionMode;

    //    [JsonIgnore]
    //    public HttpCompressionMode CompressionMode
    //    {
    //        get
    //        {
    //            if (_compressionMode != null)
    //                return _compressionMode.Value;

    //            if (!Headers.TryGetValue("Content-encoding", out var list))
    //                return (_compressionMode = HttpCompressionMode.None).Value;

    //            var lastValue = list.LastOrDefault(); // Only last header value count for Content Encoding

    //            if (lastValue == null)
    //                return (_compressionMode = HttpCompressionMode.None).Value;

    //            // Ou Equals

    //            if (lastValue.Equals("gzip", StringComparison.OrdinalIgnoreCase))
    //                return (_compressionMode = HttpCompressionMode.Gzip).Value;

    //            if (lastValue.Equals("deflate", StringComparison.OrdinalIgnoreCase))
    //                return (_compressionMode = HttpCompressionMode.Deflate).Value;

    //            if (lastValue.Equals("bzip2", StringComparison.OrdinalIgnoreCase))
    //                return (_compressionMode = HttpCompressionMode.Bzip2).Value;

    //            if (lastValue.Equals("br", StringComparison.OrdinalIgnoreCase))
    //                return (_compressionMode = HttpCompressionMode.Brotli).Value;

    //            // Unkownn compression mode, considered as uncompressed

    //            return (_compressionMode = HttpCompressionMode.None).Value;
    //        }
    //    }
    //}
}
