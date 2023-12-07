using System;

namespace Fluxzy.Clients.Mock
{
    public partial class MockedResponseContent
    {
        internal static MockedResponseContent CreateFromString(string text, int statusCode, string contentType)
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

        public static MockedResponseContent CreateFromPlainText(string text, int statusCode = 200, string contentType = "text/plain")
        {
            return CreateFromString(text, statusCode, contentType);
        }






    }
}
