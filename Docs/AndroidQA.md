# Android build and device validation

The user's first validation target is **Android 12 (API 31)**. The physical phone/tablet model is not yet specified. Android 12 is the runtime OS target for the first test; it does not change the project's Android target-SDK setting. No Android build or physical-device run has completed in this workspace.

## Missing build and validation prerequisites

1. Unity Editor **6000.0.62f1**, with valid activation for the user/organization and command-line build access.
2. That Editor's **Android Build Support**, including its supported **SDK, NDK and OpenJDK** modules. Use Unity Hub's matching modules; record the installed module versions. A standalone Java command in this container does not satisfy this toolchain.
3. Unity package resolution for the pinned manifest and successful URP/scene configuration.
4. An ARM64 Android 12 physical test device, USB debugging or an authorized device-farm connection, and Android platform tools (`adb`). Emulator results may help diagnosis but cannot close the real-hardware gate.
5. For a distributable release, the owner's signing configuration and secure keystore handling. The initial build entry point produces a development APK, not a signed production release. Do not place signing secrets in source or chat.

`UNITY_EDITOR` should point to the licensed executable. After installing the matching modules, from the repository root run:

```bash
Tools/unity.sh configure
Tools/unity.sh test
Tools/unity.sh android
```

The build entry point targets `Artifacts/Android/ImpactDrive-development.apk` unless `IMPACTDRIVE_APK_PATH` overrides it. A successful process exit must be accompanied by a fresh APK and successful Unity build report. Do not reuse an old artifact as evidence for a failed build. Test XML/logs are kept under `Artifacts/Unity`; the wrapper rejects zero-test results.

## Candidate compatibility envelope

These are proposed test prerequisites, **not a supported-device claim**:

| Item | Candidate / first validation target |
| --- | --- |
| ABI / backend | ARM64 / IL2CPP |
| First runtime OS | Android 12 / API 31, as requested |
| Candidate minimum OS | Android 8.0 / API 26; unverified |
| Build target SDK | API 35; separate from device OS |
| Graphics | OpenGL ES 3; Vulkan requires its own driver/device validation before enabling |
| Candidate low test tier | At least 4 GB RAM and an ARM64 device with usable GLES3; specific model still required |
| Higher test tier | At least one modern 6–8 GB device with a different GPU vendor from the low tier |
| Orientation | Landscape, including reverse landscape and display cutouts |
| Connectivity | Core gameplay offline; no game account required |

RAM, OS and API support alone cannot establish an acceptable frame rate. The minimum commercial specification must be derived from actual scene, physics, memory and thermal measurements, and may be higher than this initial candidate.

## Physical-device run record

Before installation, record device manufacturer/model, SoC, GPU/driver, RAM, Android version/security update, available storage, battery level, ambient conditions and thermal state. Record APK hash, build identifier, Editor/packages, graphics preset and fixed time step. Verify the APK contains ARM64 native libraries and has the intended application identifier/minimum SDK/orientation.

Install with `adb install -r <fresh-apk-path>` on the authorized device. Launch from the Android launcher, capture logcat and verify the proving ground renders, controls react and the rigidbody remains stable at rest. Repeat first launch with network disabled. Installation success alone does not close startup or gameplay gates.

## Repeatable manual and instrumented cases

| Area | Required actions | Acceptance evidence |
| --- | --- | --- |
| First launch/offline | Fresh install, launch offline, enter driving, restart | No account/network dependency, missing assets, black screen or fatal exception |
| Orientation/layout | Both landscape rotations; multiple aspect ratios and cutouts | Controls remain reachable/readable; safe-area bounds respected |
| Multitouch | Hold steering + throttle; add brake/handbrake; cross fingers between regions; release in varied order | Each control retains its own pointer; no stuck control or unexpected ownership transfer |
| Tilt/input | Calibrate stationary, rotate gently, return to centre, background/resume | Consistent steering, no stale sensor input; actual device sensor capability recorded |
| Control cancellation | Incoming overlay, notification shade, app switch, pause, focus loss | Throttle/brake/steering state clears safely and resumes intentionally |
| Driving | Start/stop, accelerate, service brake, handbrake, reverse, slopes, dry/wet/gravel/ice, diagonal bumps | Telemetry matches inputs, forces and contact; no direct chassis steering or unstable speed changes |
| Damage/recovery | Front/side/rear/roof impacts at declared speeds, repeated impacts, repair/reset | Bounded deformation, actual impairment, consistent reset and no increasing allocations/corruption |
| Cameras/audio | Change every available camera; drive forward/reverse; vary load/RPM and volume | No severe clipping or discomfort; audio follows state and stops/pauses correctly |
| Settings | Change graphics/sound/input/assist settings; close/relaunch | Persisted values restored; physical configuration/frequency unchanged by graphics settings |
| Persistence failures | Corrupt current settings, verify backup; truncate write; low-storage condition | Explicit recovery/defaults; no crash or silent destruction of valid backup |
| Suspension/resume | Background for 30 s and 5 min, lock/unlock, OS kills app, relaunch | No runaway timestep or stale touch; documented state restoration |
| Low memory | Exercise system memory pressure and repeated scene/object lifecycle | No crash, missing objects, leaked resources or unbounded memory growth |
| Soak | At least 30 min mixed driving and repeated crashes; 60 min on representative tiers | Record crash-free duration, memory trend, input integrity, thermal state and frame-time drift |

This table covers the current foundation. Traffic, full maps, garage, replay and release-content cases must be added when those systems are implemented; their absence is a scope failure, not a skipped pass.

## Physics and performance matrix

Run identical mechanical scenarios at 60 Hz and 120 Hz, with all graphics presets, on each candidate tier. Record outer fixed-step time and wheel substeps separately. Inputs/initial state must be comparable. Do not claim determinism across PhysX/device platforms without measurement.

For each run capture median, 95th and 99th percentile frame time; CPU main/render/physics time; GPU time where available; fixed-step catch-up count; managed allocations; resident and graphics memory; temperature/thermal-throttle status; battery drain; and longest loading stall. Include warm-up and steady-state intervals. Editor timings and cloud CPU test durations are not Android performance data.

| Measurement | Goal | Current result |
| --- | --- | --- |
| Low-tier sustained rendering | 30 FPS where measured feasible | UNRUN — no device |
| Capable-tier sustained rendering | 60 FPS where measured feasible | UNRUN — no device |
| Physics at 60/120 Hz | Stable, convergent behaviour within declared tolerances | UNRUN in Unity/Android |
| Touch latency | Measured end-to-end, no stuck simultaneous inputs | UNRUN — no device |
| Thermal/battery | Report 30/60 min trends and throttling | UNRUN — no device |
| Memory/streaming | Bound growth and avoid visible collider/loading failures | UNRUN; production streaming not implemented |
| APK install/start | Fresh build installs and runs offline | UNRUN — no APK |

30/60 FPS are objectives, not results. If a graphics preset misses its measured budget, reduce rendering cost before altering the player's mechanical simulation. Any adaptive simulation detail must have explicit transition/state-error tests.

## Release closure

Store signed build provenance, install/launch logs, physical-device reports, recorded physics scenarios, regression results and all known issues. Close the [20 acceptance gates](Acceptance.md) before describing the product as complete. At present, source generation and host-model tests cannot satisfy those release requirements.
