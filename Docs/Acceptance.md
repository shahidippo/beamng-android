# Release acceptance gates

**Overall result: FAIL — incomplete production scope and missing Unity/Android execution evidence.** The engineering source is not an accepted vertical slice or finished game.

These gates map one-to-one to section 21 of the [brief](Brief.md). A failed gate labelled **unrun** means evidence is missing, not that the described defect was reproduced. Source architecture and pure-model tests cannot close engine/device behaviour gates.

| # | Acceptance criterion | Gate | Evidence required / current limit |
| --- | --- | --- | --- |
| 1 | Steering creates tyre forces rather than directly rotating the chassis | FAIL — unrun integration | Inspect runtime force path and record forward/reverse steering tests; mathematical Ackermann tests alone are insufficient |
| 2 | Vehicles have distinct handling | FAIL — scope incomplete | Only one engineering platform; 18-platform comparative acceleration/braking/handling data absent |
| 3 | Suspension physically supports and moves the chassis | FAIL — unrun integration | Static sag, diagonal bump, braking pitch and cornering roll must be measured with real contacts |
| 4 | The car does not float above road surfaces | FAIL — unrun integration | Contact/ride-height inspection at spawn, rest, slopes and repair required |
| 5 | Wheels do not pass through terrain | FAIL — unrun integration | High-speed rough-road, edge, kerb, landing and rollover cases; cast-based contact has known approximation limits |
| 6 | Bounce response is physically explained and stable | FAIL — unrun integration | Measured decay/travel/force traces against declared damping and 60/120 Hz comparison |
| 7 | Collisions produce physically driven deformation | FAIL — unrun integration | Impact-energy core is limited; collision integration and directional deformation tests remain |
| 8 | Mechanical damage affects performance | FAIL — unrun integration | Measure damaged versus intact wheel alignment, torque and drivability; complete failure catalogue absent |
| 9 | Maps are complete and connected | FAIL — scope incomplete | Eight launch environments, road-graph checks, full drive-through and border/collision QA absent |
| 10 | Core menu buttons perform their advertised functions | FAIL — unrun integration / scope incomplete | Engineering controls require input automation/manual checks; complete menus absent |
| 11 | Multiple simultaneous touches work | FAIL — unrun Android | Real device touch capture, finger crossing, cancellation, app-focus and cutout tests required |
| 12 | Graphics settings preserve driving physics | FAIL — unrun Android | Same input/configuration at every preset; compare telemetry and fixed-step count, including under load |
| 13 | High-speed physics remains stable | FAIL — unrun integration | Straight-line, steering, rough-road and collision stability envelope across time steps required |
| 14 | Vehicle meshes avoid severe normal-travel clipping | FAIL — unrun visual QA | Full steering/articulation, camera, damage and reset inspections required |
| 15 | Supported-device gameplay has no reproducible crashes | FAIL — unrun Android | No supported tier can be declared before install/play/soak matrix runs |
| 16 | Android build files are complete and usable | FAIL — unrun build | Initial Unity configuration/tooling exists; package resolution, IL2CPP and Gradle build have not run |
| 17 | APK installs and launches on a supported device | FAIL — no APK | Build, signature/ABI inspection, physical-device install and launch evidence required |
| 18 | Completion claims match implemented features | PASS — reporting review only | Scope checklist explicitly marks missing content; no full-game, complete-feature or successful APK claim is made |
| 19 | Performance claims are based on Android measurements | PASS — no performance claim | 30/60 FPS are unverified goals; there are no Editor-only figures represented as Android benchmarks |
| 20 | Android build has been tested on real hardware | FAIL — no device run | No physical Android device is connected or validated here |

The two reporting passes do not imply a playable build or a successful Android test. All functional gates remain open until supported by the relevant recorded evidence.

## Evidence format

Store a report for each run containing commit/source revision, Editor/package versions, Android model/OS/SoC/RAM/GPU, graphics and physics configuration, initial conditions, inputs, duration, measurements, acceptance bounds, result and artifact paths. Preserve runner exit codes and identify skipped/unrun tests. Screenshots or videos complement telemetry but do not replace numerical validation.

Record reproducible defects with severity, steps, expected/observed behaviour, logs, affected configuration and regression test. Fix critical/high defects before the release gate. No claim of zero bugs is justified by the current evidence.
