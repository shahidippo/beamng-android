# MASTER GAME DEVELOPMENT PROMPT — ULTRA-REALISTIC ANDROID VEHICLE PHYSICS SIMULATOR

## 1. YOUR ROLE AND MAIN OBJECTIVE

You are an expert AAA game development team consisting of vehicle dynamics engineers, mechanical engineers, automotive designers, 3D artists, game physics programmers, Unity engineers, Android developers, world designers, optimisation specialists, sound designers, and professional QA testers.

Your mission is to develop a complete, polished, highly realistic, open-world vehicle physics simulator for Android, inspired by the depth of BeamNG.drive.

**Working game title: ImpactDrive Mobile**

This must be a real, fully functional, three-dimensional Android game, not a concept, prototype presented as a finished game, prerecorded animation, simple car controller, or browser demonstration.

The game must concentrate on:

- Extremely realistic vehicle handling and mechanical behaviour.
- Advanced independent suspension and tyre physics.
- Authentic chassis movement, body roll and weight transfer.
- Realistic, physically driven collision damage.
- High-quality vehicles with detailed exteriors, interiors and mechanical components.
- Large, finished, varied open-world maps.
- Precise, comfortable, responsive mobile touch controls.
- High-quality graphics with substantial optimisation for Android.
- Realistic engine sounds, transmissions and vehicle systems.
- Extensive customisation, free roaming, experiments and driving challenges.
- A polished experience with tested and working features.

Treat mechanical realism as the highest priority, followed by responsive controls, complete maps, vehicle detail, graphics and performance.

Do not sacrifice proper driving physics merely to make the graphics look impressive.

Research reputable automotive engineering references and game-engine documentation before implementing major systems. Do not guess physical values that can be calculated, obtained from reliable references or validated experimentally.

Build original intellectual property. BeamNG.drive is a technical inspiration, not a source of copyrighted maps, models, code, sounds, logos or branding.

---

# 2. DEVELOPMENT TECHNOLOGY AND ARCHITECTURE

Use Unity 6, the Universal Render Pipeline (URP), C#, appropriate Unity physics facilities and custom automotive simulation code.

Use Blender or equivalent professional modelling tools for original vehicle and world assets.

Required architecture:

- Modular vehicle simulation systems.
- Physically based rigid-body vehicle movement.
- Custom advanced wheel, tyre, suspension and steering calculations.
- Deformable body and chassis simulation.
- Data-driven vehicle configurations.
- Object-oriented and component-based systems where appropriate.
- Performance-oriented native containers and Unity Job System/Burst where supported and beneficial.
- An asset loading and streaming architecture for large environments.
- Reusable vehicle and map components.
- Automated testing and build scripts.
- Clear separation of graphics, physics and player input.
- Explicit documentation of dependencies and supported software versions.

Do not rely entirely on Unity's standard WheelCollider behaviour to produce realistic driving.

A custom vehicle dynamics layer must calculate wheel forces, suspension behaviour, tyre contact, steering geometry, traction and vehicle-specific performance.

Standard colliders and Unity's rigid-body solver may be used where appropriate, but their limitations must be assessed, tested and improved.

Use metres, kilograms, seconds, newtons, newton-metres and consistent SI units throughout the simulation.

Use fixed-timestep physics and interpolate visual rendering between physics updates.

Begin by evaluating 60 Hz and 120 Hz physics configurations. Use finer internal substeps where required for stability, contact and damage calculations. Select the final configuration from measurements on real Android hardware.

Do not simply increase the simulation frequency without considering CPU cost.

---

# 3. REALISTIC VEHICLE PHYSICS — ABSOLUTE HIGHEST PRIORITY

## 3.1 Vehicle Mass and Dynamics

Every vehicle must possess independently configurable and realistic physical characteristics:

- Total mass.
- Front-to-rear weight distribution.
- Centre of mass in three dimensions.
- Wheelbase.
- Front and rear track width.
- Chassis stiffness.
- Moment of inertia.
- Suspension mounting geometry.
- Aerodynamic drag coefficient.
- Frontal area.
- Rolling resistance.
- Downforce and lift.
- Tyre dimensions and properties.
- Engine and drivetrain characteristics.
- Unsprung and sprung mass.
- Payload and fuel mass.
- Drivetrain layout.

Vehicles must respond to forces, impulses, torque, gravity, road gradients and tyre contact rather than scripted movement.

A heavy SUV must not behave like a lightweight sports car. A van must not corner like a race car, and an overloaded truck must visibly respond to its load.

Ensure mass distribution affects acceleration, braking, cornering, rollover tendency and suspension response.

## 3.2 Advanced Suspension Physics

Implement physically meaningful suspension systems.

Support:

- MacPherson strut.
- Double wishbone.
- Multi-link.
- Solid axle and leaf-spring arrangements.
- Trailing-arm suspension.
- Off-road long-travel suspension.
- Adjustable coilovers.
- Air suspension where applicable.
- Active or adaptive dampers where applicable.

For each wheel independently calculate:

