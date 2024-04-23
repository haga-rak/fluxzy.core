namespace Fluxzy.Tests.Sandbox.Models
{
    /// <summary>
    /// Represents a name and value of a header
    /// </summary>
    public class Header
    {
        /// <summary>
        /// Default construtor 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public Header(string name, string value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>
        /// Name of header
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Value of the header 
        /// </summary>
        public string Value { get; set; }
    }
}