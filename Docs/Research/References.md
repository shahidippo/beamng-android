# Research record

The sources below were fetched from their authors' public repositories on 23 October 2026. [sources.json](sources.json) records the exact response SHA-256 and byte count because the referenced branches can change. Source was read for API and modelling guidance, not copied into the implementation.

| Source | Relevant evidence |
| --- | --- |
| [Unity 6 Rigidbody bindings](https://raw.githubusercontent.com/Unity-Technologies/UnityCsReference/6000.0/Modules/Physics/ScriptBindings/Rigidbody.bindings.cs) | Unity 6 `linearVelocity`, `GetPointVelocity`, `AddForceAtPosition`, inertia and rigidbody configuration APIs |
| [Unity 6 physics bindings](https://raw.githubusercontent.com/Unity-Technologies/UnityCsReference/6000.0/Modules/Physics/ScriptBindings/Physics.bindings.cs) | Cast/query API signatures, layer masks and trigger filtering |
| [Unity Android player settings](https://raw.githubusercontent.com/Unity-Technologies/UnityCsReference/6000.0/Editor/Mono/PlayerSettingsAndroid.bindings.cs) | Supported build-setting fields and SDK enums |
| [Unity URP package metadata](https://raw.githubusercontent.com/Unity-Technologies/Graphics/6000.0/staging/Packages/com.unity.render-pipelines.universal/package.json) | URP 17.0.4 declares Unity 6000.0 and matching graphics dependencies |
| [Project Chrono Fiala tyre implementation](https://raw.githubusercontent.com/projectchrono/chrono/main/src/chrono_vehicle/wheeled_vehicle/tire/ChFialaTire.cpp) | Slip coordinates, saturation, vertical-load dependence, contact reset and rolling resistance |
| [Project Chrono spring/damper link](https://raw.githubusercontent.com/projectchrono/chrono/main/src/chrono/physics/ChLinkTSDA.cpp) | Force from extension and extension rate; equal/opposite force and attachment-point moments |
| [Project Chrono shaft clutch](https://raw.githubusercontent.com/projectchrono/chrono/main/src/chrono/physics/ChShaftsClutch.cpp) | Bounded shaft torque transfer and clutch modulation |

The Chrono Fiala implementation describes its relation to the MSC Adams transient Fiala model. It is a useful engineering reference; it does not supply calibrated coefficients for the fictional Aster vehicle. ImpactDrive's simplified combined-slip model must not be advertised as an exact implementation of Chrono, Adams, Pacejka, or experimentally fitted Fiala tyres.

Direct requests to `docs.unity3d.com`, `unity.com` and `projectchrono.org` were denied by the environment's network proxy (HTTP 403). The official source repositories above were available through `raw.githubusercontent.com`. We did not disable TLS verification or treat denied pages as reviewed documentation.

## Required research before production calibration

- Obtain manufacturer or laboratory tyre force data across vertical load, pressure, temperature, camber and combined slip. Record licence and measurement conditions.
- Establish chassis mass/inertia and spring/damper targets from measured components or a declared fictional engineering design, including sprung/unsprung partition and motion ratios.
- Validate torque, losses and clutch behaviour with a dynamometer-equivalent test and an energy balance; do not tune acceleration by replacing physical forces with a speed curve.
- Select and document structural materials and member geometry. Derive stiffness/yield estimates, then compare measured force-displacement and impact outcomes; collision animation appearance is insufficient.
- Review current Unity Android manual and the exact Editor's SDK/NDK/OpenJDK module requirements on a connected licensed workstation before the first APK build.

Standard SI equations used in the foundation and its tests are derived in [Physics.md](../Physics.md). All numeric vehicle and surface values lacking measured evidence are labelled design assumptions. This record is preliminary technical research, not completion of the brief's automotive validation programme.