- Suspension compression.
- Suspension extension.
- Available suspension travel.
- Spring force.
- Compression damping.
- Rebound damping.
- Wheel load.
- Bump-stop contact.
- Rebound limiting.
- Unsprung-mass dynamics.
- Anti-roll bar forces.
- Road impact response.
- Dynamic camber and toe where geometry supports them.
- Suspension alignment and damage effects.

Use spring-damper relationships grounded in physics. Start with a suitable spring model such as F = kx and a velocity-dependent damping model, then add realistic nonlinear behaviour when justified.

Do not apply arbitrary body movement merely to create the appearance of suspension.

The car must actually respond to forces.

**Required demonstrations:**

1. Drive across a pothole at low speed. The corresponding wheel must move through its suspension travel while the body responds naturally.
2. Repeat at high speed. The impact and resulting forces must differ appropriately.
3. Drive over a speed bump diagonally. Each wheel must respond at the correct time.
4. Brake aggressively. The front suspension must compress as load transfers forward.
5. Accelerate aggressively. The car must exhibit vehicle-appropriate pitch and squat.
6. Corner hard. Body roll must result from realistic suspension and tyre forces.
7. Drive over uneven off-road terrain. Every wheel must react independently.
8. Damage a suspension arm. Wheel alignment and steering must change accordingly.

Avoid excessively stiff suspension, rubber-like rocking, perpetual bouncing, incorrect wheel positioning or wheels clipping through the bodywork.

## 3.3 Realistic Steering — CRITICAL

Steering must not feel like a cheap mobile driving simulator.

Do not rotate the entire car toward an input direction. Steering must create wheel angles that generate actual tyre forces and yaw moments.

Implement:

- Steering rack behaviour.
- Ackermann steering geometry.
- Vehicle-specific steering ratios.
- Configurable steering lock.
- Steering input rate.
- Physically consistent tyre self-aligning effects where feasible.
- Road-speed-related steering response.
- Proper reverse steering.
- Dynamic understeer and oversteer.
- Realistic turn radius.
- Steering response under braking and acceleration.
- Steering effects from wheel damage and misalignment.

The front wheels must visibly turn, with their movement matching the actual simulation.

At low speeds, cars must manoeuvre smoothly.

At highway speeds, turning must remain controllable and not suddenly snap the vehicle sideways.

Any optional steering assistance must be clearly configurable and must not replace the underlying physical steering model.

No fake lateral sliding, constant turning radius, unrealistic pivoting or automatic vehicle rotation.

## 3.4 Advanced Tyre Physics

Tyre simulation is essential.

Use a documented tyre model, such as a properly calibrated brush model or Pacejka-inspired model.

Consider:

- Slip angle.
- Longitudinal slip ratio.
- Wheel angular velocity.
- Lateral forces.
- Longitudinal forces.
- Vertical tyre load.
- Load sensitivity.
- Combined braking, acceleration and cornering forces.
- Tyre relaxation behaviour.
- Tyre pressure.
- Tyre temperature.
- Tyre wear.
- Tyre construction and compound.
- Tyre deformation and contact behaviour.
- Wet-road grip.
- Surface roughness.
- Tyre punctures and deflation.
- Camber effects where supported.

Tyres must progressively approach and exceed their grip limits.

Road tyres, race tyres, winter tyres, off-road tyres and heavily worn tyres must behave differently.

A car sliding on ice must behave significantly differently from the same car on warm, dry asphalt.

Avoid simply changing a single friction multiplier and calling it a complete tyre simulation.

Use combined-slip constraints so a tyre cannot generate unlimited braking and cornering grip simultaneously.

## 3.5 Surface and Terrain Interaction

Support physically distinct surfaces:

- Dry asphalt.
- Wet asphalt.
- Concrete.
- Gravel.
- Compact dirt.
- Loose dirt.
- Sand.
- Mud.
- Grass.
- Snow.
- Ice.
- Rocks.
- Uneven damaged roads.

Each must affect tyre forces, rolling resistance, wheel sinkage where applicable, suspension movement, braking performance and sound.

Implement convincing off-road traction, wheelspin, bogging and vehicle grounding.

Avoid cars floating over terrain or wheels visually penetrating roads.

---

# 4. COMPLETE ENGINE AND DRIVETRAIN SIMULATION

Vehicles must not accelerate through a basic scripted speed multiplier.

Implement mechanical drivetrain simulation.

## Engine Types

Support:

- Inline-three, four, five and six engines.
- V6, V8, V10 and V12.
- Turbocharged engines.
- Supercharged engines.
- Naturally aspirated engines.
- Diesel engines.
- Electric motors.
- Hybrid powertrains.

Each engine must have believable characteristics, including:

- Torque versus RPM curves.
- Power output.
- Idle RPM.
- Redline.
- Rev limiter.
- Throttle response.
- Engine inertia.
- Engine braking.
- Turbo boost and spool behaviour.
- Thermal behaviour.
- Load-dependent fuel consumption.
- Mechanical damage.
- Engine starting and stopping.

## Transmissions

Include:

- Manual transmissions.
- Automatic transmissions.
- Dual-clutch transmissions.
- CVTs where appropriate.
- Electric single-speed drivetrains.

Model:

