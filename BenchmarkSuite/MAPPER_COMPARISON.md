# Mapper Comparison Benchmarks

This benchmark suite provides comprehensive performance comparisons between **FastMapper**, **AutoMapper**, **Mapster**, and **Mapperly** across various mapping scenarios.

## What Gets Measured

The benchmarks measure three key performance metrics:

1. **Speed** - Execution time (mean, median, standard deviation)
2. **CPU Efficiency** - CPU usage and sampling profiler data
3. **Memory Usage** - Memory allocations (Gen0, Gen1, Gen2 collections, and allocated bytes)

## Benchmark Scenarios

### 1. Simple Object Mapping

Maps objects with basic property types (string, int, DateTime, double, bool, Guid).

- **FastMapper** - Uses compiled expression trees with caching
- **AutoMapper** - Industry-standard reflection-based mapper
- **Mapster** - High-performance mapper using code generation
- **Mapperly** - Compile-time source generator for zero-overhead mapping
- **Manual** - Baseline for comparison (hand-written mapping)

### 2. Complex Object Mapping

Maps objects with nested objects and collections (List, Dictionary, HashSet, Queue, Stack).

- Tests deep object graphs
- Validates nested collection handling

### 3. List Mapping

Maps 100-item lists of simple objects.

- Tests collection mapping efficiency
- Measures memory pressure from bulk operations

### 4. Deeply Nested Mapping

Maps objects with 3+ levels of nesting.

- Tests recursive mapping performance
- Validates deep object graph handling

## Running the Benchmarks

### Prerequisites

- .NET 9.0 SDK or later
- Sufficient disk space for benchmark artifacts (~500MB)
- Administrator/root privileges (recommended for accurate CPU profiling)

### Run All Benchmarks

```bash
cd BenchmarkSuite
dotnet run -c Release
```

### Run Specific Benchmark Class

```bash
dotnet run -c Release -- --filter *MapperComparison*
```

### Run with Additional Options

```bash
# Run with detailed memory diagnostics
dotnet run -c Release -- --memory

# Run with CPU sampling profiler
dotnet run -c Release -- --profiler EP

# Export results to different formats
dotnet run -c Release -- --exporters json,html,csv
```

## Interpreting Results

### Example Output

```
| Method                    | Mean      | Error    | StdDev   | Median    | Allocated |
|-------------------------- |----------:|---------:|---------:|----------:|----------:|
| Simple - FastMapper       |  45.23 ns | 0.234 ns | 0.219 ns |  45.10 ns |      96 B |
| Simple - AutoMapper       | 312.45 ns | 2.451 ns | 2.292 ns | 311.80 ns |     512 B |
| Simple - Mapster          |  52.31 ns | 0.412 ns | 0.385 ns |  52.20 ns |     112 B |
| Simple - Mapperly         |  14.67 ns | 0.091 ns | 0.085 ns |  14.65 ns |      96 B |
| Simple - Manual           |  12.45 ns | 0.089 ns | 0.083 ns |  12.43 ns |      96 B |
```

### Key Metrics Explained

- **Mean**: Average execution time - lower is better
- **Error**: Half of 99.9% confidence interval
- **StdDev**: Standard deviation of all measurements
- **Median**: Middle value of all measurements (less affected by outliers)
- **Allocated**: Total memory allocated per operation - lower is better
- **Gen0/Gen1/Gen2**: Garbage collection counts per 1000 operations

### Performance Goals for FastMapper

FastMapper aims to:

1. Be **significantly faster** than AutoMapper (target: 5-10x)
2. Be **competitive with** Mapster (target: within 20%)
3. Be **comparable to** Mapperly (target: within 2-3x, given Mapperly's compile-time advantage)
4. Have **minimal memory allocations** (comparable to manual mapping)
5. Maintain performance with **complex nested structures**

## Benchmark Configuration

The benchmarks use:

- **Job**: MediumRun (15 warmup, 15 iterations)
- **Runtime**: .NET 9.0
- **Diagnosers**: Memory, CPU Usage, Event Pipe Profiler
- **Profile**: CPU Sampling

## Artifacts Location

Results are saved to:

```
BenchmarkSuite/BenchmarkDotNet.Artifacts/results/
```

Files include:

- `*-report.html` - Interactive HTML report
- `*-report.csv` - CSV data for analysis
- `*.etl` - CPU profiling traces (Windows)
- `*.speedscope.json` - CPU profiling traces (cross-platform)

## Cache Configuration

FastMapper benchmarks test with **caching enabled** to simulate real-world usage patterns where mappers are reused across many operations.

To test without caching, modify the benchmark:

```csharp
return _simpleSource.FastMap<SimpleSource, SimpleDestination>(useCache: false);
```

## Continuous Performance Monitoring

Run these benchmarks regularly to:

1. Detect performance regressions
2. Validate optimization improvements
3. Compare against competition
4. Guide architecture decisions

## Additional Resources

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [FastMapper Documentation](../CommonNetFuncs.FastMap/README.md)
- [AutoMapper](https://automapper.org/)
- [Mapster](https://github.com/MapsterMapper/Mapster)
- [Mapperly](https://github.com/riok/mapperly)
