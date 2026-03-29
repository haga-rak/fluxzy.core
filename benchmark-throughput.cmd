@echo off
setlocal

set FILTER=%~1
if "%FILTER%"=="" set FILTER=*ProxyThroughputBenchmark*

dotnet build fluxzy.core.slnx -c Release -v q --nologo
dotnet run --project test/Fluxzy.Benchmarks -c Release --no-build -- --filter "%FILTER%"
