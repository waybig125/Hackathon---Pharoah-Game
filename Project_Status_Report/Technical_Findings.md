# Technical Findings & Critical Bug Fixes

This document details the major engineering hurdles overcome during the stabilization of the Pharaoh Mobile architecture.

## 1. The "Magenta Line" Artifact (Shader Incompatibility)
*   **Finding:** GLB models imported via procedural code were defaulting to the Standard Shader, which is incompatible with the Universal Render Pipeline (URP).
*   **Fix:** Implemented `AggressiveMaterialFixer` in `EgyptianCityGenerator.cs`.
    *   Iterates through every `sharedMaterial` on every sub-mesh.
    *   Force-instantiates a new `Universal Render Pipeline/Lit` material.
    *   Copies existing texture maps (`_MainTex`, `_BaseMap`) and colors (`_Color`, `_BaseColor`) to the new shader.

## 2. Physics NaN/Infinity Crash
*   **Finding:** Rapid touch inputs on the mobile joystick occasionally resulted in non-finite math results. Assigning these to `rigidbody.velocity` caused the Unity physics engine to crash.
*   **Fix:** Patched `Movement.cs` with a `Vector3` validation check.
    *   If `velocity.y` is `NaN` or `Infinity`, it is hard-reset to `0.0f`.
    *   This prevents console errors and player teleportation bugs.

## 3. Mobile UI Distortion (Aspect Ratio Stretching)
*   **Finding:** The procedural `Canvas` was being created with a default `CanvasScaler` that didn't account for ultrawide mobile screens, leading to oval-shaped joysticks and buttons.
*   **Fix:** Refined `MobileHUDButtons.cs`.
    *   Set `screenMatchMode` to `MatchWidthOrHeight`.
    *   Set `matchWidthOrHeight` to `0.5f` (perfect balance).
    *   Forced destruction of old Canvas instances to prevent UI stacking.

## 4. Unity 6 API Deprecations
*   **Finding:** Numerous scripts were throwing warnings for `FindObjectsSortMode`.
*   **Fix:** Updated all calls to `FindObjectsByType<T>(FindObjectsSortMode.None)` to modern `FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)`.

## 5. Procedural Rotation Artifacts
*   **Finding:** Trees and columns were spawning with random X/Z tilts due to local space conversion errors.
*   **Fix:** Enforced `Quaternion.Euler(0, Random.Range(0, 360), 0)` for all procedural props to ensure they stay grounded.
