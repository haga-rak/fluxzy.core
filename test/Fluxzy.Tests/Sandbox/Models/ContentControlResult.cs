namespace Fluxzy.Tests.Sandbox.Models
{
    /// <summary>
    /// Represents the result of an inspection on post, put, patch body 
    /// </summary>
    public class ContentControlResult
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="hash"></param>
        public ContentControlResult(string hash)
        {
            Hash = hash;
        }

        /// <summary>
        /// Base64 value of the hash result 
        /// </summary>
        public string Hash { get; }
    }
}