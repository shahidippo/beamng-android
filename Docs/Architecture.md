# ImpactDrive Mobile engineering architecture

This repository begins the physics-foundation stage of the [full production brief](Brief.md). It is not a finished simulator, validated vertical slice, or release candidate. The first integration target is one original Aster hatchback in an engineering proving ground. Content expansion remains gated on successful vehicle and Android validation.

## Boundaries

| Layer | Responsibility | Dependency direction |
| --- | --- | --- |
| `Assets/ImpactDrive/Physics` | SI-unit vehicle data, tyre/suspension/steering/drivetrain and damage calculations | Plain C#; no Unity dependency |
| `Assets/ImpactDrive/Runtime/Vehicle` | Ground queries, rigidbody force application, wheel integration, damage routing and telemetry | Unity + Physics |
| `Assets/ImpactDrive/Runtime/World` | Original test environment and vehicle assembly | Unity + Vehicle |
| `Assets/ImpactDrive/Runtime/Presentation` | Sample input, multitouch controls, HUD, cameras, procedural audio and settings | Reads vehicle state; submits driver inputs |
| `Assets/ImpactDrive/Editor` | Project preparation and Android build entry points | Editor-only |
| `Tests/Physics` | Package-free tests of the same C# model files used by Unity | .NET SDK; no engine emulation |
| `Assets/ImpactDrive/Tests` | Engine integration tests | Unity Test Framework |
| `Tools/Blender` | Reproducible original mesh construction | Blender |

The physics core must not read frame time, cameras, UI, scene transforms, or device input. Presentation must not change chassis orientation or velocity to implement steering. Reset/recovery is an explicit gameplay action, separate from normal driving.

## Simulation contract

The vehicle uses Unity's coordinate convention: forward is local `+Z`, up is `+Y`, and right is `+X`. Wheel order is front-left, front-right, rear-left, rear-right. Hub pivots sit at wheel centres; wheel spin is around local X. Distances, masses, times, forces and torques use metres, kilograms, seconds, newtons and newton-metres. RPM and km/h are presentation/configuration units converted at the model boundary.

Input is collected each rendered frame and sampled by fixed-step simulation. The rigidbody uses interpolation. Suspension and contact geometry are evaluated against actual colliders. Tyre forces are applied at contact positions, producing translation and yaw/pitch/roll through moments about the configured centre of mass. Wheel rotation integrates driveline/brake/reaction torque rather than assigning vehicle speed from throttle.

60 Hz and 120 Hz are experimental configurations. The runtime defaults to 16 wheel-angular substeps per outer fixed step (inspector range 4–32). These do not make the outer rigidbody/contact solver run at the same rate: body velocity and contact geometry remain fixed during those wheel substeps. Production fixed-step frequency and substep count must be selected from measured stability, convergence and CPU cost on Android. Graphics presets must not silently change physics frequency or mechanical parameters.

## Contact and suspension limitations

The runtime uses four spherecasts, with a support-ray fallback for initial overlap, filters its own colliders, and computes velocity relative to a contacted rigidbody. It applies equal/opposite reaction forces to a dynamic contacted body. This approximation cannot establish full finite-width tyre collision behaviour. A cast-based wheel can miss sharp edges, fail to envelop a kerb, or allow penetration under sufficiently fast motion. Chassis continuous collision detection helps the body but does not prove tyre contact correctness. Rough-road, step, landing and rollover tests are mandatory before declaring wheel contact reliable.

The foundation uses reduced-order spring/damper travel. It does not represent the complete constraint geometry of MacPherson, double-wishbone, multi-link, solid axle, leaf-spring, trailing-arm, air or adaptive suspension. Unsprung mass, kinematic camber gain, toe change and steering-rack self-aligning torque require dedicated models and validation. A visible wheel following calculated travel does not prove those systems exist.

Current numerical safeguards cap the contact-derived compression speed at ±12 m/s and each wheel's support at three times total vehicle weight. These are explicit stability bounds, not material/vehicle calibration. Anti-roll load exchange is disabled unless both axle wheels have contact. The rigidbody moment of inertia is a uniform-box estimate, not a measured body distribution. Extreme inputs/impacts must be tested to determine where these approximations invalidate expected behaviour.

## Damage boundary

PhysX resolves the rigidbody collision. The damage layer consumes contact direction, location and an energy budget to produce permanent local structural/visual changes and mechanical effects. It must never spend more deformation work than the available allocated collision energy or report post-impact kinetic energy as dissipated energy without an explicit assumption.

This architecture is a reduced-order hybrid. It is not a full dynamic node-and-beam chassis solver or finite-element crash model. Rigid chassis colliders do not automatically follow mesh dents. Material yield values, damage distribution, detachment thresholds and mechanical failure thresholds remain experimental until validated against a documented impact programme. Breakable doors, glass, fluid systems and comprehensive component failures are not implied by local mesh deformation.

## Rendering and assets

Unity 6 with URP is the requested rendering path. Use original geometry and materials, physically sized wheels and explicit pivots. The current car and ground are engineering assets; production interiors, panel mechanisms, LODs, texture sets, animation and visual QA remain work. No BeamNG assets or branding are included.

Start with one main light, limited real-time shadows and modest material counts. Profile before adding reflection probes, weather, particles or dense scenery. Geometry reduction, rendering resolution and effects quality are the first performance controls; vehicle dynamics must remain consistent between graphics presets.

## Large-world design — planned, not implemented

The proving ground is finite and loaded as one environment. It does not satisfy any launch open-world map acceptance gate.

For production maps, partition terrain, road visuals, props and static colliders into additive scene cells. Maintain a stable road graph independently of streamed visuals. Use asynchronous loading with a look-ahead region derived from velocity and stopping distance. Keep collision coverage ahead of the player before making a cell reachable. Unload cells only when neither active vehicles nor replay data reference them. Asset bundles/addressable content must be versioned with the map graph and physics-surface data.

A future origin-rebasing system must move rigidbodies, scene objects, AI paths, audio, particles and recorded poses atomically at a fixed-step boundary. Streaming tests must measure memory peaks, load stalls, collider continuity and high-speed border crossings. No such streaming or traffic system is currently certified.

## Native jobs and simulation detail — planned

The initial four-wheel player model favours inspectable scalar code. A Burst/Job System port requires measured CPU evidence and differential tests against the reference implementation. Do not introduce parallel mutation of a shared rigidbody or change the player's mechanical model to reduce visual cost. Future distant traffic can use lower detail only with validated, continuous state transfer when entering the high-detail region.

## Publication gate

Host-side pure-model tests are necessary but insufficient. Unity import/compilation, play-mode checks, APK packaging, physical Android installation, touch input, thermal/performance profiling and content QA remain separate gates. See [Acceptance.md](Acceptance.md), [AndroidQA.md](AndroidQA.md) and the current test report. None is replaced by a source-code review.
