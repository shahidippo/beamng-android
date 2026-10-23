using System.Diagnostics;
using System.Text.Json;

namespace ImpactDrive.HeadlessTests;

internal sealed record Measurement(string Name, double Value, string Unit);

internal sealed record CheckResult(string Name, string Status, double DurationMs, string? Error);

/// <summary>A package-free runner for the production physics source, not a reimplementation.</summary>
internal sealed class TestSuite
{
    private readonly List<CheckResult> results = new();
    private readonly List<Measurement> measurements = new();

    public void Measure(string name, double value, string unit)
    {
        measurements.Add(new(name, value, unit));
        Console.WriteLine($"MEASURE {name}: {value:F4} {unit}");
    }

    public void Run(string name, Action test)
    {
        var timer = Stopwatch.StartNew();
        try
        {
            test();
            results.Add(new(name, "passed", timer.Elapsed.TotalMilliseconds, null));
            Console.WriteLine($"PASS {name}");
        }
        catch (Exception error)
        {
            results.Add(new(name, "failed", timer.Elapsed.TotalMilliseconds, error.ToString()));
            Console.WriteLine($"FAIL {name}: {error.Message}");
        }
    }

    public int Finish(string? reportPath)
    {
        int passed = results.Count(r => r.Status == "passed");
        int failed = results.Count - passed;
        Console.WriteLine($"\n{results.Count} checks executed: {passed} passed; {failed} failed; 0 skipped.");
        Console.WriteLine("Scope: pure C# model invariants, synthetic fixed-load longitudinal coupling and save persistence. Unity/PhysX, Android, rendering and device performance are not validated by this runner.");
        if (!string.IsNullOrWhiteSpace(reportPath))
        {
            string absolute = Path.GetFullPath(reportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
            File.WriteAllText(absolute, JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                generatedUtc = DateTimeOffset.UtcNow,
                scope = "Production pure C# model invariants and save persistence; synthetic fixed-load longitudinal bench is diagnostic only; excludes Unity/PhysX and Android validation",
                total = results.Count,
                passed,
                failed,
                skipped = 0,
                checks = results,
                measurements
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        return failed == 0 && results.Count > 0 ? 0 : 1;
    }
}

internal static class Check
{
    public static void True(bool value, string reason)
    {
        if (!value) throw new InvalidOperationException(reason);
    }

    public static void Near(double actual, double expected, double tolerance, string reason)
    {
        if (!double.IsFinite(actual) || Math.Abs(actual - expected) > tolerance)
            throw new InvalidOperationException($"{reason}: expected {expected:G9} ± {tolerance:G5}, got {actual:G9}");
    }

    public static void Between(double actual, double minimum, double maximum, string reason)
    {
        if (!double.IsFinite(actual) || actual < minimum || actual > maximum)
            throw new InvalidOperationException($"{reason}: expected [{minimum:G9}, {maximum:G9}], got {actual:G9}");
    }

    public static void Throws<T>(Action action, string reason) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException(reason);
    }
}
