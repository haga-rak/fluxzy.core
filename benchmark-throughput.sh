#!/usr/bin/env bash
set -euo pipefail

FILTER="${1:-*ProxyThroughputBenchmark*}"

dotnet build fluxzy.core.slnx -c Release -v q --nologo
dotnet run --project test/Fluxzy.Benchmarks -c Release --no-build -- --filter "$FILTER"
