# Validation report — 23 October 2026

**Release status: incomplete. No APK, Unity runtime pass or Android hardware performance result exists.** This report covers work performed in the current cloud instance, not a published snapshot or a newly restored task.

## Executed checks

| Check | Result | Evidence and limits |
| --- | --- | --- |
| Pure C# physics, coupled numerical diagnostics and save storage | **55 passed, 0 failed, 0 skipped**, exit 0 | [Machine-readable results](physics-results.json); actual production core and save helper compiled with .NET 8.0.426 |
| Project and asset integrity | **193/193 checks passed**, exit 0 | `python3 Tools/verify-project.py`; verifies required files, pinned direct dependencies, asset GUIDs, assembly references and mesh indices; does not compile Unity |
| Original vehicle mesh validation | **10 categories passed**, exit 0 | [Asset results](asset-results.json); finite coordinates, nondegenerate triangles, materials, dimensions, tyre radius, moving-part separation and counts |
| C# 9 language syntax | **25 source files parsed, 0 syntax errors** | Roslyn parser from installed SDK; no Unity assembly/type resolution and therefore no Unity compile claim |
| Setup repeatability | **Passed**, exit 0 | `bash Tools/setup-cloud.sh` rerun reused pinned SDK and reran integrity checks and all 55 numerical/storage checks |
| Shell/Python tooling syntax | **Passed** | `bash -n` and Python compilation for authored setup/build/integrity helpers |
| Original asset generation | **Executed** in Blender 4.3.2 | Editable `.blend`, OBJ/MTL exports and rendered geometry inspection; production-detail and Unity import QA pending |

The .NET SDK archive was verified against SHA512 from Microsoft's official release metadata before extraction. No TLS, package signature or checksum verification was disabled. Its pinned setup script records the verified digest.

The test suite checks valid physical data, Ackermann geometry, tyre force direction and grip limits, relaxation, spring/damper response, drivetrain energy losses and direction, plastic-work limits under repeated impacts, corruption/backup recovery and coupled longitudinal behavior. These checks do not substitute for PhysX contact tests, calibrated handling measurements or mobile input tests.

## Synthetic measurements

These are outputs from a **one-dimensional, fixed-wheel-load numerical bench** using the C# core. It does not include the Unity rigidbody, dynamic load transfer, steering, collisions, ABS/TC or device rendering. Coefficients remain uncalibrated fictional design assumptions. The values are diagnostics, not achieved vehicle specifications or device benchmarks.

| Diagnostic | Measured result |
| --- | --- |
| Speed after 10 s full throttle, 60 Hz outer update / 16 wheel substeps | 79.37 km/h |
| Same setup at 120 Hz | 82.53 km/h |
| Dry locked-wheel braking from 100 km/h down to 0.5 m/s | 43.67 m |
| Ice locked-wheel braking under the same conditions | 467.49 m |

The roughly 4% difference between outer timesteps requires further convergence and Unity testing. The 120 Hz project default is provisional; no real Android measurement has selected a final frequency. The model lacks a static bristle-friction equilibrium, so braked slope creep remains an open investigation.

## Unrun checks and concrete blockers

- **Unity import and semantic compilation:** Editor 6000.0.62f1 is absent. Syntax parsing cannot establish engine API compatibility, shader compilation or correct asset import.
- **Unity tests:** 3 EditMode and 4 PlayMode test methods are authored but unrun. `bash Tools/unity.sh test` exited 2 with a missing-Editor prerequisite message. No empty or skipped run is counted as a pass.
- **Android APK:** Android Build Support, matching SDK/NDK/OpenJDK and `adb` are absent. The installed standalone Java 21 does not satisfy Unity's module requirements. No APK path contains a generated APK. Unity license activation status has not been assessed without an Editor.
- **Package access:** requests to Unity package/download hosts returned proxy HTTP 403. After saving draft domain additions, a repeat request still returned 403; draft persistence did not change current runtime policy.
- **Hardware QA:** Android 12/API 31 is the user's requested first OS target. Phone model, SoC/GPU/RAM and device execution evidence remain unavailable. No installation, multitouch, graphics, thermal, memory, battery, frame-rate, lifecycle or soak test has run.
- **Production scope:** 18 finished vehicle platforms, eight launch maps and the remaining systems in [the checklist](../FeatureChecklist.md) have not been built. The proving ground and engineering model are explicitly intermediate work.

All 20 release criteria are mapped in [Acceptance.md](../Acceptance.md). Missing execution evidence is distinguished from reproduced defects. No functional release gate is closed by the headless suite alone.

## Reusable environment configuration

The cloud configuration tool confirmed a saved draft containing:

- `install_script`: runs the tested `Tools/setup-cloud.sh` from `/workspace/beamng-android`.
- `start_skill`: .NET activation, validation commands, existing-checkout guidance, Unity startup/build commands and current blockers.
- Network additions: `packages.unity.com`, `download.packages.unity.com`, `download.unity3d.com`, `docs.unity3d.com`. Existing preset domains were preserved.

The draft is **saved, not published or applied**. Review and save it in environment settings, then publish to retain the prepared state. Publication is not an Android-readiness claim. After network settings are applied, retry package access and install/provide an appropriately activated pinned Editor with its Android modules. Then run the documented Unity and physical-device gates before scaling vehicle/map content.

No secret values were read, embedded in files or requested in chat. At the time of the initial validation run, no Git push, commit, app-store publication or game release had been performed.
