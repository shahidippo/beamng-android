# Production scope and feature status

The full requirement text is preserved in [Brief.md](Brief.md). This checklist tracks the original scope; it does not reduce that scope to the engineering proving ground. No production feature is accepted merely because its source exists. Runtime and device tests are still required.

Status meanings: **foundation** = implementation/research exists for a limited subset; **planned** = not implemented as a production feature; **unverified** = required executable evidence is absent. See the test report for tests that actually executed.

## All brief sections

| Brief section | Current work | Remaining production work / acceptance |
| --- | --- | --- |
| 1. Objective and original IP | Original ImpactDrive/Aster engineering source and assets; scope recorded | Complete, polished Android simulator remains unfulfilled |
| 2. Architecture | Unity 6/URP project; plain-C# model separation; build/test entry points | Unity import/build validation, calibrated contact limits, Jobs/Burst where measured, large-world loading |
| 3. Vehicle physics | Mass/geometry data, spring/damper travel, Ackermann steering, combined-slip tyre model, limited surfaces | Measured calibration; all suspension types; genuine unsprung mass; camber/toe kinematics; carcass/pressure/thermal/wear/compound models; all terrain interactions |
| 4. Engine/drivetrain | One fictional torque curve; bounded clutch torque; forward/neutral/reverse gears; automatic/manual selection; configurable driven axles | Engine-family catalogue, thermal/fuel/turbo/supercharger/EV/hybrid systems, true manual clutch/stall, DCT/CVT, calibrated limited-slip/locked differentials and selectable 4WD |
| 5. Brakes/assistance | Wheel brake torque, bias, handbrake, ABS/traction-control integration foundation | Physical-device response; complete hydraulic/disc/drum/thermal/fade/damage models; ESC and hill start; calibrated presets |
| 6. Structural damage | Local energy-budget plastic-deformation model and selected mechanical effects | Validated material/member calibration, full breakable attachments, component and fluid failure catalogue, collider deformation, full structural diagnostics and impact programme |
| 7. Detailed cars | One original Aster engineering vehicle asset | Production exterior/interior/mechanisms/LODs, visual clipping QA, all 18 independently calibrated platforms |
| 8. Garage | Vehicle data and repair/reset foundation | 3D garage, panels, part compatibility, complete tuning/upgrades, saved configurations, paint/wheels and comparison UX |
| 9. Maps | Finite engineering proving ground with driving test obstacles | All eight finished launch environments, connected roads, navigation, content density, streaming, map validation |
| 10. Mobile input | Multitouch analog controls and multiple steering-mode source | Android simultaneous-touch validation, latency measurements, remapping/layout/accessibility polish, gamepads and complete ancillary vehicle controls |
| 11. Cameras | Vehicle camera foundation | Complete 11-camera catalogue, free/photo/replay tools, collision/occlusion and comfort QA |
| 12. Graphics/world detail | URP setup and original engineering materials | Production lighting/materials, time/weather, wetness, lights/signals, particles/tracks, LOD/probes/occlusion and Android GPU validation |
| 13. Audio | Original RPM/load-driven procedural engine audio foundation | Calibrated engine families, complete tyre/surface/mechanical/crash/environment soundscape and device mix QA |
| 14. AI/spawning | Player vehicle assembly only | Road network AI, rules/signals, traffic density/recovery, spawning catalogue, trailers/towing, seamless simulation detail changes |
| 15. Modes | Free driving in the engineering test area and reset/repair | Production free roam, crash/handling tools, off-road/time trials/drag/delivery/traffic/stunt objectives, replay and deterministic slow motion |
| 16. Android performance | ARM64/IL2CPP configuration intention and profiling plan | Install/build verification; measured device tiers; 30/60 FPS, memory, thermals, battery, streaming; adaptive rendering without physics changes |
| 17. Menus/HUD | Engineering HUD, pause/settings and simulation telemetry source | Complete front end, vehicle/map/mode selection, garage, replay/loading/credits/navigation/save management and accessibility |
| 18. Saves/offline | Versioned local settings with integrity/backup foundation | Complete garage/progress/condition persistence, migration matrix, suspension/resume/corruption/low-storage recovery on Android |
| 19. Physics validation | Pure-model invariant tests and analytic targets | Instrumented engine-level and real-world-reference acceleration, braking, handling, suspension and crash measurements |
| 20. Automated QA | Host model test runner and engine test sources | Unity execution, all driving/damage/map/device/soak cases; defect triage and regression evidence |
| 21. Acceptance | Explicit pass/fail gates in Acceptance.md | Close every failing or unrun release gate |
| 22. Production phases | Phase 1 documents and Phase 2/3 foundation work | Validated vertical slice, content expansion, complete gameplay, device optimization, QA and release |
| 23. Final deliverables | Editable source/configuration, original assets, docs and test tooling | Verified installable APK, finished content/UI, tested build, physical-device reports and benchmarks |
| 24. Non-negotiable rules | Limits and evidence stated; no finished-game claim | All original rules remain binding through final delivery |

