using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace TraceAllocationAnalyzer;

/// <summary>
///     Loads a .nettrace captured with Microsoft-Windows-DotNETRuntime GC+Type+Stack
///     keywords (Verbose) and aggregates GC/AllocationTick events.
///
///     Each AllocationTick event represents ~100 KB of allocations since the last tick
///     on the same thread, so we weight each event as 100 KB for total-bytes estimates
///     (this matches PerfView's "GC Heap Alloc Ignore Free (Coarse Sampling)" view).
///
///     Produces three roll-ups per trace:
///       1. By managed type       (who allocates the most bytes)
///       2. By top managed frame  (nearest user/framework frame on the stack)
///       3. By Fluxzy frame       (first frame in Fluxzy.* — the hotspot inside our code)
/// </summary>
internal static class Program
{
    private const long BytesPerTick = 100_000;       // AllocationTick samples ~every 100 KB
    private const int DefaultTop = 20;
    private const int DefaultStackDepth = 40;

    private static int Main(string[] args)
    {
        if (args.Length < 1) {
            Console.Error.WriteLine("Usage: TraceAllocationAnalyzer <trace.nettrace> [top=20] [stackDepth=40]");
            return 2;
        }

        var tracePath = args[0];
        var top = args.Length >= 2 ? int.Parse(args[1]) : DefaultTop;
        var stackDepth = args.Length >= 3 ? int.Parse(args[2]) : DefaultStackDepth;

        if (!File.Exists(tracePath)) {
            Console.Error.WriteLine($"File not found: {tracePath}");
            return 2;
        }

        Console.Error.WriteLine($"Indexing {Path.GetFileName(tracePath)} ...");
        var etlxPath = TraceLog.CreateFromEventPipeDataFile(tracePath);

        try {
            using var traceLog = new TraceLog(etlxPath);
            Analyze(traceLog, Path.GetFileName(tracePath), top, stackDepth);
        }
        finally {
            try { File.Delete(etlxPath); } catch { /* ignore */ }
        }

        return 0;
    }

    private static void Analyze(TraceLog traceLog, string traceName, int top, int stackDepth)
    {
        var byType = new Dictionary<string, long>(StringComparer.Ordinal);
        var byTopFrame = new Dictionary<string, long>(StringComparer.Ordinal);
        var byFluxzyFrame = new Dictionary<string, long>(StringComparer.Ordinal);
        var byStack = new Dictionary<string, StackAggregate>(StringComparer.Ordinal);

        var totalEvents = 0;
        var withStack = 0;
        long totalAllocationBytes = 0; // sum of AllocationAmount64 (triggering-object bytes)

        var source = traceLog.Events.GetSource();

        source.Clr.GCAllocationTick += evt => {
            totalEvents++;
            totalAllocationBytes += evt.AllocationAmount64;

            var typeName = string.IsNullOrEmpty(evt.TypeName) ? "<unknown>" : evt.TypeName;
            Add(byType, typeName, BytesPerTick);

            var callStack = evt.CallStack();
            if (callStack == null) return;
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

            if (frames.Count == 0) return;

            // Nearest meaningful managed frame (top of stack, skipping empty names).
            Add(byTopFrame, frames[0], BytesPerTick);

            // First Fluxzy.* frame — the user-code hotspot responsible for the allocation.
            var fluxzy = frames.FirstOrDefault(f =>
                f.StartsWith("Fluxzy.", StringComparison.Ordinal) ||
                f.StartsWith("fluxzy!", StringComparison.Ordinal) ||
                f.Contains("!Fluxzy.", StringComparison.Ordinal));

            if (fluxzy != null)
                Add(byFluxzyFrame, fluxzy, BytesPerTick);

            var key = string.Join("\n", frames.Take(8)); // 8-deep stack fingerprint
            if (!byStack.TryGetValue(key, out var agg)) {
                agg = new StackAggregate { Frames = frames, TypeName = typeName };
                byStack[key] = agg;
            }
            agg.Bytes += BytesPerTick;
            agg.Count++;
        };

        source.Process();

        var estimatedTotal = (long) totalEvents * BytesPerTick;

        Console.WriteLine();
        Console.WriteLine($"========== {traceName} ==========");
        Console.WriteLine($"AllocationTick events  : {totalEvents:N0}");
        Console.WriteLine($"With stack             : {withStack:N0} ({Pct(withStack, totalEvents)})");
        Console.WriteLine($"Estimated bytes (×100K): {FormatBytes(estimatedTotal)}");
        Console.WriteLine($"Sum AllocationAmount64 : {FormatBytes(totalAllocationBytes)}  (size of triggering objects)");
        Console.WriteLine();

        PrintTop("Top allocated types", byType, estimatedTotal, top);
        PrintTop("Top allocating frames (nearest managed frame)", byTopFrame, estimatedTotal, top);
        PrintTop("Top Fluxzy frames (first Fluxzy.* on stack)", byFluxzyFrame, estimatedTotal, top);
        PrintTopStacks("Top stacks (8-deep)", byStack, estimatedTotal, Math.Min(top, 10));
    }

    private static void PrintTop(string title, Dictionary<string, long> map, long totalBytes, int top)
    {
        Console.WriteLine($"--- {title} ---");
        Console.WriteLine($"{"Bytes",12}  {"%",6}  Name");
        foreach (var kv in map.OrderByDescending(kv => kv.Value).Take(top)) {
            Console.WriteLine($"{FormatBytes(kv.Value),12}  {Pct(kv.Value, totalBytes),6}  {kv.Key}");
        }
        Console.WriteLine();
    }

    private static void PrintTopStacks(string title, Dictionary<string, StackAggregate> map, long totalBytes, int top)
    {
        Console.WriteLine($"--- {title} ---");
        var rank = 0;
        foreach (var agg in map.Values.OrderByDescending(a => a.Bytes).Take(top)) {
            rank++;
            Console.WriteLine($"#{rank}  bytes={FormatBytes(agg.Bytes)}  count={agg.Count:N0}  ({Pct(agg.Bytes, totalBytes)})  type={agg.TypeName}");
            foreach (var f in agg.Frames.Take(8))
                Console.WriteLine($"      {f}");
            Console.WriteLine();
        }
    }

    private static void Add(Dictionary<string, long> map, string key, long value)
    {
        if (!map.TryGetValue(key, out var cur)) cur = 0;
        map[key] = cur + value;
    }

    private static string Pct(long a, long b)
        => b <= 0 ? "-" : (100d * a / b).ToString("F1", CultureInfo.InvariantCulture) + "%";

    private static string Pct(int a, int b)
        => b <= 0 ? "-" : (100d * a / b).ToString("F1", CultureInfo.InvariantCulture) + "%";

    private static string FormatBytes(long bytes)
    {
        double v = bytes;
        string[] u = { "B", "KB", "MB", "GB" };
        var i = 0;
        while (v >= 1024 && i < u.Length - 1) { v /= 1024; i++; }
        return v.ToString("F2", CultureInfo.InvariantCulture) + " " + u[i];
    }

    private sealed class StackAggregate
    {
        public long Bytes;
        public int Count;
        public string TypeName = "";
        public List<string> Frames = new();
    }
}
