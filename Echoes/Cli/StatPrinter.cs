using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Echoes.Cli
{
    public class StatPrinter : IDisposable
    {
        private readonly int _topPosition;
        private readonly int _consoleWidth = 78; 
        private readonly BufferBlock<Exchange> _exchangeBlock = new();
        private readonly CancellationTokenSource _tokenSource = new();
        private readonly Task _workTask;
        private readonly StatViewModel _viewModel;

        public StatPrinter(int topPosition, string boundAddress, int boundPort)
        {
            _topPosition = topPosition;
            _viewModel = new StatViewModel()
            {
                BoundAddress = boundAddress,
                BoundPort = boundPort
            };
            _workTask = null; // InnerRun();

        }

        public async Task OnNewExchange(Exchange exchange)
        {
            await _exchangeBlock.SendAsync(exchange).ConfigureAwait(false);
        }

        private async Task InnerRun()
        {
            try
            {
                do
                {
                    _exchangeBlock.TryReceiveAll(out var exchanges); 

                    if (exchanges == null)
                        exchanges = new List<Exchange>();

                    foreach (var exchange in exchanges)
                    {
                        if (exchange?.Request?.Header?.Path == null)
                            continue;

                        var s = exchange?.Request?.Header?.Path;

                        if (!Uri.TryCreate(
                                s.ToString(), UriKind.Absolute, out var uri))
                            continue; 


                        if (uri.Scheme == "http")
                        {
                            _viewModel.PlainRequestCount++;

                            _viewModel.PlainRequestSize +=
                                (exchange.Request.Header.RawHeader.Length +
                                 exchange.Request.Header.ContentLength);

                            if (exchange?.Response.Body != null)
                            {
                                _viewModel.PlainResponseCount++;

                                _viewModel.PlainResponseSize +=
                                    (exchange.Response.Header.RawHeader.Length +
                                     exchange.Response.Header.ContentLength);
                            }
                        }

                        if (uri.Scheme == "https")
                        {
                            _viewModel.SecureRequestCount++;

                            _viewModel.SecureRequestSize +=
                                (exchange.Request.Header.RawHeader.Length +
                                 exchange.Request.Header.ContentLength);

                            if (exchange?.Response.Body != null)
                            {
                                _viewModel.SecureResponseCount++;

                                _viewModel.SecureResponseSize +=
                                    (exchange.Response.Header.RawHeader.Length +
                                     exchange.Response.Header.ContentLength);
                            }
                        }

                    }

                    await Print(_viewModel).ConfigureAwait(false);
                    await Task.Delay(500, _tokenSource.Token).ConfigureAwait(false);
                }
                while (
                    !_tokenSource.IsCancellationRequested &&
                    await _exchangeBlock.OutputAvailableAsync(_tokenSource.Token).ConfigureAwait(false)); 
            }
            catch (OperationCanceledException)
            {
                // Natural death
            }
        }

        private async Task Print(StatViewModel viewModel)
        {
           // return;

            Console.SetCursorPosition(0, _topPosition);

            await Console.Out.WriteLineAsync($" Proxy is listening on {viewModel.BoundAddress}:{viewModel.BoundPort}").ConfigureAwait(false);
            await Console.Out.WriteLineAsync(new string('-', _consoleWidth)).ConfigureAwait(false);

            int titleWidth = 40;

            var values = new Dictionary<string, PrintRow>()
            {
                { "Processed requests",
                    new PrintRow()
                    {
                        Count = viewModel.TotalRequest,
                        Highlight = true,
                        Size = viewModel.TotalRequestSize
                    } } ,
                { "Processed responses",
                    new PrintRow()
                    {
                        Count = viewModel.TotalResponse,
                        Highlight = true,
                        Size = viewModel.TotalResponseSize
                    } } ,
                { "Plain request",
                    new PrintRow()
                    {
                        Count = viewModel.PlainRequestCount,
                        Highlight = false,
                        Size = viewModel.PlainRequestSize
                    } } ,
                { "Plain response",
                    new PrintRow()
                    {
                        Count = viewModel.PlainResponseCount,
                        Highlight = false,
                        Size = viewModel.PlainResponseSize
                    } } ,
                { "Secure request",
                    new PrintRow()
                    {
                        Count = viewModel.SecureRequestCount,
                        Highlight = false,
                        Size = viewModel.SecureRequestSize
                    } } ,
                { "Secure response",
                    new PrintRow()
                    {
                        Count = viewModel.SecureResponseCount,
                        Highlight = false,
                        Size = viewModel.SecureResponseSize
                    } } ,
            };

            foreach (var kp in values)
            {
                await Console.Out.WriteAsync("|").ConfigureAwait(false);
                await Console.Out.WriteAsync(Padding(titleWidth, kp.Key, false));
                await Console.Out.WriteAsync("|").ConfigureAwait(false);


                var colSize = (_consoleWidth - 4 - titleWidth) / 2;

                var curColor = Console.ForegroundColor = Console.ForegroundColor; 

                if (kp.Value.Highlight)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }

                await Console.Out.WriteAsync(Padding(colSize, $"{kp.Value.Count}" , true));


                if (kp.Value.Highlight)
                {
                    Console.ForegroundColor = curColor;
                }


                await Console.Out.WriteAsync("|").ConfigureAwait(false);
                await Console.Out.WriteAsync(Padding(colSize, $"{kp.Value.FormattedSize}" , true));

                await Console.Out.WriteAsync("|").ConfigureAwait(false);
                await Console.Out.WriteLineAsync().ConfigureAwait(false);
            }

            await Console.Out.WriteLineAsync(new string('-', _consoleWidth)).ConfigureAwait(false);


        }

        private static string Padding(int size, string str, bool leading)
        {
            str = leading ? str + " " : " " + str;
            var remainder = size - str.Length;
            return leading ? new string(' ', remainder) + str : str + new string(' ', remainder);
        }

        public void Dispose()
        {
            _tokenSource.Cancel();
            _workTask?.GetAwaiter().GetResult();
        }
    }

    class PrintRow
    {
        public int Count { get; set; }

        public long Size { get; set; }

        public bool Highlight { get; set; }

        public string FormattedSize => GetBytesReadable(Size);

        private string GetBytesReadable(long i)
        {
            // Get absolute value
            long absoluteI = (i < 0 ? -i : i);

            // Determine the suffix and readable value
            string suffix;
            double readable;
            if (absoluteI >= 0x1000000000000000) // Exabyte
            {
                suffix = "E";
                readable = (i >> 50);
            }
            else if (absoluteI >= 0x4000000000000) // Petabyte
            {
                suffix = "PB";
                readable = (i >> 40);
            }
            else if (absoluteI >= 0x10000000000) // Terabyte
            {
                suffix = "TB";
                readable = (i >> 30);
            }
            else if (absoluteI >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = (i >> 20);
            }
            else if (absoluteI >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = (i >> 10);
            }
            else if (absoluteI >= 0x400) // Kilobyte
            {
                suffix = "KB";
                readable = i;
            }
            else
            {
                return i.ToString("0 B"); // Byte
            }
            // Divide by 1024 to get fractional value
            readable = (readable / 1024);
            // Return formatted number with suffix
            return readable.ToString("0.# ") + suffix;
        }
    }
}