- Gear ratios.
- Final-drive ratios.
- Clutch engagement.
- Torque transfer.
- Transmission losses.
- Gear selection.
- Shift time.
- Engine RPM changes.
- Automatic shift logic.
- Manual clutch mode.
- Engine stalling where applicable.
- Transmission damage.

## Drivetrain Layouts

Implement FWD, RWD, AWD and selectable 4WD.

Include open, locked and limited-slip differentials, with vehicle-appropriate torque distribution.

A rear-wheel-drive sports car must react differently from a front-wheel-drive hatchback under full throttle.

Four-wheel-drive off-road vehicles must have realistic benefits without becoming physically impossible climbing machines.

---

# 5. BRAKES, VEHICLE ELECTRONICS AND ASSISTANCE

Implement:

- Per-wheel braking torque.
- Brake bias.
- Disc and drum brakes.
- Hydraulic braking behaviour.
- Anti-lock braking system.
- Electronic stability control.
- Traction control.
- Parking brake.
- Mechanical handbrake.
- Brake temperature.
- Brake fade.
- Damaged braking components.
- Adjustable assistance settings.
- Hill-start assistance on suitable vehicles.

ABS must actually modulate braking when wheels approach locking.

Traction control must intervene using physically plausible torque or brake actions.

ESC must act based on vehicle state rather than applying unexplained artificial rotation.

All assists must be adjustable or switchable where appropriate.

Include assisted and simulation-oriented driving presets, without modifying the fundamental physical correctness of the car.

---

# 6. BEAMNG-INSPIRED STRUCTURAL DAMAGE AND CRASH PHYSICS

This is another primary feature.

Create an efficient physically driven deformation system suitable for Android.

Research the principles of node-and-beam structures, material strength, plastic deformation and mechanical failure.

Use a hybrid architecture if necessary:

- A calibrated rigid-body chassis foundation.
- A localised deformable node-and-constraint structure.
- Structural component deformation.
- Visual meshes attached to deformation points.
- Breakable mechanical and body attachments.
- An impact-energy-based damage system.
- Actual changes to mechanical function.

Avoid implementing an expensive full soft-body simulation on every distant traffic vehicle when it would overwhelm mobile hardware.

However, the player's vehicle and nearby high-detail vehicles must have meaningful physically driven deformation, rather than simply switching to predetermined damaged models.

## Collision Requirements

Every crash must consider:

- Impact velocity and direction.
- Relative velocity.
- Vehicle mass.
- Impact contact points.
- Collision impulse and energy.
- Structural resistance.
- Material behaviour.
- Body-panel deformation.
- Chassis damage.
- Component mounting strength.
- Mechanical failure.

Impacts must produce plausible differences.

For example:

A minor parking accident might scratch or deform a bumper.

A moderate frontal impact might damage the bumper, bonnet, radiator and suspension mounts.

A severe head-on collision might deform major structural members, disable the engine and alter wheel alignment.

Side impacts, rear impacts and rollovers must damage relevant areas.

## Functional Mechanical Damage

Support:

- Crumpled bonnets.
- Deformed roofs.
- Bent doors.
- Broken glass.
- Detached bumpers.
- Damaged headlights.
- Damaged radiators.
- Wheel damage.
- Bent axles.
- Damaged suspension arms.
- Broken driveshafts.
- Steering failure.
- Engine damage.
- Transmission failure.
- Fuel or fluid leakage where appropriate.
- Tyre punctures.
- Wheel detachment.
- Exhaust damage.
- Vehicle immobilisation.

Vehicle components must be physically connected, detached or damaged through defined mechanical relationships.

A damaged wheel must not continue behaving like a perfectly aligned wheel.

A destroyed engine must not provide full power.

Do not make deformation look like rubber melting, a texture-only dent, a randomly exploding mesh or a car compressing into an unrecognisable ball under a tiny impact.

Maintain plausible material thickness, constraint stability and collision response.

Implement structural debugging tools that display nodes, constraints, stress, permanent deformation and breakage.

---

# 7. EXTREMELY DETAILED CAR MODELLING

Visual realism must match the mechanical simulation.

Create original, accurately proportioned vehicles with complete, professionally modelled exteriors and interiors.

Prioritise genuine geometry and correctly animated parts over excessive decorative details.

## Exterior Requirements

Every vehicle must have:

- Accurate body proportions.
- Detailed front and rear bodywork.
- Proper panel gaps.
- Separate doors.
- Bonnet and boot mechanisms.
- Detailed headlights and taillights.
- Turn signals.
- Realistic mirrors.
- Door handles.
- Correctly fitted tyres and wheels.
- Brake discs and calipers where applicable.
- Windscreen wipers.
- Windows and appropriate glass materials.
- Undercarriage components.
- Exhaust systems.
- Functional suspension visualisation.

## Interior Requirements

Include:

- Dashboard.
- Steering wheel.
- Seats.
- Centre console.
- Gauge cluster.
- Functional speedometer.
- Functional tachometer.
- Gear selector.
- Pedals.
- Interior mirrors.
- Door panels.
- Indicator controls.
- Lighting.
- Detailed driving camera.

The steering wheel must animate with steering input.

The tachometer must display actual engine RPM, not an arbitrary animation.