## Vehicle collection

| Platform | Current status |
| --- | --- |
| Economy hatchback | Aster 1.6 engineering foundation; unverified in Unity/Android |
| Compact sedan | Planned |
| Mid-size family sedan | Planned |
| Large luxury sedan | Planned |
| Sports coupe | Planned |
| High-performance sports car | Planned |
| Supercar | Planned |
| Muscle car | Planned |
| Station wagon | Planned |
| Small SUV | Planned |
| Large SUV | Planned |
| Pickup truck | Planned |
| Off-road 4×4 | Planned |
| Delivery van | Planned |
| Heavy commercial truck | Planned |
| Classic car | Planned |
| Electric performance car | Planned |
| Rally/off-road racing vehicle | Planned |

Every additional platform requires its own mass/inertia, dimensions, suspension geometry, tyres, torque/powertrain, braking, steering, structural data, original assets and validation record. Changing a model name or body mesh is not a completed vehicle.

## Launch maps

| Map | Current status |
| --- | --- |
| Major modern city, approximately 6 × 6 km or larger | Planned |
| Mountain roads | Planned |
| Desert and off-road landscape | Planned |
| Realistic crash-test facility | Engineering proving-ground foundation only; production facility not accepted |
| Countryside | Planned |
| Snow and ice | Planned |
| Race and vehicle-testing complex | Planned |
| Coastal and highway region | Planned |

Each map must satisfy all 15 map-quality rules in the brief. A finite obstacle ground, empty terrain, large coordinate bounds or a generated list of roads is not evidence of a completed launch map.

## Phase exit gates

| Phase | Exit evidence | Current status |
| --- | --- | --- |
| 1. Research/specification | Reviewed sources, architecture, valid schema, physical assumptions, device/test strategy and scope | Preliminary docs available; production calibration research outstanding |
| 2. Vehicle foundation | One car accelerates, brakes, reverses and steers by forces; independent suspension/contact; mobile controls; objective tests | Source foundation; Unity/Android verification outstanding |
| 3. Crash/mechanical damage | Direction/energy-dependent impacts, stable deformation, functional failures, repeatable recovery | Reduced-order foundation; collision programme outstanding |
| 4. Vertical slice | Finished car and medium environment; complete basic UX/audio; installable APK; device validation | Not reached |
| 5. Collection | 18 distinct platforms with individual assets/data/tests | Not reached |
| 6. World construction | Eight complete maps with validated connectivity/collision/streaming | Not reached |
| 7. Garage/gameplay | Full modes, customization, AI, replay, saves and menus | Not reached |
| 8. Android optimization | Physical-device performance/thermal/memory results on declared tiers | Not reached |
| 9. Full QA | Required suites/soaks executed; no known critical/high issues; residual issues documented | Not reached |
| 10. Release candidate | Appropriate signing, install/start/offline/resume checks, source and reproducible package | Not reached |

## Final deliverables

| Requested deliverable | Status |
| --- | --- |
| Functioning Android APK | Not generated or tested in this environment |
| Editable Unity project | Source/configuration present; Unity import and build remain unverified |
| Original vehicle assets | One engineering vehicle; collection incomplete |
| Maps/environment assets | Engineering proving ground; launch maps incomplete |
| Physics implementation | Reduced-order foundation; full physics scope incomplete |
| Touch implementation | Source present; real touch validation outstanding |
| Complete menus | Incomplete; engineering HUD/settings only |
| Configuration files | Initial project/model/build configuration |
| Documentation | Architecture, research, physical assumptions, status and QA |
| Tested build configuration | Android build not executed |
| Dependency list | Project package manifest plus toolchain documentation |
| Installation instructions | Build/install workflow provided; device installation not executed |
| Test reports | Host model results when generated; engine/device suites explicitly unrun |
| Performance benchmarks | No Android benchmark results |
| Known limitations | Explicit in Architecture, Physics, Acceptance and AndroidQA |
