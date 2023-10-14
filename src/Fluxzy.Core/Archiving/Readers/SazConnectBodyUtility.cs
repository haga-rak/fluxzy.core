// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Security.Authentication;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Fluxzy.Readers
{
    internal static class SazConnectBodyUtility
    {
        public static SslProtocols ? GetSslVersion(string contentBody)
        {
            var regex = new Regex(@"Version:\s*[0-9\.]+\s*\((?<sslVersion>.*)\)");
            var match = regex.Match(contentBody);

            if (match.Success) {
                var sslVersion = match.Groups["sslVersion"].Value;
                if (sslVersion == "TLS/1.2")
                    return SslProtocols.Tls12;
#if NETCOREAPP3_1_OR_GREATER
                if (sslVersion == "TLS/1.3")
                    return SslProtocols.Tls13;
#endif
                if (sslVersion == "TLS/1.1")
#pragma warning disable SYSLIB0039
                    return SslProtocols.Tls11;
#pragma warning restore SYSLIB0039
                if (sslVersion == "TLS/1")
#pragma warning disable SYSLIB0039
                    return SslProtocols.Tls;
#pragma warning restore SYSLIB0039
                if (sslVersion == "SSL/3")
#pragma warning disable CS0618
                    return SslProtocols.Ssl3;
#pragma warning restore CS0618
                if (sslVersion == "SSL/2")
#pragma warning disable CS0618
                    return SslProtocols.Ssl2;
#pragma warning restore CS0618
            }

            return null; 
        }

        public static string ? GetKeyExchange(string?  contentBody)
        {
            if (string.IsNullOrWhiteSpace(contentBody))
                return null;

            var regex = new Regex(@"Key Exchange: (?<keyExchange>.+)\s*$", RegexOptions.Multiline);

            var match = regex.Match(contentBody);

            if (match.Success) {
                return match.Groups["keyExchange"].Value;
            }

            return null; 
        }

        public static string ? GetCertificateCn(string?  contentBody)
        {
            if (string.IsNullOrWhiteSpace(contentBody))
                return null;

            var regex = new Regex(@"(?<cn>CN=.+)\s*$", RegexOptions.Multiline);

            var match = regex.Match(contentBody);

            if (match.Success) {
                return match.Groups["cn"].Value;
            }

            return null; 
        }

        public static string ? GetCertificateIssuer(string?  contentBody)
        {
            if (string.IsNullOrWhiteSpace(contentBody))
                return null;

            var regex = new Regex(@"(?<cn>CN=.+)\s*$", RegexOptions.Multiline);

            var matches = regex.Matches(contentBody);

            if (matches.Count > 1) {
                return matches[1].Groups["cn"].Value;
            }

            return null; 
        }


        public static DateTime ?  GetSessionTimersValue(this XElement element, string attributeName)
        {
            var res = DateTime.TryParse(element.XPathSelectElement($"//SessionTimers")?.Attribute(attributeName)?.Value, out var result);

            if (!res)
                return null; 

            return result; 
        }

        public static int  GetSessionDurationValue(this XElement element, string attributeName)
        {
            var res = int.TryParse(element.XPathSelectElement($"//SessionTimers[@{attributeName}]")?.Value, out var result);

            if (!res)
                return 0; 

            return result; 
        }

        public static int GetSessionFlagsAttributeAsInteger(this XElement element, string keyName)
        {
            var target = element.XPathSelectElement($"//SessionFlags/SessionFlag[@N=\"{keyName}\"]");

            int.TryParse(target?.Attribute("V")?.Value, out var result);

            return result; 
        }

        public static int GetSessionId(this XElement element)
        {
            var target = element.XPathSelectElement($"/")?.Attribute("SID")?.Value;

            int.TryParse(target, out var result);

            return result; 
        }

        public static string? GetSessionFlagsAttributeAsString(this XElement element, string keyName)
        {
            var target = element.XPathSelectElement($"//SessionFlags/SessionFlag[@N=\"{keyName}\"]");
            
            return target?.Attribute("V")?.Value; 
        }


        public static int? GetConnectId(this XElement element)
        {
            var rawValue = element.GetSessionFlagsAttributeAsString("x-serversocket"); 

            if (string.IsNullOrWhiteSpace(rawValue))
                return null;

            rawValue = rawValue.Replace("REUSE ServerPipe#", string.Empty);

            if (!int.TryParse(rawValue, out var result))
                return null; 

            return result;
        }


        public static bool IsConnectionOpener(this XElement element)
        {
            return
                element.XPathSelectElement($"//PipeInfo")?.Attribute("Reused")?.Value?.Equals("false") ?? true;
        }
    }
}