Brake pedals, suspension and drivetrain visuals must respond to their corresponding systems where visible.

## Mechanical Detail

Provide properly constructed:

- Engine bay.
- Engine model.
- Transmission housing.
- Driveshaft.
- Differentials.
- Suspension arms.
- Springs and shock absorbers.
- Steering linkages.
- Brake systems.
- Chassis structure.

Moving parts must use correct pivot positions.

They must not float, clip into surrounding parts or move in ways inconsistent with their mechanical function.

## Initial Vehicle Collection

Target at least 18 distinct original vehicle platforms, including:

1. Economy hatchback.
2. Compact sedan.
3. Mid-size family sedan.
4. Large luxury sedan.
5. Sports coupe.
6. High-performance sports car.
7. Supercar.
8. Muscle car.
9. Station wagon.
10. Small SUV.
11. Large SUV.
12. Pickup truck.
13. Off-road 4x4.
14. Delivery van.
15. Heavy commercial truck.
16. Classic car.
17. Electric performance car.
18. Rally or off-road racing vehicle.

Each vehicle must have distinct mass properties, engine performance, steering characteristics, suspension geometry and damage behaviour.

Do not simply place 18 different car models over the same identical driving controller.

Use original designs unless the necessary real-car licensing rights are available.

---

# 8. CAR CUSTOMISATION AND GARAGE

Create a fully functional garage.

Allow players to:

- Browse and select vehicles.
- Rotate and inspect vehicles in 3D.
- Open appropriate body panels.
- Inspect engines and suspension.
- Change paint colour.
- Change wheel designs.
- Adjust tyre specifications.
- Change ride height.
- Adjust spring stiffness.
- Adjust damper settings.
- Modify alignment.
- Adjust gearing.
- Configure differentials.
- Install compatible engine upgrades.
- Change exhaust configurations.
- Modify brakes.
- Save multiple vehicle configurations.
- Reset cars to factory condition.
- Repair damage.
- Compare vehicle specifications.

Advanced tuning:

- Camber.
- Toe.
- Caster where supported.
- Spring rates.
- Rebound and compression damping.
- Anti-roll bars.
- Ride height.
- Tyre pressure.
- Brake bias.
- Differential settings.
- Transmission ratios.
- Steering lock.

Changing an adjustment must change the actual simulation, not just the displayed specification.

Support a detailed but approachable interface, with presets for players who do not understand mechanical tuning.

---

# 9. LARGE, COMPLETE, VARIED OPEN-WORLD MAPS

Map quality must be treated as essential.

Do not create tiny, repetitive, empty, unfinished or visually misleading environments.

Create a complete collection of large, original environments inspired by real geographic and driving conditions.

Use world streaming and level-of-detail technology to support environments larger than can fit entirely in memory.

The following are launch content targets, not excuses to ship unfinished environments.

## Map 1 — Major Modern City

A substantial metropolitan environment with:

- Dense downtown.
- Skyscrapers.
- Suburban neighbourhoods.
- Residential streets.
- Business districts.
- Shopping areas.
- Parking garages.
- Multi-lane roads.
- Intersections and traffic lights.
- Roundabouts.
- Bridges.
- Tunnels.
- Highway connections.
- Industrial districts.
- Service stations.
- Functional traffic AI.
- Different road surfaces.
- Day and night driving.

Target an explorable area approximately 6 km × 6 km or larger, with believable route continuity and useful road density.

Do not fill the world with repeated buildings or long empty roads to inflate the map size.

## Map 2 — Mountain Roads

Include:

- Long mountain highways.
- Hairpin bends.
- Steep gradients.
- Cliffs.
- Forests.
- Tunnels.
- Bridges.
- Scenic valleys.
- Dangerous corners.
- Viewpoints.
- Gravel roads.
- High-altitude weather conditions.

The road network must be substantial enough for extended driving, not merely one short loop.

## Map 3 — Desert and Off-Road Landscape

Include:

- Large open desert.
- Sand dunes.
- Rocky tracks.
- Dry riverbeds.
- Off-road trails.
- Remote roads.
- Small settlements.
- Testing areas.
- Challenging slopes.
- Surface-specific tyre interaction.

## Map 4 — Realistic Crash Test Facility

Include:

- Crash barriers.
- Concrete walls.
- Ramps.
- Vehicle launch areas.
- Rollover testing.
- Suspension testing.
- Rough-road testing.
- Impact testing.
- Acceleration measurement.
- Brake testing.
- Skidpad.
- Collision measurement.

Create functional tools that measure impact speeds, damage, deformation and resulting vehicle performance.

## Map 5 — Countryside

Include:

- Country roads.
- Farms.
- Villages.
- Dirt tracks.
- Bridges.
- Rolling hills.
- Forest sections.
- Small highways.
- Agricultural environments.
- Different road widths and surface conditions.

## Map 6 — Snow and Ice

Include:

- Snow-covered roads.
- Frozen areas.
- Mountain routes.
- Forest tracks.
- Slippery intersections.
- Winter villages.
- Snowbanks.
- Areas with varying traction.

Provide physically meaningful differences between compact snow, ice and clearer pavement.

## Map 7 — Race and Vehicle Testing Complex

Include:

