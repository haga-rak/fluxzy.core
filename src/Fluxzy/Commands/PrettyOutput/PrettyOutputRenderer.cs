// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Writers;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Fluxzy.Cli.Commands.PrettyOutput
{
    /// <summary>
    /// Main orchestrator for pretty console output using Spectre.Console.
    /// Displays a live statistics panel and exchange table with interactive controls.
    /// </summary>
    public class PrettyOutputRenderer : IAsyncDisposable
    {
        private readonly CircularExchangeBuffer _buffer;
        private readonly ProxyStatistics _statistics;
        private readonly FluxzySetting _setting;
        private readonly int _maxBufferRows;
        private readonly CancellationToken _externalToken;
        private readonly CancellationTokenSource _internalCts;
        private readonly IAnsiConsole _console;

        private volatile bool _isPaused;
        private volatile bool _isDisposed;
        private Task? _inputTask;

        // Scrolling state
        // _scrollOffset = 0 means viewing the latest (bottom of buffer)
        // _scrollOffset > 0 means scrolled up by that many rows
        private int _scrollOffset;
        private volatile bool _autoScroll = true; // When true, always show latest entries
        private readonly object _scrollLock = new();

        // Track connections for accurate counting
        private readonly HashSet<int> _activeConnectionIds = new();
        private readonly object _connectionLock = new();

        public PrettyOutputRenderer(
            FluxzySetting setting,
            int maxRows,
            CancellationToken externalToken,
            IAnsiConsole? console = null)
        {
            _setting = setting ?? throw new ArgumentNullException(nameof(setting));
            _maxBufferRows = maxRows;
            _externalToken = externalToken;
            _internalCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            _console = console ?? AnsiConsole.Console;

            _buffer = new CircularExchangeBuffer(maxRows);
            _statistics = new ProxyStatistics();
            _isPaused = false;
            _scrollOffset = 0;
        }

        /// <summary>
        /// Gets the maximum number of rows to display based on terminal height.
        /// Recalculated on each call to handle window resizing.
        /// </summary>
        private int GetMaxDisplayRows()
        {
            try
            {
                // Reserve space for: stats panel (7 lines) + help bar (1 line) + table header/border (3 lines) + margins (4 lines)
                const int reservedLines = 15;
                var terminalHeight = Console.WindowHeight;
                return Math.Max(5, Math.Min(terminalHeight - reservedLines, _maxBufferRows));
            }
            catch
            {
                return 15; // Default fallback - safe for most terminals
            }
        }

        /// <summary>
        /// Subscribes to proxy events for exchange and connection updates.
        /// </summary>
        public void SubscribeToProxy(Proxy proxy)
        {
            if (proxy == null)
                throw new ArgumentNullException(nameof(proxy));

            proxy.Writer.ExchangeUpdated += OnExchangeUpdated;
            proxy.Writer.ConnectionUpdated += OnConnectionUpdated;
            proxy.Writer.ErrorUpdated += OnErrorUpdated;
        }

        /// <summary>
        /// Unsubscribes from proxy events.
        /// </summary>
        public void UnsubscribeFromProxy(Proxy proxy)
        {
            if (proxy == null)
                return;

            proxy.Writer.ExchangeUpdated -= OnExchangeUpdated;
            proxy.Writer.ConnectionUpdated -= OnConnectionUpdated;
            proxy.Writer.ErrorUpdated -= OnErrorUpdated;
        }

        /// <summary>
        /// Starts the render and input loops. This method blocks until cancelled.
        /// </summary>
        public async Task RunAsync()
        {
            var linkedToken = _internalCts.Token;

            _inputTask = Task.Run(() => InputLoop(linkedToken), linkedToken);

            try
            {
                await RenderLoop(linkedToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
        }

        private void OnExchangeUpdated(object? sender, ExchangeUpdateEventArgs e)
        {
            // Only process completed exchanges
            if (e.UpdateType != ArchiveUpdateType.AfterResponse)
                return;

            var info = e.ExchangeInfo;
            var metrics = info.Metrics;

            var responseTime = metrics.ResponseBodyEnd > metrics.ReceivedFromProxy
                ? (metrics.ResponseBodyEnd - metrics.ReceivedFromProxy).TotalMilliseconds
                : 0;

            var entry = new ExchangeDisplayEntry
            {
                Id = info.Id,
                Timestamp = metrics.ReceivedFromProxy != default ? metrics.ReceivedFromProxy : DateTime.Now,
                Method = info.Method ?? "---",
                Host = TruncateString(info.KnownAuthority ?? "---", 30),
                Path = TruncateString(info.Path ?? "/", 40),
                StatusCode = info.StatusCode,
                Size = metrics.TotalReceived,
                ResponseTimeMs = responseTime,
                HasError = info.ClientErrors.Any()
            };

            _buffer.Add(entry);

            // Record statistics with uploaded bytes
            _statistics.RecordExchange(
                entry.StatusCode,
                metrics.TotalReceived,
                metrics.TotalSent,
                entry.HasError);
        }

        private void OnConnectionUpdated(object? sender, ConnectionUpdateEventArgs e)
        {
            lock (_connectionLock)
            {
                if (_activeConnectionIds.Add(e.Connection.Id))
                {
                    _statistics.IncrementConnections();
                }
            }
        }

        private void OnErrorUpdated(object? sender, DownstreamErrorEventArgs e)
        {
            // Errors are already counted in exchange updates through HasError
        }

        private async Task RenderLoop(CancellationToken token)
        {
            await _console.Live(BuildLayout())
                .AutoClear(false)
                .Overflow(VerticalOverflow.Ellipsis)
                .StartAsync(async ctx =>
                {
                    var lastRender = DateTime.MinValue;
                    const int minIntervalMs = 100; // Max 10 renders per second

                    while (!token.IsCancellationRequested)
                    {
                        var elapsed = (DateTime.UtcNow - lastRender).TotalMilliseconds;

                        if (elapsed >= minIntervalMs)
                        {
                            ctx.UpdateTarget(BuildLayout());
                            lastRender = DateTime.UtcNow;
                        }

                        try
                        {
                            await Task.Delay(50, token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                });
        }

        private async Task InputLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(intercept: true);

                        // Handle special keys (arrows, page up/down, home/end)
                        if (key.Key != default)
                        {
                            HandleSpecialKey(key.Key);
                        }

                        // Handle character keys
                        switch (char.ToLower(key.KeyChar))
                        {
                            case 'p':
                                _isPaused = true;
                                break;
                            case 'r':
                                _isPaused = false;
                                break;
                            case 'd':
                                _buffer.Clear();
                                lock (_scrollLock)
                                {
                                    _scrollOffset = 0;
                                    _autoScroll = true;
                                }
                                break;
                        }
                    }

                    await Task.Delay(50, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (InvalidOperationException)
                {
                    // Console.KeyAvailable can throw when not available
                    await Task.Delay(100, token);
                }
            }
        }

        private void HandleSpecialKey(ConsoleKey key)
        {
            lock (_scrollLock)
            {
                var maxDisplayRows = GetMaxDisplayRows();
                var bufferCount = _buffer.Count;
                var maxScrollOffset = Math.Max(0, bufferCount - maxDisplayRows);

                switch (key)
                {
                    case ConsoleKey.PageUp:
                        _scrollOffset = Math.Min(_scrollOffset + maxDisplayRows, maxScrollOffset);
                        _autoScroll = false;
                        break;

                    case ConsoleKey.PageDown:
                        _scrollOffset = Math.Max(_scrollOffset - maxDisplayRows, 0);
                        if (_scrollOffset == 0)
                            _autoScroll = true;
                        break;

                    case ConsoleKey.UpArrow:
                        _scrollOffset = Math.Min(_scrollOffset + 1, maxScrollOffset);
                        _autoScroll = false;
                        break;

                    case ConsoleKey.DownArrow:
                        _scrollOffset = Math.Max(_scrollOffset - 1, 0);
                        if (_scrollOffset == 0)
                            _autoScroll = true;
                        break;

                    case ConsoleKey.Home:
                        _scrollOffset = maxScrollOffset;
                        _autoScroll = false;
                        break;

                    case ConsoleKey.End:
                        _scrollOffset = 0;
                        _autoScroll = true;
                        break;
                }
            }
        }

        private IRenderable BuildLayout()
        {
            var layout = new Layout("Root")
                .SplitRows(
                    new Layout("Stats").Size(7),
                    new Layout("Table"),
                    new Layout("Help").Size(1)
                );

            layout["Stats"].Update(BuildStatisticsPanel());
            layout["Table"].Update(BuildExchangeTable());
            layout["Help"].Update(BuildHelpBar());

            return layout;
        }

        private Panel BuildStatisticsPanel()
        {
            var grid = new Grid()
                .AddColumn(new GridColumn().NoWrap())
                .AddColumn(new GridColumn().NoWrap())
                .AddColumn(new GridColumn().NoWrap())
                .AddColumn(new GridColumn().NoWrap());

            // Row 1: Exchange counts
            grid.AddRow(
                new Markup($"[bold]Exchanges:[/] {_statistics.TotalExchanges}"),
                new Markup($"[green]Success:[/] {_statistics.SuccessCount}"),
                new Markup($"[red]Errors:[/] {_statistics.ErrorCount}"),
                new Markup($"[blue]Connections:[/] {_statistics.ActiveConnections}")
            );

            // Row 2: Configuration
            var sslEngine = _setting.UseBouncyCastle ? "[cyan]BouncyCastle[/]" : "[dim]OS Default[/]";
            var pcapStatus = _setting.CaptureRawPacket ? "[green]ON[/]" : "[dim]OFF[/]";
            var outputDir = _setting.ArchivingPolicy.Directory != null
                ? TruncateString(_setting.ArchivingPolicy.Directory, 40)
                : "[dim]None[/]";

            grid.AddRow(
                new Markup($"[bold]SSL:[/] {sslEngine}"),
                new Markup($"[bold]PCAP:[/] {pcapStatus}"),
                new Markup($"[bold]Output:[/] {outputDir}"),
                new Markup("")
            );

            // Row 3: Bandwidth and scroll info
            var pauseIndicator = _isPaused ? "[yellow bold]PAUSED[/]" : "";
            var scrollIndicator = _autoScroll ? "[dim]LIVE[/]" : $"[yellow]SCROLL +{_scrollOffset}[/]";

            grid.AddRow(
                new Markup($"[cyan]Download:[/] {FormatBytes(_statistics.TotalDownloaded)}"),
                new Markup($"[yellow]Upload:[/] {FormatBytes(_statistics.TotalUploaded)}"),
                new Markup($"[dim]Buffer:[/] {_buffer.Count}/{_buffer.Capacity}"),
                new Markup($"{scrollIndicator} {pauseIndicator}")
            );

            return new Panel(grid)
                .Header("[bold]Proxy Statistics[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Blue);
        }

        private Table BuildExchangeTable()
        {
            var table = new Table()
                .Border(TableBorder.Simple)
                .BorderColor(Color.Grey)
                .AddColumn(new TableColumn("TIME").Width(12))
                .AddColumn(new TableColumn("METHOD").Width(7))
                .AddColumn(new TableColumn("HOST").Width(30))
                .AddColumn(new TableColumn("PATH").Width(40))
                .AddColumn(new TableColumn("STATUS").Width(6).Centered())
                .AddColumn(new TableColumn("SIZE").Width(8).RightAligned())
                .AddColumn(new TableColumn("MS").Width(7).RightAligned());

            // Get dynamic display rows based on terminal height
            var maxDisplayRows = GetMaxDisplayRows();

            // Get all entries from buffer
            var allEntries = _buffer.GetSnapshot();
            var totalCount = allEntries.Length;

            int scrollOffset;
            bool autoScroll;

            lock (_scrollLock)
            {
                // Clamp scroll offset to valid range
                var maxScrollOffset = Math.Max(0, totalCount - maxDisplayRows);
                _scrollOffset = Math.Min(_scrollOffset, maxScrollOffset);
                scrollOffset = _scrollOffset;
                autoScroll = _autoScroll;
            }

            // Calculate which entries to show
            // allEntries is in chronological order (oldest first)
            // We want to show the latest entries by default (scrollOffset = 0)
            // scrollOffset > 0 means we're scrolled back in time

            ExchangeDisplayEntry[] entriesToShow;

            if (totalCount <= maxDisplayRows)
            {
                // Buffer has fewer entries than display rows - show all
                entriesToShow = allEntries;
            }
            else
            {
                // Calculate the starting index
                // When scrollOffset = 0, show the last maxDisplayRows entries
                // When scrollOffset > 0, show entries further back
                var endIndex = totalCount - scrollOffset;
                var startIndex = Math.Max(0, endIndex - maxDisplayRows);
                var count = Math.Min(maxDisplayRows, endIndex - startIndex);

                entriesToShow = new ExchangeDisplayEntry[count];
                Array.Copy(allEntries, startIndex, entriesToShow, 0, count);
            }

            // Build caption
            var captionParts = new List<string>();

            if (_isPaused)
            {
                captionParts.Add("[yellow]PAUSED - press 'r' to resume[/]");
            }

            if (!autoScroll && scrollOffset > 0)
            {
                var viewStart = totalCount - scrollOffset - entriesToShow.Length + 1;
                var viewEnd = totalCount - scrollOffset;
                captionParts.Add($"[dim]Showing {viewStart}-{viewEnd} of {totalCount} - press End to go to latest[/]");
            }

            if (captionParts.Count > 0)
            {
                table.Caption(string.Join(" | ", captionParts));
            }

            // Add rows
            foreach (var entry in entriesToShow)
            {
                var statusMarkup = FormatStatusCode(entry.StatusCode);
                var methodMarkup = FormatMethod(entry.Method);

                table.AddRow(
                    entry.Timestamp.ToString("HH:mm:ss.fff"),
                    methodMarkup,
                    Markup.Escape(entry.Host),
                    Markup.Escape(entry.Path),
                    statusMarkup,
                    FormatBytes(entry.Size),
                    entry.ResponseTimeMs.ToString("F0")
                );
            }

            return table;
        }

        private static Markup BuildHelpBar()
        {
            return new Markup("[dim]p: pause | r: resume | d: discard | PgUp/PgDn: scroll | Home/End: jump | Ctrl+C: exit[/]");
        }

        private static string FormatStatusCode(int statusCode)
        {
            return statusCode switch
            {
                >= 200 and < 300 => $"[green]{statusCode}[/]",
                >= 300 and < 400 => $"[yellow]{statusCode}[/]",
                >= 400 and < 500 => $"[red]{statusCode}[/]",
                >= 500 => $"[red bold]{statusCode}[/]",
                0 => "[dim]---[/]",
                _ => statusCode.ToString()
            };
        }

        private static string FormatMethod(string method)
        {
            return method.ToUpperInvariant() switch
            {
                "GET" => "[green]GET[/]",
                "POST" => "[yellow]POST[/]",
                "PUT" => "[blue]PUT[/]",
                "DELETE" => "[red]DELETE[/]",
                "PATCH" => "[cyan]PATCH[/]",
                "HEAD" => "[dim]HEAD[/]",
                "OPTIONS" => "[dim]OPTIONS[/]",
                _ => method
            };
        }

        private static string FormatBytes(long bytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            return bytes switch
            {
                >= GB => $"{bytes / (double)GB:F1}GB",
                >= MB => $"{bytes / (double)MB:F1}MB",
                >= KB => $"{bytes / (double)KB:F1}KB",
                _ => $"{bytes}B"
            };
        }

        private static string TruncateString(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (value.Length <= maxLength)
                return value;

            return value.Substring(0, maxLength - 3) + "...";
        }

        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            try
            {
                _internalCts.Cancel();
            }
            catch
            {
                // Ignore cancellation exceptions
            }

            if (_inputTask != null)
            {
                try
                {
                    await _inputTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
            }

            _internalCts.Dispose();
        }
    }
}
