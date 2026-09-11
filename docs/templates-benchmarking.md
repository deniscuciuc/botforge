# Template Engine Benchmarks

Production-grade benchmarking for the `TeleForge.Templates` template engine.

## Quick Start

```bash
# Run all benchmarks
scripts/benchmark.sh

# Run a specific benchmark class
scripts/benchmark.sh "*SimpleRender*"

# Run all loop-related benchmarks
scripts/benchmark.sh "*Loop*"

# On Windows (PowerShell)
dotnet run --project tools/TeleForge.Templates.Benchmarks -c Release -- --filter *
```

## Benchmark Scenarios

| Scenario | Class | What It Measures |
|---|---|---|
| **Simple Render** | `SimpleRenderBenchmark` | Baseline end-to-end render with 2 parameters |
| **Loop Scaling** | `LoopScalingBenchmark` | Loop expansion at 3, 10, 100, 1000 items |
| **Conditionals** | `ConditionalEvalBenchmark` | If/else evaluation including nested blocks |
| **Formatter Pipeline** | `FormatterPipelineBenchmark` | All 9 built-in formatters in one template |
| **Partial + Layout** | `PartialLayoutBenchmark` | Layout inheritance and partial resolution |
| **Keyboard Builder** | `KeyboardBuilderBenchmark` | Static, small dynamic, and large dynamic keyboards |
| **Parameter Substitution** | `ParameterSubstitutionBenchmark` | Scaling at 5, 25, 50, 100 parameters |
| **Large Template Stress** | `LargeTemplateStressBenchmark` | Full pipeline: 20 sections, formatters, 50-item loop |
| **Startup Loading** | `StartupLoadingBenchmark` | YAML template deserialization at 10, 50, 200 templates |

## Reading Reports

After a run, artifacts are written to `BenchmarkArtifacts/` at the repository root:

```
BenchmarkArtifacts/
  results/
    SimpleRenderBenchmark-report-github.md    # Markdown summary
    SimpleRenderBenchmark-report.html         # Interactive HTML
    SimpleRenderBenchmark-report.csv          # Machine-readable
    SimpleRenderBenchmark-report-full.json    # Full data with distributions
    ...
```

### Key Metrics

| Metric | What to Look For |
|---|---|
| **Mean** | Average time per operation. Primary comparison metric. |
| **P95** | 95th percentile. Flags tail latency issues. |
| **Allocated** | Bytes allocated per operation. Drives GC pressure. |
| **Gen0/Gen1/Gen2** | GC collections per 1000 ops. Gen1+ collections signal allocation problems. |
| **Ratio** | Relative to baseline. >1.0 means slower than baseline. |

### Interpreting Results

- **Simple Render < 5µs**: Healthy baseline
- **Loop scaling linear**: Mean at 1000 items should be ~100× the mean at 10 items
- **Parameter substitution**: Watch for super-linear growth — indicates O(n×m) quadratic cost
- **Allocations per op**: Lower is better. Large jumps indicate string copying

## Optimization Decision Framework

Use these soft gates to decide when optimization is warranted:

| Signal | Threshold | Action |
|---|---|---|
| Mean regression > 20% | Run-to-run comparison | Investigate root cause |
| P95 regression > 30% | vs. baseline | Review recent changes to that code path |
| Allocation per op > 2× baseline | Memory diagnostics | Profile allocation sites |
| Super-linear scaling | Loop/Param benchmarks | Algorithmic change needed |
| Gen1+ GC collections appear | Any scenario | High priority — reduces throughput |

### Creating a Baseline

```bash
# Run full benchmark suite and save results
scripts/benchmark.sh

# Copy the JSON reports as your baseline
cp BenchmarkArtifacts/results/*-full.json baselines/
```

### Comparing Against Baseline

Compare JSON reports manually or use BenchmarkDotNet's `--join` flag:

```bash
dotnet run --project tools/TeleForge.Templates.Benchmarks -c Release -- \
  --filter * --join
```

## Project Structure

```
tools/TeleForge.Templates.Benchmarks/
├── Program.cs                  # BenchmarkSwitcher entry point
├── BenchmarkConfig.cs          # Shared config: exporters, diagnosers, columns
├── BenchmarkFixtures.cs        # Deterministic test data for all scenarios
└── Benchmarks/
    ├── SimpleRenderBenchmark.cs
    ├── LoopScalingBenchmark.cs
    ├── ConditionalEvalBenchmark.cs
    ├── FormatterPipelineBenchmark.cs
    ├── PartialLayoutBenchmark.cs
    ├── KeyboardBuilderBenchmark.cs
    ├── ParameterSubstitutionBenchmark.cs
    ├── LargeTemplateStressBenchmark.cs
    └── StartupLoadingBenchmark.cs
```

## Tips

- Always run in **Release** mode — Debug builds disable JIT optimizations
- Close other CPU-intensive applications during benchmarking
- Use a plugged-in laptop or set power profile to "High Performance"
- Run 2-3 times to verify run-to-run variance before drawing conclusions
- Use `--filter` to isolate individual scenarios when investigating regressions
