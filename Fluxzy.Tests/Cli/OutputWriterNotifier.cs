// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fluxzy.Tests.Cli
{
    public class OutputWriterNotifier : TextWriter
    {
        private readonly Dictionary<string, TimeoutTaskCompletionSource<string>> _runningWait = new ();

        public override Encoding Encoding => Encoding.UTF8;

        public Task<string> WaitForValue(string regexPattern, int timeoutSeconds = 5)
        {
            lock (_runningWait)
            {
                if (!_runningWait.TryGetValue(regexPattern, out var completionSource))
                {
                    _runningWait[regexPattern] = completionSource = new TimeoutTaskCompletionSource<string>(timeoutSeconds);
                    
                    

                }

                return completionSource.CompletionSource.Task;
            }
        }

        public override void Write(string? value)
        {
            if (value != null)
            {
                lock (_runningWait)
                {
                    foreach (var (regexPattern, cancellableTaskSource)
                             in _runningWait.Where(v => !v.Value.CompletionSource.Task.IsCompleted))
                    {
                        var testValue = value;

                        testValue = testValue.Replace("\r", string.Empty);
                        testValue = testValue.Replace("\n", string.Empty);

                        var matchResult = Regex.Match(testValue, regexPattern, RegexOptions.None);

                        if (matchResult.Success && matchResult.Groups.Count > 1)
                        {
                            cancellableTaskSource.CompletionSource.TrySetResult(matchResult.Groups[1].Value);
                            break; 
                        }
                    }
                }
            }

            base.Write(value);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            lock (_runningWait)
            {
                foreach (var cancellableTaskSource in _runningWait.Values)
                {
                    cancellableTaskSource.Dispose();
                }
            }

        }
    }
}