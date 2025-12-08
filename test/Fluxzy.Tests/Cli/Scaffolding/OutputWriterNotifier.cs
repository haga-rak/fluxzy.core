// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-r

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Tests.Cli.Scaffolding
{
    public class OutputWriterNotifier : TextWriter
    {
        private readonly bool _hook;
        private readonly Dictionary<string, TimeoutTaskCompletionSource<string>> _runningWait = new();

        private readonly StringBuilder _builder = new();

        public OutputWriterNotifier(bool hook = false)
        {
            _hook = hook;
        }

        public override Encoding Encoding { get; } = new UTF8Encoding(false);

        public override void Write(string? value)
        {
            if (value != null) {
                if (_hook) {
                    _builder.Append(value);
                }

                lock (_runningWait) {
                    foreach (var (regexPattern, cancellableTaskSource)
                             in _runningWait.Where(v => !v.Value.CompletionSource.Task.IsCompleted)) {
                        var testValue = value;

                        testValue = testValue.Replace("\r", string.Empty);
                        testValue = testValue.Replace("\n", string.Empty);

                        var matchResult = Regex.Match(testValue, regexPattern, RegexOptions.None);

                        if (matchResult.Success && matchResult.Groups.Count > 1) {
                            cancellableTaskSource.CompletionSource.TrySetResult(matchResult.Groups[1].Value);

                            break;
                        }
                    }
                }
            }

            base.Write(value);
        }

        public Task<string> WaitForValue(string regexPattern, CancellationToken token, int timeoutSeconds = 5)
        {
            lock (_runningWait) {
                if (!_runningWait.TryGetValue(regexPattern, out var completionSource)) {
                    _runningWait[regexPattern] = completionSource =
                        new TimeoutTaskCompletionSource<string>(timeoutSeconds, regexPattern, token);
                }

                return completionSource.CompletionSource.Task;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            lock (_runningWait) {
                foreach (var cancellableTaskSource in _runningWait.Values) {
                    cancellableTaskSource.Dispose();
                }
            }
        }

        public string GetOutput()
        {
            return _builder.ToString();
        }
    }
}
