using System;
using Fluxzy.Rules.Actions.HighLevelActions;

namespace Fluxzy.Clients.Mock
{
    /// <summary>
    /// Represents a mocked response content.
    /// </summary>
    public partial class MockedResponseContent
    {
        /// <summary>
        /// Create a body content from a file, if contentType is null, it will be inferred from the file extension
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="statusCode"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static MockedResponseContent CreateFromFile(string fileName, int statusCode = 200, string? contentType = null)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Value cannot be empty or whitespace only string.", nameof(fileName));

            if (contentType == null) {
                contentType = ContentTypeResolver.GetContentType(fileName);
            }

            var response = new MockedResponseContent(statusCode, BodyContent.CreateFromFile(fileName));

            if (!string.IsNullOrWhiteSpace(contentType))
                response.Headers.Add(new MockedResponseHeader("Content-Type", contentType));

            return response;
        }

        /// <summary>
        /// Creates a new instance of the MockedResponseContent class with the specified text, status code, and content type.
        /// </summary>
        /// <param name="text">The content of the response as a string.</param>
        /// <param name="statusCode">The HTTP status code to be returned.</param>
        /// <param name="contentType">The MIME type of the content.</param>
        /// <returns>A new instance of the MockedResponseContent class with the specified parameters.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the 'text' parameter is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the 'statusCode' parameter is not within the range of 100 to 599.</exception>
        public static MockedResponseContent CreateFromString(string text, int statusCode, string contentType)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            if (statusCode < 100 || statusCode > 599)
                throw new ArgumentOutOfRangeException(nameof(statusCode));

            var response = new MockedResponseContent(statusCode, BodyContent.CreateFromString(text));

            if (!string.IsNullOrWhiteSpace(contentType))
                response.Headers.Add(new MockedResponseHeader("Content-Type", contentType));

