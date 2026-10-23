# Physics design and validation targets

These are reduced-order models for an original fictional Aster 1.6 hatchback. The values below are engineering design inputs, not measured real-car specifications. A passing mathematical test establishes an invariant or consistency check; it does not establish realistic handling or crash fidelity.

## Vehicle configuration

`VehicleSpec` is the serializable source of mechanical configuration. `CreateAster()` constructs independent defaults; `Validate()` rejects nonfinite/out-of-range values, impossible steering geometry, malformed torque curves and invalid gearing. Field names carry SI units where applicable. Configuration validation must run before accepting garage/save data.

| Parameter | Initial value | Evidence/category |
| --- | --- | --- |
| Total rigidbody mass | 1,280 kg | Fictional design assumption; no separate unsprung rigidbodies |
| Static front axle fraction | 0.60 | Design assumption; centre of mass must reproduce it |
| Wheelbase | 2.60 m | Original vehicle geometry target |
| Front/rear track | 1.52 / 1.50 m | Original vehicle geometry target |
| Centre-of-mass height | 0.52 m | Design target requiring integration verification |
| Wheel radius / rotational inertia | 0.305 m / 1.1 kg·m² | Design assumption; no measured tyre mass distribution |
| Rest suspension length / travel | 0.35 / 0.24 m | Reduced-order travel limits |
| Wheel spring rate | 34,000 N/m | Design assumption with unit motion ratio |
| Compression/rebound damping | 2,500 / 3,600 N·s/m | Linear, asymmetric reduced-order coefficients |
| Bump-stop additional rate | 180,000 N/m | Starts at 85% of travel; design assumption |
| Axle anti-roll rate | 10,000 N/m | Equivalent wheel-difference rate; not torsion-bar material geometry |
| Rack centre-angle lock / rate | 0.54 rad / 1.8 rad/s | Design inputs; not a measured steering rack |
| Drag coefficient / frontal area | 0.31 / 2.15 m² | Design assumptions |
| Rolling-resistance coefficient | 0.013 | Design assumption; surface model also affects resistance |
| Total service brake torque / front bias | 6,000 N·m / 0.65 | Sum over all four wheels, not per-wheel torque |
| Total rear handbrake torque | 3,000 N·m | Sum over the two rear wheels |
| Engine inertia / idle / limiter | 0.24 kg·m² / 850 / 6,500 rpm | Fictional naturally aspirated engine target |
| Forward gear ratios | 3.54, 2.05, 1.38, 1.03, 0.82, 0.68 | Ratio magnitudes; final drive applied separately |
| Reverse / final drive / efficiency | −3.2 / 4.1 / 0.92 | Simplified transmission assumptions |
| Clutch capacity / shift duration | 250 N·m / 0.22 s | Design assumptions |

The torque curve spans 80 N·m at 800 rpm to a 158 N·m point at 3,500 rpm, then 115 N·m at 6,500 rpm. The largest power among the supplied curve knots is approximately 82.36 kW at 5,500 rpm, using `P = Tω`. Peak power between knots must be calculated from the actual interpolation; a knot maximum is not necessarily the curve maximum.

## Suspension

Let compression `x = restLength − currentLength`, positive for compression. The reduced unilateral ground support is:

`F = max(0, kx + c(dx/dt) + kb max(0, x − 0.85 travel))`

Choose compression or rebound damping from the sign of compression velocity. No support is produced when there is no contact or no compression. The anti-roll pair uses `ΔF = karb(xleft − xright)`, adding to the left and subtracting from the right before enforcing valid contact reactions. The pair must have zero net force when both wheels support it; contact loss must not pull on the ground.

With `g = 9.80665 m/s²`, level-road equilibrium and no bump stop:

| Analytic check | Front corner | Rear corner |
| --- | --- | --- |
| Equivalent supported mass `m × axleFraction / 2` | 384 kg | 256 kg |
| Static wheel load | 3,765.754 N | 2,510.502 N |
| Spring sag `mg/k` | 0.11076 m | 0.07384 m |
| Uncoupled corner frequency `sqrt(k/m)/(2π)` | 1.498 Hz | 1.834 Hz |
| Compression damping ratio `c/(2sqrt(km))` | 0.346 | 0.424 |
| Rebound damping ratio | 0.498 | 0.610 |

These frequency/damping calculations approximate each corner independently. Pitch/roll coupling, anti-roll bars, tyre stiffness and genuine unsprung motion change modal behaviour. They are comparison targets for static sag and decay, not proof of a complete suspension model.

Expected quasistatic longitudinal load transfer is `ΔF = m ax h / wheelbase` on level ground, neglecting aero and suspension geometry. Compare the measured axle-load change and sign against this expression during acceleration/braking. Lateral transfer also depends on track, roll-centre geometry and roll-stiffness distribution; a single total transfer equation is insufficient to calibrate front/rear balance.

## Steering and tyres

Ackermann geometry uses `R = wheelbase / tan(|δcentre|)`, then `δinner = atan(wheelbase/(R−track/2))` and `δouter = atan(wheelbase/(R+track/2))`. Positive input turns right, so the right wheel is the inner wheel. Reverse retains the same steering geometry; velocity and contact forces determine reversed yaw response.

