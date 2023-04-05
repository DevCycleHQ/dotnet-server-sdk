# DevCycle .NET Server SDK Benchmark 

This utility tool will run some basic benchmarks against the DevCycle .NET Server SDK, which interfaces with the local bucketing library. The benchmark is done locally and utilizes the configuration fixture data in the DevCycle.SDK.Server.Local.MSTests project.  

The benchmarks are built using [BenchmarkDotNet](https://benchmarkdotnet.org/articles/overview.html)

## Running the Benchmark

Benchmarks must be run against a release build of the solution.

    dotnet run -c Release



