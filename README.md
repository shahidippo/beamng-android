# ImpactDrive Mobile

Unity 6 / URP / C# source for the first engineering milestone of the requested Android vehicle simulator. **This is incomplete development work, not the finished game. No APK has been generated, and Unity gameplay has not been run in this cloud machine.** The full requested scope is preserved in [Docs/Brief.md](Docs/Brief.md).

The current implementation contains one original Aster hatchback, a finite proving ground, force-based suspension and combined-slip tyres, a mechanical engine/clutch/gearbox model, reduced-order structural damage, mobile controls, cameras, procedural engine sound and recoverable settings storage. The C# numerical and storage code has executable tests. Unity integration, touch behavior, rendering, collision behavior and Android performance remain unverified.

## Validate the work available here

From `/workspace/beamng-android`:

```bash
bash Tools/setup-cloud.sh
```

This installs or reuses a SHA512-verified .NET 8.0.426 SDK under `/workspace/.tools`, checks project/asset integrity and runs the package-free C# test suite. It does not install Unity or build Android. On another machine with .NET 8.0.426:

```bash
python3 Tools/verify-project.py
dotnet run --project Tests/Physics/ImpactDrive.Physics.Tests.csproj --configuration Release -- --report Artifacts/Tests/physics-results.json
```

See [the validation report](Docs/Validation/Report.md) for executed results and [Tests/README.md](Tests/README.md) for test boundaries. Headless model checks do not certify the Unity car.

## Open and test the Unity project

Required tools:

| Tool | Version / requirement |
| --- | --- |
| Unity Editor | 6000.0.62f1, with a valid Unity entitlement/activation |
| Android module | Android Build Support plus its matching SDK, NDK and OpenJDK installed through Unity Hub |
| URP | 17.0.4, pinned in `Packages/manifest.json` |
| UGUI | 2.0.0 |
| Unity Test Framework | 1.4.5 |
| Blender | Assets generated and inspected with 4.3.2; only needed for asset editing/regeneration |
| Android test device | First requested OS: Android 12 / API 31; model and GPU not yet specified |

Use the Android SDK/NDK/JDK supplied for the pinned Editor. The cloud machine's Java 21 installation is not a substitute for Unity's matching toolchain. The exact bundled component revisions and dependency lockfile must be recorded after the first successful Unity import; they have not been fabricated here.

1. Install the pinned Editor and Android modules. Open this repository directory as a Unity project.
2. Allow Package Manager to resolve the manifest. Required Unity download hosts are `packages.unity.com`, `download.packages.unity.com` and `download.unity3d.com`. Network additions saved to the cloud configuration do not take effect merely because they were drafted.
3. Run **ImpactDrive → Configure project and URP**. This creates the URP renderer/pipeline and shader references, configures readable original meshes, and selects the Android build settings. Generated render assets live in the ignored `Assets/ImpactDrive/Generated/` directory.
4. Open `Assets/ImpactDrive/Scenes/ProvingGround.unity`, then press Play. Investigate import/compiler/runtime failures before proceeding; this step is unverified in the current environment.
5. Run the EditMode and PlayMode suites in Test Runner. Alternatively:

```bash
export UNITY_EDITOR=/absolute/path/to/6000.0.62f1/Editor/Unity
bash Tools/unity.sh test
```

The wrapper rejects missing results, failures and zero-test runs. Test results and Editor logs are written under `Artifacts/Unity/`.

## Build and validate Android

```bash
bash Tools/unity.sh android
```

The build script selects ARM64, IL2CPP, OpenGL ES 3, landscape display, minimum API 26 and target API 35. API 26 is a provisional configuration floor, not a verified supported-device promise. Android 12 is the first requested device OS. The development APK uses Unity's development/debug signing configuration and is intended for testing, not store release.

Expected output **after a successful build**: `Artifacts/Android/ImpactDrive-development.apk`. This path currently has no APK. The script throws on build failure. A licensed self-hosted runner workflow is supplied in `.github/workflows/unity-android.yml`; its runner must provide `UNITY_EDITOR` and the matching Android modules.

On a connected ARM64 device with USB debugging authorized:

```bash
adb devices
export ANDROID_SERIAL=your_device_serial
bash Tools/android-device-check.sh
```

Then complete [Android QA](Docs/AndroidQA.md). Installation and launching alone do not pass gameplay, multitouch, thermal, offline or suspension/resume gates. No on-device FPS, battery or memory measurements exist yet.

## Controls in the implemented HUD

- Touch: analogue steering stick, wheel, buttons or tilt; separate analogue throttle and brake; handbrake and gear controls.
- Keyboard: W/up throttle, S/down brake, A/D steering, Space handbrake, R drive/reverse, Q/E shifts, C camera, Backspace reset, Escape pause.
- Pause: change steering mode, sensitivity, dead zone, ABS/TC/steering assistance, sound volume, graphics quality and 30/60 rendering target.
- Reset returns to the spawn; repair restores modeled damage. Reverse selection is guarded while moving against the requested direction. Physics frequency stays independent of rendering settings.

See [presentation details](Assets/ImpactDrive/Runtime/Presentation/README.md) for the exact implemented controls and remaining input work. Control functionality is implemented in source, pending Unity/device execution.

## Architecture and assets

- `Assets/ImpactDrive/Physics`: engine-independent SI numerical models used by both tests and Unity.
- `Assets/ImpactDrive/Runtime`: Unity rigidbody integration, world construction and presentation.
- `Assets/ImpactDrive/Art/Resources/ImpactDrive`: original editable vehicle definition and generated meshes/material descriptions.
- `Tools/Blender`: original procedural asset source and editable Blender file. Unity imports OBJ files directly and does not require Blender to launch.
- `Assets/ImpactDrive/Editor`: reproducible render configuration and Android build entry points.
- `Docs`: [architecture](Docs/Architecture.md), [physics assumptions](Docs/Physics.md), [research](Docs/Research/References.md), [feature checklist](Docs/FeatureChecklist.md) and [all 20 acceptance gates](Docs/Acceptance.md).

There are no BeamNG assets, maps, sounds, code or branding in this project. The generated meshes are engineering assets with visible limitations; they are not represented as production car art.

## Remaining release work

The required next gate is a successful Unity import, physics integration tests and Android 12 hardware trial. The brief requires the foundation to be stable before scaling content. The 18 production vehicle platforms, eight launch maps, complete garage/game modes, traffic, streaming, advanced mechanical systems, production assets/audio, real-device profiling and release QA are still outstanding. Reduced-order mesh deformation keeps rigid collision shapes; it is not a full soft-body simulation.

The original full-game objective remains open. [The feature checklist](Docs/FeatureChecklist.md) records the scope without claiming planned features as implemented.
