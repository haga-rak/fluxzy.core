// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fluxzy.Core
{
    public static class ConnectionErrorPageHelper
    {
        private static readonly string ErrorHeader =
            "HTTP/1.1 {0}\r\n" +
            "x-fluxzy: Fluxzy error\r\n" +
            "Content-length: {1}\r\n" +
            "Content-type: text/html\r\n" +
            "Connection : close\r\n\r\n";

        public static (string FlatHeader, byte[] BodyContent) GetPrettyErrorPage(
            Authority authority, 
            IEnumerable<ClientError> clientErrors, Exception? originalException)
        {
            // TODO: This block section is slow due to multiple string concatenation
            // TODO: Consider using a string builder instead

            var headerStatus = "528 Fluxzy error";
            var rawStatusCode = "528"; 

            if (!FluxzySharedSetting.Use528) {
                headerStatus = "502 Bad Gateway";
                rawStatusCode = "502";
            }

            var errorMessage = string.Join("<br/><br/>",
                clientErrors.Select(s => s.Message).Where(s => !string.IsNullOrWhiteSpace(s)));

            if (string.IsNullOrEmpty(errorMessage)) {
                errorMessage = originalException?.Message ?? "Internal fluxzy error.";
            }

            var bodyTemplate = FileStore.error;

            bodyTemplate = bodyTemplate.Replace("@@error-status-code@@", rawStatusCode);
            bodyTemplate = bodyTemplate.Replace("@@error-host@@", authority.ToString());
            bodyTemplate = bodyTemplate.Replace("@@error-message@@", errorMessage);

            var body = Encoding.UTF8.GetBytes(bodyTemplate);
            var header = string.Format(ErrorHeader, headerStatus, body.Length);
            return (header, body);
        }
    }
}
