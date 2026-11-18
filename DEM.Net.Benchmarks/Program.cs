using BenchmarkDotNet.Running;
using DEM.Net.Benchmarks;

// dotnet run -c Release --project DEM.Net.Benchmarks -- --filter '*'
// Baselines for the async / GeoPoint / hot path work: compare Mean and Allocated before and after each change.
// dotnet run -c Release --project DEM.Net.Benchmarks -- quality: vertical error of the mesh reductions
if (args is ["quality"])
    ReductionQuality.Run();
else
    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
