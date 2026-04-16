@echo off
setlocal

set FILTER=
set SHORT_ARGS=

:parse_args
if "%~1"=="" goto done_args
if /i "%~1"=="--short" (
    set SHORT_ARGS=--warmupCount 10 --iterationCount 20 --launchCount 1
    shift
    goto parse_args
)
if /i "%~1"=="--contention" (
    rem Opt-in CLR contention ETW trace (EventPipe). Produces .nettrace per run
    rem in BenchmarkDotNet.Artifacts/. Open in PerfView / VS / speedscope.
    set FLUXZY_BENCH_CONTENTION=1
    shift
    goto parse_args
)
if /i "%~1"=="--alloc" (
    rem Opt-in CLR allocation ETW trace (EventPipe). Produces .nettrace per run
    rem with sampled GC/AllocationTick events + managed stacks. Defaults to
    rem shorter iterations since the trace overhead skews absolute numbers.
    rem Open in PerfView ("GC Heap Alloc Ignore Free (Coarse Sampling) Stacks").
    set FLUXZY_BENCH_ALLOC=1
    if "%SHORT_ARGS%"=="" set SHORT_ARGS=--warmupCount 1 --iterationCount 5 --launchCount 1
    shift
    goto parse_args
)
if /i "%~1"=="--h2-8k" (
    rem H2 + 8192 body only, ~30%% of default duration
    set SHORT_ARGS=--warmupCount 2 --iterationCount 10 --launchCount 1
    set FILTER=*ProxyThroughputBenchmark*True*8192*
    shift
    goto parse_args
)
if /i "%~1"=="--h2-0k" (
    rem H2 + 0 body only, ~30%% of default duration
    set SHORT_ARGS=--warmupCount 2 --iterationCount 10 --launchCount 1
    set FILTER=*ProxyThroughputBenchmark*True*0*
    shift
    goto parse_args
)
set FILTER=%~1
shift
goto parse_args
:done_args

if "%FILTER%"=="" set FILTER=*ProxyThroughputBenchmark*

dotnet build fluxzy.core.slnx -c Release -v q --nologo
dotnet run --project test/Fluxzy.Benchmarks -c Release --no-build -- --filter "%FILTER%" %SHORT_ARGS%