- Full race circuit.
- Pit lane.
- Drag strip.
- Skidpad.
- Handling circuit.
- Off-road course.
- High-speed oval or banked track.
- Acceleration tests.
- Braking zones.
- Lap timing.
- Sector timing.

## Map 8 — Coastal and Highway Region

Include:

- Extended highway system.
- Coastal towns.
- Ocean-side roads.
- Bridges.
- Mountain-to-coast routes.
- Urban interchanges.
- Rural roads.
- Long driving routes.
- Scenic locations.
- Rest stops.

## Map Quality Rules

Every launch map must:

1. Be explorable from end to end.
2. Have properly connected roads.
3. Avoid invisible barriers in accessible spaces.
4. Avoid unfinished terrain and abrupt world edges.
5. Have working collisions.
6. Have consistent road elevation.
7. Have correctly placed surface materials.
8. Include sufficient environmental detail.
9. Support continuous driving without major loading pauses.
10. Have properly functioning spawn and reset positions.
11. Support high-speed driving without obvious terrain streaming failures.
12. Include a map and navigation interface.
13. Pass an automated road-connectivity inspection.
14. Pass visual and collision quality assurance.
15. Maintain acceptable mobile performance.

Map generation tools may assist construction, but procedurally generated roads, terrain and structures must be inspected and corrected.

A large empty heightmap does not count as a complete open-world map.

---

# 10. MOBILE TOUCH CONTROLS — EXTREMELY IMPORTANT

Design the complete interaction system specifically for landscape-mode Android phones and tablets.

Never simply shrink a PC interface onto a phone screen.

## Steering Methods

Provide:

1. Left and right steering buttons.
2. On-screen steering wheel.
3. Analogue virtual steering stick.
4. Gyroscope tilt steering.

The player must be able to switch between control modes.

## Throttle and Braking

Provide:

- Analogue accelerator.
- Analogue brake.
- Reverse control.
- Handbrake.
- Optional clutch pedal.
- Gear selection.
- Gear-up and gear-down buttons.
- Automatic/manual transmission switching.

Analogue inputs must support intermediate values rather than operating only at full throttle or full braking.

## Additional Touch Controls

Include:

- Engine start/stop.
- Headlights.
- High beams.
- Indicators.
- Hazard lights.
- Horn.
- Camera selection.
- Camera look.
- Reset vehicle.
- Repair vehicle.
- Spawn vehicle.
- Slow motion.
- Pause.
- Screenshot mode.
- Photo mode.
- Map.
- Vehicle configuration.
- Settings.

## Control Quality

Make steering smooth but immediate.

Implement adjustable:

- Sensitivity.
- Dead zones.
- Steering response curves.
- Input filtering.
- Steering return speed.
- Gyroscope calibration.
- Button sizes.
- Button locations.
- Transparency.
- Haptic feedback.

Use frame-independent input collection and sample controls correctly for physics.

Support multiple simultaneous touches, including steering, acceleration, braking and camera input.

Prevent touch conflicts and unintentional steering changes when the player drags across the screen.

Do not add excessive input latency to make controls appear smooth.

Include optional controller support for compatible Bluetooth and USB gamepads.

---

# 11. CAMERA SYSTEM

Provide:

- Third-person chase camera.
- Close chase camera.
- Far chase camera.
- Cockpit camera.
- Dashboard camera.
- Bonnet camera.
- Front bumper camera.
- Rear-facing camera.
- Cinematic orbit camera.
- Free camera.
- Crash replay camera.

Camera behaviour must be smooth without excessive delay.

The chase camera should adapt to vehicle speed and direction without forcing artificial yaw onto the vehicle.

Support adjustable field of view, camera distance, camera height and camera sensitivity.

Include optional realistic camera vibration for road bumps, engine vibration and collisions.

Never make excessive shaking the default.

---

# 12. REALISTIC GRAPHICS AND WORLD DETAIL

Use physically based materials and mobile-appropriate rendering.

Include:

- Realistic automotive paint.
- Metallic reflections.
- Roughness variation.
- Appropriate glass transparency.
- Rubber materials.
- Interior materials.
- High-quality lighting.
- Dynamic shadows where feasible.
- Day/night cycle.
- Dynamic weather.
- Rain and wet surfaces.
- Fog.
- Clouds.
- Functional headlights.
- Brake lights.
- Indicators.
- Tyre smoke.
- Road dust.
- Mud splashes.
- Sparks from relevant collisions.
- Damage particles.
- Skid marks.
- Tyre tracks.
- Water spray.

Graphics must remain consistent with gameplay.

For example, rain must influence road traction, not merely appear as a screen overlay.

Wet surfaces must affect tyre behaviour.

Use efficient reflection approximations, lighting probes, baked illumination, suitable LOD meshes, occlusion culling and modern mobile GPU techniques.

Do not depend on PC-only ray tracing.

---

# 13. REALISTIC ENGINE AUDIO AND ENVIRONMENTAL SOUNDS

Develop a responsive vehicle audio system.

Include:

