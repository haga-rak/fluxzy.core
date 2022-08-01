// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Echoes.Clients.H2;

namespace Echoes.Clients
{
    internal class H1Logger
    {
        public static List<string> AuthorizedHosts { get; }

        static H1Logger()
        {
            var hosts = Environment.GetEnvironmentVariable("EnableH1TracingFilterHosts");

            if (!string.IsNullOrWhiteSpace(hosts))
            {
                AuthorizedHosts =
                    hosts.Split(new[] { ",", ";", " " }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToList();

                return;
            }

            AuthorizedHosts = null;
        }

        public Authority Authority { get; }
    
        private readonly bool _active;
        private readonly string _directory;

        public H1Logger(Authority authority,  bool? active = null)
        {
            Authority = authority;

            active ??= string.Equals(Environment.GetEnvironmentVariable("EnableH1Tracing"),
                "true", StringComparison.OrdinalIgnoreCase);

            var loggerPath = Environment.ExpandEnvironmentVariables(Environment.GetEnvironmentVariable("TracingDirectory")
                             ?? "%appdata%/echoes-debug"); 

            _active = active.Value;

            if (_active && AuthorizedHosts != null)
            {
                // Check for domain restriction 
                _active = AuthorizedHosts.Any(c => Authority.HostName.ToString().EndsWith(
                    c, StringComparison.OrdinalIgnoreCase));
            }

            _directory = new DirectoryInfo(Path.Combine(loggerPath, "h1")).FullName;
            _directory = Path.Combine(_directory, DebugContext.ReferenceString);

            Directory.CreateDirectory(_directory);
        }

        private void WriteLn(
            int exchangeId, string message)
        {
            var fullPath = _directory;
            var portString = Authority.Port == 443 ? string.Empty : $"-{Authority.Port:00000}";

            fullPath = Path.Combine(fullPath,
                $"{Authority.HostName}{portString}");

            Directory.CreateDirectory(fullPath);

            fullPath = Path.Combine(fullPath, $"exId={exchangeId:00000}.txt");

            lock (string.Intern(fullPath))
                File.AppendAllText(fullPath,
                    $"[{ITimingProvider.Default.InstantMillis:000000000}] {message}\r\n");
        }

        public void TraceResponse(Exchange exchange, bool full = false)
        {
            if (!_active)
                return;

            var firstLine = full? 
                exchange.Response.Header.RawHeader.ToString() : 
                exchange.Response.Header.RawHeader.ToString().Split("\r\n").First();

            Trace(exchange.Id,  $"Response : " + firstLine);
        }

        public void Trace(
            int exchangeId, string message)
        {
            if (!_active)
                return;

            WriteLn(exchangeId, message);
        }


        public void Trace(
            int exchangeId,
            Func<string> sendMessage)
        {

            if (!_active)
                return;

            Trace(exchangeId, sendMessage());
        }

        public void Trace(Exchange exchange, string preMessage, Exception ex = null)
        {
            if (!_active)
                return;

            Trace(exchange, preMessage + (ex == null ? string.Empty : ex.ToString()));
        }

        public void Trace(StreamWorker streamWorker,
            Exchange exchange,
            string preMessage)
        {
            if (!_active)
                return;

            Trace(exchange,  preMessage);
        }
    

        public void Trace(
            Exchange exchange,
            Func<string> sendMessage)
        {

            if (!_active)
                return;

            Trace(exchange, sendMessage());
        }

        public void Trace(
            Exchange exchange,
            string preMessage)
        {
            if (!_active)
                return;

            var method = exchange.Request.Header[":method".AsMemory()].First().Value.ToString();
            var path = exchange.Request.Header[":path".AsMemory()].First().Value.ToString();

            int maxLength = 30;

            if (path.Length > maxLength)
            {
                path = "..." + path.Substring(path.Length - (maxLength - 3), (maxLength - 3));
            }

            var message =
                $"{method.PadRight(6, ' ')} - " +
                $"({path}) - " +
                $"Cid = {exchange.Connection?.Id ?? 0} " +
                $" - {preMessage}";

            WriteLn(exchange.Id, message);
        }
    
    }
}