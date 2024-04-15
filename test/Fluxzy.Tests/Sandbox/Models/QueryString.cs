namespace Fluxzy.Tests.Sandbox.Models
{
    /// <summary>
    /// Query string 
    /// </summary>
    public class QueryString
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public QueryString(string name, string value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>
        /// Name 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Values, multiple values with the same name are joined with ", "
        /// </summary>
        public string Value { get; set; }
    }
}