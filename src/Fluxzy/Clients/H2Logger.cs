// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Frames;
using Fluxzy.Core;

namespace Fluxzy.Clients
{
    /// <summary>
    ///     Utility for tracing H2 Connection
    /// </summary>
    internal class H2Logger
    {
        private static readonly string? _directory;

        private static readonly string loggerPath = Environment.ExpandEnvironmentVariables(
            Environment.GetEnvironmentVariable("TracingDirectory")
            ?? "%appdata%/.fluxzy-debug");

        private readonly bool _active;

        static H2Logger()
        {
            if (!DebugContext.IsH2TracingEnabled)
                return;

            _directory = new DirectoryInfo(Path.Combine(loggerPath, "h2")).FullName;
            _directory = Path.Combine(_directory, DebugContext.ReferenceString);

            Directory.CreateDirectory(_directory);

            var hosts = Environment.GetEnvironmentVariable("EnableH2TracingFilterHosts");

            if (!string.IsNullOrWhiteSpace(hosts)) {
                AuthorizedHosts =
                    hosts.Split(new[] { ",", ";", " " }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(s => s.Trim())
                         .ToList();

                return;
            }

            AuthorizedHosts = null;
        }

        public H2Logger(Authority authority, int connectionId, bool? active = null)
        {
            Authority = authority;
            ConnectionId = connectionId;

            active ??= DebugContext.IsH2TracingEnabled;

            _active = active.Value;

            if (_active && AuthorizedHosts != null)

                // Check for domain restriction 
            {
                _active = AuthorizedHosts.Any(c => Authority.HostName.EndsWith(
                    c, StringComparison.OrdinalIgnoreCase));
            }
        }

        public static List<string>? AuthorizedHosts { get; }

        public Authority Authority { get; }

        public int ConnectionId { get; }

        private void WriteLn(int streamIdentifier, string message)
        {
            if (_directory == null)
                return; 
            
            var fullPath = _directory;
            var portString = Authority.Port == 443 ? string.Empty : $"-{Authority.Port:00000}";

            fullPath = Path.Combine(fullPath,
                $"{Authority.HostName}{portString}");

            Directory.CreateDirectory(fullPath);

            fullPath = Path.Combine(fullPath, $"cId={ConnectionId:00000}-sId={streamIdentifier:00000}.txt");

            lock (string.Intern(fullPath)) {
                File.AppendAllText(fullPath,
                    $"[{ITimingProvider.Default.InstantMillis:000000000}] {message}\r\n");
            }
        }

        private void WriteLnHPack(int streamIdentifier, string message)
        {
            var fullPath = _directory!;
            var portString = Authority.Port == 443 ? string.Empty : $"-{Authority.Port:00000}";

            fullPath = Path.Combine(fullPath,
                $"{Authority.HostName}{portString}");

            Directory.CreateDirectory(fullPath);

            fullPath = Path.Combine(fullPath, $"cId={ConnectionId:00000}-hpack.txt");

            lock (string.Intern(fullPath)) {
                File.AppendAllText(fullPath,
                    $"[{ITimingProvider.Default.InstantMillis:000000000}] ({streamIdentifier:00000}) - {message}\r\n");
            }
        }

        private static string GetFrameExtraMessage(ref H2FrameReadResult frame)
        {
            switch (frame.BodyType) {
                case H2FrameType.Data: {
                    var innerFrame = frame.GetDataFrame();

                    return $"Length = {innerFrame.BodyLength}, EndStream = {innerFrame.EndStream}";
                }

                case H2FrameType.Headers: {
                    var innerFrame = frame.GetHeadersFrame();

                    return
                        $"Length = {innerFrame.BodyLength}, EndHeaders = {innerFrame.EndHeaders}, EndStream = {innerFrame.EndStream}";
                }

                case H2FrameType.Priority: {
                    var innerFrame = frame.GetPriorityFrame();

                    return
                        $"Exclusive = {innerFrame.Exclusive}, StreamDependency = {innerFrame.StreamDependency}, Weight = {innerFrame.Weight}";
                }

                case H2FrameType.RstStream: {
                    var innerFrame = frame.GetRstStreamFrame();

                    return $"ErrorCode = {innerFrame.ErrorCode}";
                }

                case H2FrameType.Settings: {
                    var builder = new StringBuilder();
                    var index = 0;

                    while (frame.TryReadNextSetting(out var innerFrame, ref index)) {
                        builder.Append(
                            $"Ack = {innerFrame.Ack}, SettingIdentifier = {innerFrame.SettingIdentifier}, Value = {innerFrame.Value}, ");
                    }

                    return builder.ToString();
                }

                case H2FrameType.PushPromise: {
                    return "";
                }

                case H2FrameType.Ping: {
                    return "";
                }

                case H2FrameType.Goaway: {
                    var innerFrame = frame.GetGoAwayFrame();

                    return $"ErrorCode = {innerFrame.ErrorCode}, LastStreamId = {innerFrame.LastStreamId}";
                }

                case H2FrameType.WindowUpdate: {
                    var innerFrame = frame.GetWindowUpdateFrame();

                    return $"WindowSizeIncrement = {innerFrame.WindowSizeIncrement}";
                }

                case H2FrameType.Continuation: {
                    var innerFrame = frame.GetContinuationFrame();

                    return $"Length = {innerFrame.BodyLength}, EndHeaders = {innerFrame.EndHeaders}";
                }

                default:
                    return "";
            }
        }

        public void IncomingFrame(ref H2FrameReadResult frame)
        {
            if (!_active)
                return;

            var message =
                "RCV <== " +
                $"Type = {frame.BodyType}, " +
                $"Flags = {frame.Flags}, ";

            message += GetFrameExtraMessage(ref frame);

            WriteLn(frame.StreamIdentifier, message);
        }

        public void OutgoingFrame(ref H2FrameReadResult frame)
        {
            if (!_active)
                return;

            var message =
                "SNT ==> " +
                $"Type = {frame.BodyType}, " +
                $"Flags = {frame.Flags}, ";

            message += GetFrameExtraMessage(ref frame);

            WriteLn(frame.StreamIdentifier, message);
        }

        public void OutgoingFrame(ReadOnlyMemory<byte> buffer)
        {
            if (!_active)
                return;

            var frame = H2FrameReader.ReadFrame(ref buffer);

            OutgoingFrame(ref frame);
        }

        public void OutgoingWindowUpdate(int value, int streamIdentifier)
        {
            if (!_active)
                return;

            var message =
                "SNT ==> " +
                $"Type = {H2FrameType.WindowUpdate}, ";

            message += $"WindowSizeIncrement = {value}";
            ;

            WriteLn(streamIdentifier, message);
        }

        public void Trace(int streamId, string message)
        {
            if (!_active)
                return;

            WriteLn(streamId, message);
        }

        public void TraceH(int streamId, Func<string> message)
        {
            if (!_active)
                return;

            WriteLnHPack(streamId, message());
        }

        public void Trace(int streamId, Func<string> messageString)
        {
            if (!_active)
                return;

            WriteLn(streamId, messageString());
        }

        public void TraceDeep(int streamId, Func<string> messageString)
        {
            if (!_active || true)
                return;

            WriteLn(streamId, messageString());
        }

        public void TraceDeep(int streamId, string messageString)
        {
            if (!_active)
                return;

            WriteLn(streamId, messageString);
        }

        public void Trace(Exchange exchange, string preMessage, Exception? ex = null, int streamIdentifier = 0)
        {
            if (!_active)
                return;

            Trace(exchange, streamIdentifier, preMessage + (ex == null ? string.Empty : ex.ToString()));
        }

        public void Trace(
            StreamWorker streamWorker,
            Exchange exchange,
            string preMessage)
        {
            if (!_active)
                return;

            Trace(exchange, streamWorker.StreamIdentifier, preMessage);
        }

        public void TraceResponse(
            StreamWorker streamWorker,
            Exchange exchange)
        {
            if (!_active)
                return;

            var firstLine = exchange.Response.Header?.GetHttp11Header().ToString().Split("\r\n").First();

            Trace(exchange, streamWorker.StreamIdentifier, "Response : " + firstLine);
        }

        public void IncomingSetting(ref SettingFrame settingFrame)
        {
            if (!_active)
                return;

            var message =
                "RCV <== ";

            message +=
                $"Ack = {settingFrame.Ack}, SettingIdentifier = {settingFrame.SettingIdentifier}, Value = {settingFrame.Value}";

            WriteLn(0, message);
        }

        public void OutgoingSetting(ref SettingFrame settingFrame)
        {
            if (!_active)
                return;

            var message =
                "SNT ==> ";

            message +=
                $"Ack = {settingFrame.Ack}, SettingIdentifier = {settingFrame.SettingIdentifier}, Value = {settingFrame.Value}";

            WriteLn(0, message);
        }

        public void Trace(
            Exchange exchange,
            int streamId,
            Func<string> sendMessage)
        {
            if (!_active)
                return;

            Trace(exchange, streamId, sendMessage());
        }

        public void Trace(
            Exchange exchange,
            int streamId,
            string preMessage)
        {
            if (!_active)
                return;

            var method = exchange.Request.Header[":method".AsMemory()].First().Value.ToString();
            var path = exchange.Request.Header[":path".AsMemory()].First().Value.ToString();

            var maxLength = 30;

            if (path.Length > maxLength)
                path = "..." + path.Substring(path.Length - (maxLength - 3), maxLength - 3);

            var message =
                $"{method.PadRight(6, ' ')} - " +
                $"({path}) - " +
                $"Sid = {streamId} " +
                $" - {preMessage}";

            WriteLn(streamId, message);
        }

        public void Trace(
            WindowSizeHolder holder,
            int windowSizeIncrement)
        {
            if (!_active)
                return;

            var message =
                "Window Update - " +
                $"Before = {holder.WindowSize} - " +
                $"Value = {windowSizeIncrement} -  " +
                $"After = {holder.WindowSize + windowSizeIncrement} -  ";

            //$"Sid = {holder.StreamIdentifier} " +

            WriteLn(holder.StreamIdentifier, message);
        }
    }
}