At the initial 0.54 rad rack centre lock, the geometric rear-axle midpoint radius is about 4.337 m. Front inner/outer wheel angles are about 36.009° / 27.024°, and the outer front-wheel centre describes an approximately 11.444 m diameter circle. These exclude body overhang, tyre width, slip and compliance; they are not a kerb-to-kerb measured turning circle.

Tyre forces depend on longitudinal slip, lateral slip, normal load and the explicit surface parameters. Slip is regularized near rest to avoid division by zero. The model must enforce a combined-force bound: a tyre cannot independently spend its full traction allowance in both directions. Relaxation state must decay/reset appropriately when contact is lost. Wheel dynamics must include the equal/opposite contact reaction torque `−Fx r`.

The foundation is a brush-inspired saturation approximation, not fitted tyre data. The default longitudinal/lateral stiffnesses are 70,000 N per unit slip ratio and 60,000 N/rad at a 3,200 N reference load; force demand scales with load ratio to power 0.85. Effective friction scales with load ratio to power −0.12. Longitudinal/lateral relaxation lengths are 0.25/0.40 m, integrated using the exponential solution of a first-order relaxation equation; the 0.8 m/s low-speed floor prevents frozen state at rest. All coefficients are uncalibrated design assumptions.

The pure model defines dry asphalt, wet asphalt, concrete, gravel, compact dirt, sand, mud, grass, packed snow and ice parameter sets. The runtime surface selector exposes dry asphalt, wet asphalt, concrete, gravel, grass and ice; the proving ground has dedicated dry, wet, gravel and ice test lanes. Surfaces vary stiffness, relaxation, rolling resistance and sliding/peak friction, but remain rigid ground approximations. Pressure, temperature, wear, compound families, deformable carcass, terrain sinkage and soil shear-strength evolution are not implemented. The regularized tyre model has no static bristle-friction equilibrium; braked slope creep requires investigation in Unity before any hill-hold claim.

## Drivetrain and brakes

The engine integrates inertia against combustion, friction and clutch reaction torque. The driveline transfers bounded clutch torque through signed gear and final-drive ratios, with losses. Neutral disconnects wheel torque. A shift is a finite torque-interruption state; reverse changes torque direction rather than rotating the chassis. Wheel torques are distributed equally among configured driven wheels, an open-differential approximation. There is no true manual clutch/stall simulation, CVT/DCT, turbo, electric or hybrid powertrain in this foundation.

Brake torque opposes wheel angular motion and cannot accelerate a stopped wheel backwards merely because a large brake step overshoots zero. The runtime includes a wheel-pressure ABS modulator, slip-triggered engine-throttle traction control, and a simplified brake-work heating/cooling/fade calculation. These interventions require dynamic tests; labels in a HUD are not evidence. ESC, detailed hydraulics, differentiated disc/drum assemblies, complete brake thermal calibration and hill-hold remain production requirements.

At 100 km/h, using `ρ = 1.225 kg/m³`, the initial aerodynamic drag estimate `0.5ρCdAv²` is about 315 N. Base rolling resistance `Crr mg` is about 163 N. These are analytic sanity checks for the declared assumptions. Ideal constant-friction stopping distance `v²/(2μg)` neglects brake ramp-up, load sensitivity, wheel inertia, drag, gradient and ABS cycles; use it as an explanatory bound, never a universal target or a forced stopping-distance controller.

## Structural damage

For a one-dimensional material approximation, elastic work is `0.5 k x²` before yield. Plastic work must consume a nonnegative impact-energy allocation; residual energy and deformation saturation must be accounted for. Relative contact speed, effective mass and impulse constrain plausible available collision energy. Contact impulse alone is not a calibrated material damage measure.

The current hybrid foundation uses a 20-node quasistatic body cage with directional beam resistance. Its node plastic-work model is `(Fy + Hx) dx + 0.5 H dx²`; it solves for a displacement that fits the allocated energy and cumulative plastic-travel limit. Opposite impacts do not erase the plastic path history. Initial end/cabin node yield forces are 3,000/9,000 N, hardening 14,000/35,000 N/m and maximum cumulative travel 0.45/0.24 m. Beam yield/break strain thresholds are 0.025/0.22. These are uncalibrated numerical design assumptions, not material test data. Unspent impact energy is reported; there is no time-resolved node recoil or elastic beam dynamics.

The hybrid retains a rigidbody/collider shell and local permanent mesh deformation plus selected mechanical impairment. It is not finite-element analysis, a complete soft-body vehicle, or a validated crash-safety simulator. Production work includes proper member topology, force-displacement calibration, breakable attachments, non-rubber material response, collider/visual consistency and repeated-impact energy accounting.

## Required comparisons

Record configuration, time step, tyre coefficients, surface, weather, initial state and build revision for every run. Compare 60 and 120 Hz using the same physical inputs. Report objective error tolerances before tuning:

- Static sag/load and free-decay response; diagonal bump timing; low/high-speed potholes; airborne and landing contact.
- 0–50, 0–100, 80–120 km/h; quarter mile; top-speed equilibrium and gradients.
- 100–0 km/h on dry, wet and ice; ABS disabled/enabled; repeated braking/fade.
- Forward/reverse turning circles; skidpad; slalom; fishhook; high-speed straight-line stability.
- Front/side/rear/roof impacts; collision work, permanent deformation, component state, residual drivability and reset.

Real-device handling, engine integration and measured vehicle performance remain unverified until their corresponding tests run. Pure-model test results must be kept separate from Unity and Android evidence.
