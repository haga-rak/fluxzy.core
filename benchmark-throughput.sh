#!/usr/bin/env bash
set -euo pipefail

SHORT_ARGS=""
FILTER=""

# Parse options
while [[ $# -gt 0 ]]; do
    case "$1" in
        --short)
            SHORT_ARGS="--warmupCount 1 --iterationCount 3 --launchCount 1"
            shift
            ;;
        --h2-8k)
            # H2 + 8192 body only, ~30% of default duration
            SHORT_ARGS="--warmupCount 2 --iterationCount 5 --launchCount 1"
            FILTER="*ProxyThroughputBenchmark*True*8192*"
            shift
            ;;
        *)
            FILTER="$1"
            shift
            ;;
    esac
done

FILTER="${FILTER:-*ProxyThroughputBenchmark*}"

dotnet build fluxzy.core.slnx -c Release -v q --nologo
dotnet run --project test/Fluxzy.Benchmarks -c Release --no-build -- --filter "$FILTER" $SHORT_ARGS