            return response;
        }

        /// <summary>
        /// Creates a <see cref="MockedResponseContent"/> object from a byte array.
        /// </summary>
        /// <param name="data">The data to be included in the response body.</param>
        /// <param name="statusCode">The status code of the response (optional, default is 200).</param>
        /// <param name="contentType">The content type of the response (optional, default is "application/octet-stream").</param>
        /// <returns>A new instance of <see cref="MockedResponseContent"/> with the specified data, status code, and content type.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="data"/> parameter is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="statusCode"/> parameter is less than 100 or greater than 599.</exception>
        public static MockedResponseContent CreateFromByteArray(
            byte[] data, int statusCode = 200, string contentType = "application/octet-stream")
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (statusCode < 100 || statusCode > 599)
                throw new ArgumentOutOfRangeException(nameof(statusCode));

            var response = new MockedResponseContent(statusCode, BodyContent.CreateFromArray(data));

            if (!string.IsNullOrWhiteSpace(contentType))
                response.Headers.Add(new MockedResponseHeader("Content-Type", contentType));

            return response;
        }

        /// <summary>
        /// Creates a mocked response content object from plain text.
        /// </summary>
        /// <param name="text">The text content.</param>
        /// <param name="statusCode">The HTTP status code of the mocked response content. Default value is 200.</param>
        /// <param name="contentType">The content type of the mocked response content. Default value is "text/plain".</param>
        /// <returns>A MockedResponseContent object representing the mocked response content.</returns>
        public static MockedResponseContent CreateFromPlainText(
            string text, int statusCode = 200, string contentType = "text/plain")
        {
            return CreateFromString(text, statusCode, contentType);
        }

        /// <summary>
        /// Creates a mocked response content object from a JSON content string.
        /// </summary>
        /// <param name="jsonContent">The JSON content string to be used.</param>
        /// <param name="statusCode">The status code of the response (default is 200).</param>
        /// <param name="contentType">The content type of the response (default is "application/json").</param>
        /// <returns>A MockedResponseContent object created from the provided JSON content.</returns>
        public static MockedResponseContent CreateFromJsonContent(
            string jsonContent, int statusCode = 200, string contentType = "application/json")
        {
            return CreateFromString(jsonContent, statusCode, contentType);
        }

        /// <summary>
        /// Creates a mocked response content from HTML content.
        /// </summary>
        /// <param name="htmlContent">The HTML content of the response.</param>
        /// <param name="statusCode">The status code of the response (default is 200).</param>
        /// <param name="contentType">The content type of the response (default is "text/html").</param>
        /// <returns>A mocked response content object.</returns>
        public static MockedResponseContent CreateFromHtmlContent(
            string htmlContent, int statusCode = 200, string contentType = "text/html")
        {
            return CreateFromString(htmlContent, statusCode, contentType);
        }

        /// <summary>
        /// Creates a MockedResponseContent object from XML content.
        /// </summary>
        /// <param name="xmlContent">The XML content to be used in the response.</param>
        /// <param name="statusCode">The HTTP status code for the response. Default value is 200.</param>
        /// <param name="contentType">The content type for the response. Default value is "text/xml".</param>
        /// <returns>A MockedResponseContent object representing the response.</returns>
        public static MockedResponseContent CreateFromXmlContent(
            string xmlContent, int statusCode = 200, string contentType = "text/xml")
        {
            return CreateFromString(xmlContent, statusCode, contentType);
        }

        /// <summary>
        /// Creates a mocked response content from a JSON file.
        /// </summary>
        /// <param name="fileName">The name of the JSON file.</param>
        /// <param name="statusCode">The status code of the response content (default is 200).</param>
        /// <param name="contentType">The content type of the response content (default is "application/json").</param>
        /// <returns>A mocked response content object.</returns>
        public static MockedResponseContent CreateFromJsonFile(
            string fileName, int statusCode = 200, string contentType = "application/json")
        {
            return CreateFromFile(fileName, statusCode, contentType);
        }

        /// <summary>
        /// Creates a <see cref="MockedResponseContent"/> object by reading the contents of an HTML file.
        /// </summary>
        /// <param name="fileName">The path to the HTML file to be read.</param>
        /// <param name="statusCode">The HTTP status code to be associated with the mocked response. Default value is 200.</param>
        /// <param name="contentType">The content-type of the mocked response. Default value is "text/html".</param>
        /// <returns>A <see cref="MockedResponseContent"/> object representing the contents of the HTML file.</returns>
        public static MockedResponseContent CreateFromHtmlFile(
            string fileName, int statusCode = 200, string contentType = "text/html")
        {
            return CreateFromFile(fileName, statusCode, contentType);
        }

        /// <summary>
        /// Creates a MockedResponseContent object from an XML file.
        /// </summary>
        /// <param name="fileName">The path of the XML file.</param>
        /// <param name="statusCode">The status code of the mock response. Default is 200.</param>
        /// <param name="contentType">The content type of the mock response. Default is 'text/xml'.</param>
        /// <returns>A MockedResponseContent object representing the XML file content.</returns>
        public static MockedResponseContent CreateFromXmlFile(
            string fileName, int statusCode = 200, string contentType = "text/xml")
        {
            return CreateFromFile(fileName, statusCode, contentType);
        }

        /// <summary>
        /// Creates a MockedResponseContent object from a plain text file.
        /// </summary>
        /// <param name="fileName">The name of the plain text file.</param>
        /// <param name="statusCode">The status code of the response. Defaults to 200.</param>
        /// <param name="contentType">The content type of the response. Defaults to "text/plain".</param>
        /// <returns>A MockedResponseContent object.</returns>
        public static MockedResponseContent CreateFromPlainTextFile(
            string fileName, int statusCode = 200, string contentType = "text/plain")
        {
            return CreateFromFile(fileName, statusCode, contentType);
        }

        /// <summary>
        /// Creates a mocked response content with the specified status code.
        /// </summary>
        /// <param name="statusCode">The status code of the mocked response content.</param>
        /// <returns>A mocked response content object with the specified status code.</returns>
        public static MockedResponseContent CreateEmptyWithStatusCode(int statusCode)
        {
            return CreateFromString(string.Empty, statusCode, string.Empty);
        }
    }
}
