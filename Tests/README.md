# Validation scopes

The standalone runner compiles the production `Assets/ImpactDrive/Physics` source and the production `AtomicSaveFile` helper. It has no third-party test packages or NuGet downloads. A failed check returns a nonzero process exit code; the runner also rejects a zero-check run.

From the project root with the pinned .NET SDK installed:

```sh
dotnet run --project Tests/Physics/ImpactDrive.Physics.Tests.csproj --configuration Release -- --report Artifacts/Tests/physics-results.json
```

The suite contains 55 checks: configuration validation, Ackermann geometry, tire contact signs and combined force bounds, relaxation, suspension sag and damping, drivetrain torque and power losses, plastic-work limits, save corruption/recovery, and a coupled longitudinal diagnostic. The JSON report includes the actual run timestamp, each outcome and diagnostic measurements.

The longitudinal bench has one translational degree of freedom, four fixed wheel loads and the production tire/powertrain models. Its 60 Hz and 120 Hz comparison can reveal wheel integration instability. Its speeds and stopping distances are **synthetic diagnostics**, not measured Unity vehicle performance or real-world calibration. It omits load transfer, suspension motion, collision contact, ABS/TC, steering and Android execution.

Unity-specific checks are separate:

```sh
UNITY_EDITOR=/path/to/Unity Tools/unity.sh test
```

Three EditMode tests exercise core behavior in Unity's managed runtime. Four PlayMode tests exercise actual rigidbody settling, wheel-driven acceleration/braking, reverse, and the absence of artificial airborne steering force. They require the pinned licensed Editor and Test Framework. Results must come from that invocation's XML files; the .NET runner cannot pass these checks on Unity's behalf.

During initial cloud development the 55 standalone checks passed; Unity tests were **not run** because the Editor was absent. Android installation, multitouch, suspension/resume, hardware performance, thermal behavior, full handling calibration, and crash behavior also remain unverified. See the main validation report for the current release gates.
