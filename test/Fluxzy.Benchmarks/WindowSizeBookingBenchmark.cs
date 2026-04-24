using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Fluxzy.Clients;
using Fluxzy.Clients.H2;
using Fluxzy.Core;

namespace Fluxzy.Benchmarks;

/// <summary>
///     Isolates the WindowSizeHolder.BookWindowSize overhead that occurs on every H2 DATA frame.
///     Compares: single holder, two-stage (stream + overall), and batched two-stage.
///
///     Run: dotnet run -c Release -- --filter *WindowSizeBooking*
/// </summary>
[MemoryDiagnoser]
public class WindowSizeBookingBenchmark
{
    private const int FrameSize = 16384; // H2 default max frame size
    private const int WindowSize = 1 << 24; // 16 MB — large enough to avoid slow-path waits
    private const int BatchFrames = 4; // matches ServerStreamWorker.BatchFrames

    private WindowSizeHolder _streamHolder = null!;
    private WindowSizeHolder _overallHolder = null!;

    [Params(1, 4, 16)]
    public int ConcurrentTasks { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var authority = new Authority("bench.local", 443, true);
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Re-create holders each iteration so window is always full.
        _streamHolder?.Dispose();
        _overallHolder?.Dispose();

        _streamHolder = new WindowSizeHolder(WindowSize, streamIdentifier: 1);
        _overallHolder = new WindowSizeHolder(WindowSize, streamIdentifier: 0);
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        _streamHolder?.Dispose();
        _overallHolder?.Dispose();
    }

    /// <summary>
    ///     Single holder booking — fast path, no contention.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<int> BookSingleHolder_Uncontended()
    {
        var total = 0;

        for (var i = 0; i < 1024; i++) {
            total += await _streamHolder.BookWindowSize(FrameSize, CancellationToken.None);
        }

        return total;
    }

    /// <summary>
    ///     Two-stage booking (stream + overall) — the per-frame H2 code path.
    /// </summary>
    [Benchmark]
    public async Task<int> BookTwoStage_Uncontended()
    {
        var total = 0;

        for (var i = 0; i < 1024; i++) {
            var streamAllowed = await _streamHolder.BookWindowSize(FrameSize, CancellationToken.None);
            total += await _overallHolder.BookWindowSize(streamAllowed, CancellationToken.None);
        }

        return total;
    }

    /// <summary>
    ///     Two-stage booking under contention: multiple tasks share the overall holder
    ///     (simulating concurrent H2 streams on the same connection).
    /// </summary>
    [Benchmark]
    public async Task<int> BookTwoStage_Contended()
    {
        var tasks = new Task<int>[ConcurrentTasks];

        for (var t = 0; t < ConcurrentTasks; t++) {
            // Each task gets its own stream holder but shares the overall holder.
            var streamHolder = new WindowSizeHolder(WindowSize, streamIdentifier: t + 10);
            var framesPerTask = 1024 / ConcurrentTasks;

            tasks[t] = Task.Run(async () => {
                var sum = 0;

                for (var i = 0; i < framesPerTask; i++) {
                    var streamAllowed = await streamHolder.BookWindowSize(FrameSize, CancellationToken.None);
                    sum += await _overallHolder.BookWindowSize(streamAllowed, CancellationToken.None);
                }

                streamHolder.Dispose();

                return sum;
            });
        }

        var results = await Task.WhenAll(tasks);
        var total = 0;

        foreach (var r in results)
            total += r;

        return total;
    }

    /// <summary>
    ///     Batched two-stage booking (uncontended): book BatchFrames× at once from
    ///     both holders, serve subsequent frames from local budget with zero holder calls.
    ///     Simulates the optimized ServerStreamWorker path.
    /// </summary>
    [Benchmark]
    public async Task<int> BookTwoStage_Batched()
    {
        var budget = 0;
        var total = 0;

        for (var i = 0; i < 1024; i++) {
            if (budget >= FrameSize) {
                budget -= FrameSize;
                total += FrameSize;
                continue;
            }

            var batchRequest = FrameSize * BatchFrames;
            var streamAllowed = await _streamHolder.BookWindowSize(batchRequest, CancellationToken.None);
            var overallAllowed = await _overallHolder.BookWindowSize(streamAllowed, CancellationToken.None);

            var streamRefund = streamAllowed - overallAllowed;
            if (streamRefund > 0)
                _streamHolder.UpdateWindowSize(streamRefund);

            budget += overallAllowed;
            var grant = System.Math.Min(budget, FrameSize);
            budget -= grant;
            total += grant;
        }

        return total;
    }

    /// <summary>
    ///     Batched two-stage under contention: each task batches from its own stream
    ///     holder and the shared overall holder, serving most frames from local budget.
    /// </summary>
    [Benchmark]
    public async Task<int> BookTwoStage_BatchedContended()
    {
        var tasks = new Task<int>[ConcurrentTasks];

        for (var t = 0; t < ConcurrentTasks; t++) {
            var streamHolder = new WindowSizeHolder(WindowSize, streamIdentifier: t + 10);
            var framesPerTask = 1024 / ConcurrentTasks;

            tasks[t] = Task.Run(async () => {
                var budget = 0;
                var sum = 0;

                for (var i = 0; i < framesPerTask; i++) {
                    if (budget >= FrameSize) {
                        budget -= FrameSize;
                        sum += FrameSize;
                        continue;
                    }

                    var batchRequest = FrameSize * BatchFrames;
                    var streamAllowed = await streamHolder.BookWindowSize(batchRequest, CancellationToken.None);
                    var overallAllowed = await _overallHolder.BookWindowSize(streamAllowed, CancellationToken.None);

                    var streamRefund = streamAllowed - overallAllowed;
                    if (streamRefund > 0)
                        streamHolder.UpdateWindowSize(streamRefund);

                    budget += overallAllowed;
                    var grant = System.Math.Min(budget, FrameSize);
                    budget -= grant;
                    sum += grant;
                }

                streamHolder.Dispose();

                return sum;
            });
        }

        var results = await Task.WhenAll(tasks);
        var total = 0;

        foreach (var r in results)
            total += r;

        return total;
    }
}
