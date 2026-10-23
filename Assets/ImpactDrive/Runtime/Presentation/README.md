# Development presentation

`GameplayPresentation.Create(vehicle)` constructs the interface, an independent driving
camera and original four-cylinder development audio. It expects an initialized
`VehicleController`. The bootstrap creates it once per proving-ground scene.

Implemented controls:

- Analogue stick, wheel, separate left/right buttons, or gravity-sensor tilt steering.
- Separate captured pointer IDs for steering, pedals, handbrake and camera look.
- Analogue throttle/brake: the bottom of the pedal is zero; drag toward the top for one.
- Drive/reverse selection, neutral, shift down/up, automatic/manual transmission.
- Engine toggle, repair, return to spawn, six camera views and quarter-speed time.
- Pause, steering sensitivity/dead zone, tilt calibration, ABS/TC combinations,
  speed-dependent steering assistance, volume, quality level, target render rate.
- Actual speed, RPM, gear, health, wheel contact/load/compression/slip/brake temperature
  and ABS telemetry. The RPM bar ends at the configured engine redline.
- Safe-area layout and explicit pause/reset of held controls on focus loss or suspension.

Desktop: W/S or up/down arrows operate pedals; A/D or left/right arrows steer;
Space is the handbrake; R changes direction when nearly stopped; Q/E shift;
C changes view; Backspace returns to spawn; Escape opens/closes pause;
hold right mouse to look. Touch and keyboard can be used simultaneously.

Settings are version 1 JSON inside a 128 KiB bounded, SHA-256-checked envelope in
`Application.persistentDataPath`. Writes flush a temporary file before replacing the
primary and retaining a previous-good backup. Corrupt primaries recover from backup.
Unknown schema versions revert to defaults. No historical schema migration is claimed.
No vehicle condition, garage, career or replay save format is implemented here.

Rendering target and quality options do not write `Time.fixedDeltaTime`. Time control
changes `Time.timeScale`; it is a live simulation slowdown, not a replay system.
Tilt currently uses `Input.acceleration` for gravity-based steering. Gyroscope fusion,
controller bindings, drag-to-reposition controls and haptics are not implemented.
The engine signal is original procedural synthesis driven by RPM and throttle,
not a calibrated production recording. No suspension, tyre or impact sound library
is included.

## Required Unity and device checks (not executed in this environment)

1. Play the proving-ground scene; ensure every visible action operates its stated system.
2. Hold steering, partial throttle and brake with three fingers; release one and verify
   the other two persist. Drag each finger outside its original control and release.
3. Background the app while holding throttle. Return to the pause screen and resume;
   throttle must stay zero until a new press.
4. Test tilt calibration in both landscape rotations and on a flat stationary device.
5. Verify all six camera positions, near clipping and obstruction avoidance.
6. Change 30/60 render targets and quality while confirming identical physics timestep.
7. Close/reopen the app after setting controls and sound; verify restoration. Corrupt
   the primary save and verify backup restoration, then test storage-full handling.
8. Check controls on small phones, tablets and display cutouts. Inspect independent
   pointer release, input latency and text readability on physical Android hardware.
9. Listen for synthesis dropouts, volume artifacts and focus/pause behavior on device.

Only the platform-independent save-file tests can run without Unity. Source presence
and those tests do not establish touch, rendering, audio or Android readiness.
