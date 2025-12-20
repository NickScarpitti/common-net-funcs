# Quick Start Guide - Mapper Benchmarks

## Run Complete Comparison

To run all mapper comparison benchmarks and get comprehensive results:

```bash
cd BenchmarkSuite
dotnet run -c Release
```

This will output results showing:

- **Execution time** (Mean, Median, StdDev)
- **Memory allocations** (Allocated bytes, GC collections)
- **CPU efficiency** (CPU usage via Event Pipe Profiler)

## Understanding the Results

### Speed Comparison

Look at the **Mean** column - this shows average execution time:

- Lower numbers = faster
- FastMapper should be 5-10x faster than AutoMapper
- FastMapper should be competitive with Mapster (within 20%)

### Memory Efficiency

Look at the **Allocated** column:

- Lower numbers = less memory pressure
- Less memory allocation = fewer garbage collections
- FastMapper aims for minimal allocations similar to manual mapping

### CPU Efficiency

After the run completes, check the `BenchmarkDotNet.Artifacts` folder for:

- `.speedscope.json` files - Open at https://www.speedscope.app/ to visualize CPU hotspots
- ETL files (Windows) - Open with PerfView or Windows Performance Analyzer

## Quick Test Run

For a faster test run during development (less accurate but quicker):

```bash
dotnet run -c Release -- --job short
```

## Export Results

Export to multiple formats for analysis:

```bash
dotnet run -c Release -- --exporters json,html,csv,markdown
```

Results will be in: `BenchmarkDotNet.Artifacts/results/`

## Filter Specific Benchmarks

### Run only Simple mapping benchmarks:

```bash
dotnet run -c Release -- --filter "*Simple*"
```

### Run only Complex mapping benchmarks:

```bash
dotnet run -c Release -- --filter "*Complex*"
```

### Run only List mapping benchmarks:

```bash
dotnet run -c Release -- --filter "*List*"
```

### Run only Nested mapping benchmarks:

```bash
dotnet run -c Release -- --filter "*Nested*"
```

## Baseline Comparison

To see percentage differences from a baseline (Manual mapping):

The benchmarks already include Manual mapping as a baseline in simple scenarios. Results will show how much slower each mapper is compared to hand-written code.

## Troubleshooting

### "Process was terminated" errors

- Close other applications to free memory
- Run with admin/sudo privileges for accurate profiling

### Inconsistent results

- Close background applications
- Disable CPU frequency scaling
- Run multiple times and average results

### Missing artifacts

- Check `BenchmarkDotNet.Artifacts/results/` folder
- Ensure you ran with `-c Release` configuration

## Sample Output Interpretation

```
| Method                  | Mean      | Allocated |
|------------------------ |----------:|----------:|
| Simple - FastMapper     |  45.23 ns |      96 B |
| Simple - AutoMapper     | 312.45 ns |     512 B |
| Simple - Mapster        |  52.31 ns |     112 B |
| Simple - Mapperly       |  14.67 ns |      96 B |
| Simple - Manual         |  12.45 ns |      96 B |
```

**Analysis:**

- FastMapper is **6.9x faster** than AutoMapper (312.45 / 45.23)
- FastMapper is **1.2x slower** than Mapster (52.31 / 45.23)
- FastMapper is **3.1x slower** than Mapperly (45.23 / 14.67) - Mapperly uses compile-time generation
- FastMapper is **3.6x slower** than manual mapping (45.23 / 12.45)
- FastMapper allocates **5.3x less memory** than AutoMapper (512 / 96)
- FastMapper allocates similar memory to Manual/Mapperly mapping (96 B each)

## Next Steps

1. Run the benchmarks: `dotnet run -c Release`
2. Review the generated HTML report in `BenchmarkDotNet.Artifacts/results/`
3. Analyze CPU profiles using speedscope.app
4. Compare against performance goals in MAPPER_COMPARISON.md
5. Identify optimization opportunities

## Performance Goals

FastMapper should demonstrate:

- ✅ 5-10x faster than AutoMapper
- ✅ Competitive with Mapster (within 20%)
- ✅ Reasonable compared to Mapperly (compile-time source generation gives Mapperly an advantage)
- ✅ Minimal memory allocations
- ✅ Scales well with complexity