- Engine starting.
- Idle.
- Engine acceleration.
- RPM-dependent sound.
- Engine load.
- Throttle lift.
- Exhaust character.
- Turbo spool.
- Supercharger whine.
- Gear shifts.
- Transmission whine where appropriate.
- Engine braking.
- Tyre squeal.
- Tyre scrub.
- Gravel noise.
- Suspension impacts.
- Wind noise.
- Water sounds.
- Road-surface noise.
- Collision sounds.
- Metal deformation.
- Glass breaking.
- Component detachment.
- Horns.
- Environment ambience.

Vehicle sounds must correspond to actual engine RPM, load and state.

Use carefully blended samples or suitable procedural techniques with original or properly licensed sound assets.

A small hatchback must not sound like a V12 supercar.

---

# 14. TRAFFIC, AI AND VEHICLE SPAWNING

Implement traffic with realistic road-following behaviour.

AI vehicles must:

- Follow road networks.
- Observe traffic lights.
- Give way where appropriate.
- Navigate intersections.
- Change lanes appropriately.
- Brake for obstacles.
- Maintain reasonable speeds.
- Avoid unnecessary collisions.
- React to player vehicles.
- Handle blocked roads.
- Recover from traffic problems.

Add adjustable traffic density.

Allow spawning:

- Single vehicles.
- Multiple vehicles.
- Parked vehicles.
- Moving traffic.
- Crash test vehicles.
- AI pursuit vehicles.
- Trailers and towed loads where supported.

Nearby traffic must have believable vehicle dynamics and collision response.

Distant AI may use reduced simulation detail to maintain performance, but must upgrade correctly as it approaches the player.

Do not let vehicles snap visibly when simulation detail changes.

---

# 15. GAME MODES

## Free Roam

Unlimited driving and exploration without unnecessary restrictions.

## Crash Testing

Test impacts using walls, barriers, ramps, moving vehicles and other objects.

## Vehicle Handling Testing

Test grip, braking, acceleration and cornering.

## Off-Road Challenges

Traverse difficult terrain using suitable vehicles.

## Time Trials

Complete timed routes and circuits.

## Drag Racing

Test realistic vehicle acceleration.

## Delivery and Transport

Drive vans and trucks with vehicle loads that affect mass and handling.

## Traffic Driving

Drive in realistic traffic with optional road rules and penalties.

## Stunt Playground

A dedicated physics-experiment environment using large ramps, drops and obstacles.

Keep ordinary road maps grounded in realistic construction rather than filling them with unrealistic stunt objects.

## Replay and Slow Motion

Record recent gameplay states and provide replay tools where computationally feasible.

Support slow-motion collision viewing without corrupting physics state.

---

# 16. PERFORMANCE OPTIMISATION FOR ANDROID

The game must be designed for mobile hardware from the beginning.

Support Android ARM64 devices.

Use an appropriately supported Unity/Android toolchain with IL2CPP.

Set graphics presets:

- Low.
- Medium.
- High.
- Ultra, on capable devices.

Target:

- Stable 30 FPS on defined supported lower-performance devices.
- Stable 60 FPS on capable mid-range/high-end devices where measured and achievable.
- Higher refresh rates only when practical.
- Consistent vehicle handling across graphics settings.
- Controlled battery and thermal behaviour.
- Fast menu navigation.
- Predictable memory usage.
- Smooth map streaming.

Optimise with:

- Mesh LOD.
- Texture compression.
- Texture streaming.
- Occlusion culling.
- Frustum culling.
- Object pooling.
- Static batching and suitable instancing.
- Reduced unnecessary physics updates.
- Dynamic resolution where supported.
- Appropriate physics sleeping.
- Efficient collision layers.
- Asynchronous asset loading.
- Scene and terrain chunk streaming.
- Profiling-guided CPU and GPU improvements.
- Reduced expensive allocations.
- Physics simulation priority for the active vehicle.

**Critical:** Never make the player's vehicle physically inaccurate merely to preserve an expensive visual effect.

Maintain the essential driving simulation and reduce rendering complexity first.

For demanding crash scenes, use measured adaptive detail rather than unpredictable or visually obvious physics changes.

Do not claim universal support across all Android phones.

Establish a documented minimum device specification and clearly communicate device limitations.

---

# 17. POLISHED MENUS AND USER INTERFACE

Create complete interfaces for:

- Main menu.
- Play.
- Map selection.
- Vehicle selection.
- Garage.
- Vehicle configuration.
- Game mode selection.
- Graphics settings.
- Sound settings.
- Input settings.
- Camera settings.
- Accessibility.
- Replay controls.
- Pause menu.
- Loading screens.
- Save management.
- Credits.

Support responsive layouts for different aspect ratios, screen sizes and display cutouts.

Make the interface visually polished and readable.

Include:

- Speedometer.
- Tachometer.
- Current gear.
- Fuel or battery level.
- Engine temperature where applicable.
- Damage indicators.
- ABS and traction control indicators.
- Navigation.
- FPS display option.
- Physics debug overlays.
- Detailed vehicle telemetry.

The HUD must display actual simulation values.

---

# 18. SAVING, OFFLINE PLAY AND DATA MANAGEMENT

The game must run offline for its core gameplay.

Implement:

