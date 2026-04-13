using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.Stacks;

namespace TraceContentionAnalyzer;

/// <summary>
///     Loads a .nettrace captured with Microsoft-Windows-DotNETRuntime contention+stack
///     keywords and aggregates CLR ContentionStart events by their managed call stack so
///     you can see which locks are hot.
/// </summary>
internal static class Program
{
    private const int DefaultTopStacks = 15;
    private const int DefaultStackDepth = 20;

    private static int Main(string[] args)
    {
        if (args.Length < 1) {
            Console.Error.WriteLine("Usage: TraceContentionAnalyzer <trace.nettrace> [topStacks] [stackDepth]");
            return 2;
        }

        var tracePath = args[0];
        var topStacks = args.Length >= 2 ? int.Parse(args[1]) : DefaultTopStacks;
        var stackDepth = args.Length >= 3 ? int.Parse(args[2]) : DefaultStackDepth;

        if (!File.Exists(tracePath)) {
            Console.Error.WriteLine($"File not found: {tracePath}");
            return 2;
        }

        Console.Error.WriteLine($"Indexing {Path.GetFileName(tracePath)} (may take a moment)...");

        var etlxPath = TraceLog.CreateFromEventPipeDataFile(tracePath);

        try {
            using var traceLog = new TraceLog(etlxPath);
            AnalyzeContention(traceLog, topStacks, stackDepth);
        }
        finally {
            try { File.Delete(etlxPath); } catch { /* ignore */ }
        }

        return 0;
    }

    private static void AnalyzeContention(TraceLog traceLog, int topStacks, int stackDepth)
    {
        var stacks = new Dictionary<string, StackAggregate>(StringComparer.Ordinal);
        var source = traceLog.Events.GetSource();

        var totalEvents = 0;
        var withStack = 0;

        source.Clr.ContentionStart += evt => {
            totalEvents++;
            var callStack = evt.CallStack();

            if (callStack == null)
                return;

            withStack++;

            var frames = new List<string>(stackDepth);
            var cursor = callStack;
            var depth = 0;

            while (cursor != null && depth < stackDepth) {
                var method = cursor.CodeAddress.FullMethodName;

                if (!string.IsNullOrEmpty(method))
                    frames.Add(method);

                cursor = cursor.Caller;
                depth++;
            }

            if (frames.Count == 0)
                return;

            var key = string.Join("\n", frames);

            if (!stacks.TryGetValue(key, out var agg)) {
                agg = new StackAggregate { Frames = frames };
                stacks[key] = agg;
            }

            agg.Count++;
        };

        source.Process();

        Console.WriteLine($"Total ContentionStart events : {totalEvents:N0}");
        Console.WriteLine($"With resolvable stack        : {withStack:N0}");
        Console.WriteLine($"Unique stacks                : {stacks.Count:N0}");
        Console.WriteLine();

        if (stacks.Count == 0) {
            Console.WriteLine("No stacks captured. Verify the trace includes the Stack (0x40000000) keyword.");
            return;
        }

        var top = stacks.Values
            .OrderByDescending(s => s.Count)
            .Take(topStacks)
            .ToList();

        var totalWithStack = Math.Max(withStack, 1);

        for (var i = 0; i < top.Count; i++) {
            var agg = top[i];
            var pct = 100d * agg.Count / totalWithStack;

            Console.WriteLine($"=== #{i + 1}  count={agg.Count:N0}  ({pct:F1}% of stacked)");

            foreach (var frame in agg.Frames)
                Console.WriteLine($"  {frame}");

            Console.WriteLine();
        }

        // Lock-site rollup: skip past System.Threading.Monitor.* frames to find the first
        // user-visible caller — that is the `lock(...)` site. More actionable than the raw
        // leaf count (which is always Monitor.Enter_Slowpath).
        var siteCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var agg in stacks.Values) {
            var site = FindLockSite(agg.Frames);

            if (site == null)
                continue;

            if (!siteCounts.TryGetValue(site, out var n))
                n = 0;

            siteCounts[site] = n + agg.Count;
        }

        Console.WriteLine("=== Top lock sites (first caller outside System.Threading.Monitor) ===");

        foreach (var (site, count) in siteCounts.OrderByDescending(kv => kv.Value).Take(topStacks)) {
            var pct = 100d * count / totalWithStack;
            Console.WriteLine($"  {count,8:N0}  {pct,5:F1}%  {site}");
        }
    }

    private static string? FindLockSite(List<string> frames)
    {
        foreach (var frame in frames) {
            if (frame.StartsWith("System.Threading.Monitor.", StringComparison.Ordinal))
                continue;

            if (frame.StartsWith("System.Threading.ManualResetEventSlim.", StringComparison.Ordinal))
                continue;

            return frame;
        }

        return null;
    }

    private sealed class StackAggregate
    {
        public int Count;
        public List<string> Frames = new();
    }
}
