using ImpactDrive.HeadlessTests;
using ImpactDrive.Physics;

var suite = new TestSuite();
PhysicsChecks.Register(suite);
PersistenceChecks.Register(suite);
LongitudinalBench.Register(suite);
string? report = null;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--report" && i + 1 < args.Length) report = args[++i];
    else throw new ArgumentException("Usage: dotnet run --project Tests/Physics -- [--report <path>]");
}
return suite.Finish(report);