- Automatic saving.
- Manual saving where suitable.
- Saved car configurations.
- Garage persistence.
- Settings persistence.
- Vehicle condition saving.
- Progress saving.
- Reliable restoration after app suspension.
- Protection against corrupted save files.
- Save version migration.
- Backup or recovery mechanisms where feasible.

No unnecessary online account should be required.

Do not require invasive Android permissions.

---

# 19. MANDATORY PHYSICS VALIDATION

Do not call the handling realistic solely because it feels good to the developer.

Establish objective tests and compare performance with suitable real-world vehicle engineering references.

## Acceleration

Measure:

- 0–50 km/h.
- 0–100 km/h.
- 80–120 km/h.
- Quarter-mile acceleration.
- Top speed.
- Acceleration on different gradients.

Compare simulated vehicles with reasonable reference targets for their fictional design and mechanical specifications.

## Braking

Measure:

- 100–0 km/h stopping distance.
- Brake response on dry asphalt.
- Wet-road stopping distance.
- Braking on ice.
- ABS on/off behaviour.
- Brake fade after repeated stops.

Do not impose a universal stopping distance on vehicles with different tyres, mass and braking systems.

## Steering and Handling

Measure:

- Turning circle.
- Steady-state cornering.
- Steering response.
- Slalom behaviour.
- Understeer and oversteer characteristics.
- Fishhook and emergency manoeuvre response.
- Weight transfer.
- High-speed stability.

## Suspension

Measure:

- Static suspension sag.
- Wheel travel.
- Bounce response.
- Damping response.
- Body roll.
- Pitch response.
- Bump-stop behaviour.
- Wheel contact on rough terrain.

## Collisions

Measure:

- Impact velocity.
- Collision impulses.
- Deformation.
- Damage distribution.
- Structural constraint stability.
- Remaining drivability.
- Component failure.

Record telemetry and evaluate simulations against reference behaviours.

All parameters must have meaningful units and valid ranges.

---

# 20. STRICT AUTOMATED TESTING AND BUG PREVENTION

Establish automated tests and repeatable manual QA procedures.

Test the following:

### Driving Tests

- Normal acceleration.
- Heavy acceleration.
- Normal braking.
- Emergency braking.
- Reverse driving.
- Low-speed turning.
- High-speed steering.
- Handbrake use.
- Cornering on wet roads.
- Driving on gravel.
- Snow and ice.
- Hill climbs.
- Suspension articulation.
- Rough-road handling.
- Rollover.
- Different vehicle masses and drivetrains.

### Damage Tests

- Low-speed impact.
- High-speed frontal collision.
- Side collision.
- Rear collision.
- Multiple sequential collisions.
- Roof impact.
- Rollover damage.
- Wheel damage.
- Suspension damage.
- Engine failure.
- Broken drivetrain.
- Detached components.
- Car-to-car collisions.
- Resetting a severely damaged vehicle.

### Map Tests

- Drive every road.
- Inspect map borders.
- Check road connectivity.
- Detect floating objects.
- Detect missing terrain.
- Check collision placement.
- Test bridge and tunnel clearance.
- Test spawn locations.
- Test map loading and unloading.
- Test traffic pathfinding.
- Test long high-speed driving.
- Test off-road terrain boundaries.

### Android Tests

- Installation.
- First launch.
- Different screen resolutions.
- Landscape orientation.
- Multitouch.
- Gyroscope.
- Touch input latency.
- App suspension.
- App resuming.
- Low-memory conditions.
- Thermal throttling.
- Frame-rate consistency.
- Battery usage.
- Device-specific graphics issues.
- APK packaging.
- Offline operation.

### Long-Duration Testing

Run extended sessions involving free roaming, traffic and repeated collisions.

Look for:

- Memory leaks.
- Increased loading times.
- Physics instability.
- Gradual FPS reduction.
- Crashes.
- Save corruption.
- Missing assets.
- Stuck controls.
- UI failures.

Every discovered error must be logged, prioritised, fixed and retested.

Do not declare that zero bugs exist without evidence.

The release target is zero known critical or high-severity issues, with reproducible testing results and clearly documented remaining limitations.

---

# 21. STRICT QUALITY ACCEPTANCE RULES

Reject the game as incomplete if any of the following remain unresolved:

1. Steering controls rotate the chassis directly instead of generating tyre forces.
2. Every vehicle handles almost identically.
3. Suspension movement is cosmetic rather than simulated.
4. The car floats above road surfaces.
5. Wheels pass through terrain.
6. Vehicles bounce excessively without physical explanation.
7. Collisions use only predetermined damage animations.
8. Mechanical damage has no effect on performance.
9. Maps are incomplete or disconnected.
10. Core menu buttons do nothing.
11. Multiple simultaneous touch inputs malfunction.
12. Changing graphics settings changes driving physics unpredictably.
13. Significant physics instability occurs at high speed.
14. Vehicle meshes clip severely during normal suspension movement.
15. The game suffers reproducible crashes in ordinary supported-device gameplay.
16. Required Android build files are missing.
17. The APK cannot install or launch on a supported test device.
18. Features are claimed to be complete but have not been implemented.
19. Performance claims are based solely on the Unity Editor.
20. An Android build has never been tested on actual Android hardware.

