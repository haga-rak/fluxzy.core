namespace Fluxzy.Tests.Sandbox.Models
{
    /// <summary>
    /// Information about the payload 
    /// </summary>
    public class RequestContent
    {
        /// <summary>
        /// Value of "content-length "
        /// </summary>
        public long? Length { get; set; }

        /// <summary>
        /// Value of "content-type"
        /// </summary>
        public string? Type { get; set; }

        public string? Hash { get; set; }
    }
}