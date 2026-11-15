using BenchmarkDotNet.Running;

// dotnet run -c Release --project DEM.Net.Benchmarks -- --filter '*'
// Baselines for the async / GeoPoint / hot path work: compare Mean and Allocated before and after each change.
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