Use validation gates and publish a pass/fail result for every acceptance criterion.

---

# 22. DEVELOPMENT PHASES — COMPLETE THE ENTIRE PROJECT

This is a production-scale project. Use a phased development strategy to avoid constructing a large, unstable game.

## Phase 1 — Research and Technical Specification

Research vehicle dynamics, tyre modelling, suspension geometry, crash deformation, Android performance and Unity limitations.

Produce:

- Technical architecture.
- Physics design.
- Vehicle configuration schema.
- Rendering strategy.
- World streaming plan.
- Device compatibility targets.
- Test strategy.
- Complete feature checklist.

## Phase 2 — Physically Correct Vehicle Foundation

Create one fully functioning original vehicle with:

- Real drivetrain.
- Realistic steering.
- Four-wheel independent tyre simulation.
- Complete suspension.
- Brake system.
- Working touch controls.
- Camera system.
- Basic sound.
- Technical telemetry.

Test it on a flat track, skidpad, bumps and gradients.

Do not expand to many cars before the core physics is stable.

## Phase 3 — Crash and Mechanical Damage

Add physically driven deformation, breakable components and mechanical damage to the existing vehicle.

Test frontal, side, rear and rollover impacts.

## Phase 4 — Complete Vertical Slice

Finish one highly detailed car and one complete medium-sized environment with menus, graphics, sound, touch controls and functional damage.

Create an installable Android APK of this working vertical slice.

Validate the fundamental game before scaling content.

## Phase 5 — Vehicle Collection

Extend the stable architecture to the full vehicle collection.

Each car must have its own physical data and visual assets.

## Phase 6 — World Construction

Build the eight launch maps with functional road networks, environmental systems and properly placed objects.

Use streaming where required.

## Phase 7 — Garage and Gameplay Systems

Implement vehicle customisation, traffic, replay tools, game modes, saves and polished interfaces.

## Phase 8 — Android Optimisation

Profile real devices and optimise rendering, physics, collision meshes, memory and streaming.

Establish performance results for the supported device tiers.

## Phase 9 — Full Quality Assurance

Run all automated and manual tests.

Correct bugs, regressions, physics problems, broken assets, loading problems and performance issues.

## Phase 10 — Release Candidate

Create a complete, signed or appropriately configured installable Android APK.

Provide the Unity source project and all required supporting files.

Verify installation, startup, menus, vehicles, maps and core gameplay on real supported Android devices.

---

# 23. ANDROID APK AND FINAL DELIVERABLES

The final output must include:

1. A fully functioning Android APK.
2. The editable Unity project.
3. Original vehicle assets.
4. Maps and environment assets.
5. Physics implementation.
6. Touch control implementation.
7. Complete menus.
8. Game configuration files.
9. Documentation.
10. Tested build configuration.
11. Dependency list.
12. Installation instructions.
13. Test reports.
14. Performance benchmarks.
15. Known limitations and unresolved issues, if any.

Use an appropriate Android application identifier, ARM64 compatibility, IL2CPP, correct Android SDK/NDK setup and compatible graphics APIs.

Provide an APK for direct installation. An Android App Bundle can also be produced for store distribution if needed.

Do not pretend an APK was generated if it was not.

If the current environment cannot compile Android applications, provide the complete buildable project and an exact list of missing build requirements.

A source-code listing without the necessary project configuration, assets or instructions does not count as the finished game.

---

# 24. FINAL NON-NEGOTIABLE DEVELOPMENT RULES

- Never substitute arcade car movement for simulated vehicle dynamics.
- Never disguise cosmetic suspension animation as real suspension physics.
- Never fake structural destruction using only simple visual effects.
- Never ship unfinished, inaccessible or empty maps as complete maps.
- Never give every car the same engine, steering or mass settings.
- Never prioritise visual quality over input responsiveness and physics stability.
- Never reuse unauthorised assets from BeamNG.drive or other commercial games.
- Never claim a feature works without implementing and testing it.
- Never invent successful test results.
- Never skip Android compatibility testing.
- Never claim all bugs are eliminated without thorough evidence.
- Never hide major technical limitations.
- Never leave a demonstrated core feature broken because it is difficult to implement.
- Never consider a simple driving demonstration equivalent to the full requested simulator.

When a feature is too expensive for a particular phone, use transparent, configurable, physically defensible optimisations.

Use actual profiling and vehicle-dynamics tests to justify important engineering decisions.

A smaller, correctly simulated feature is preferable to a visually impressive but fake one during intermediate development. However, keep the complete finished-game scope as the final objective.

## FINAL INSTRUCTION

Your responsibility is to create a genuinely high-quality Android automotive physics simulator that captures the engineering depth, experimental freedom and satisfying mechanical behaviour associated with advanced PC vehicle simulators while remaining optimised for phones.

Begin with technical research and a functioning physics foundation.

Progress through the full development pipeline, continually testing every major system.

Deliver a professionally constructed, genuinely playable, feature-complete Android game and installable APK, not simply a design document, mock-up or unfinished prototype.

**Prioritise real vehicle physics, exceptional mobile controls, believable destruction, fully detailed vehicles, large completed maps and reliable Android performance above everything else.**