# Implementation Plan: Horror Egyptian Refactor and Fixes

## Phase 1: Critical Fixes & Adjustments
1. **Fog & Atmosphere:** 
   - Update `AtmosphereManager.cs` or the generator to change fog color to a yellowish tint.
2. **Desktop Input Fix:**
   - Investigate and fix `Character.cs` or `MobileInputManager.cs` to ensure desktop mouse and keyboard inputs correctly drive the player when not on mobile. The `MobileHUDButtons` fix hid the UI, but the input routing might still be intercepting or overriding desktop input.
3. **Mobile UI Stretching Fix:**
   - Update the `CanvasScaler` in `MobileHUDButtons.cs` from `matchWidthOrHeight = 0.5f` to `matchWidthOrHeight = 1f` (match height) or `Expand/Shrink` to prevent UI stretching on ultrawide mobile screens while keeping aspect ratios correct.
4. **Temple Visibility:**
   - Ensure the Central Temple is scaled properly and placed where the player can see it, and check if it's being occluded by fog or generated too far away.

## Phase 2: Weather, Audio, & Vibe
1. **Subtle Rain & Clouds:**
   - Add a subtle rain particle system attached to the player or global scene.
   - Add a slow-moving cloud particle system high in the sky.
2. **Audio/SFX:**
   - Source ambient/rain SFX from `Low Poly Shooter Pack` or `Inspiration` pack.
   - Add an AudioSource for ambient rain/wind.
3. **Horror Glows:**
   - Add subtle, eerie glows (e.g., green, purple, or deep red) to specific prefabs or generation points to enhance the horror vibe.

## Phase 3: Generator Refactor & Complex Houses
1. **File Management:**
   - Rename `EgyptianCityGenerator.cs` to `RandomEgyptianCityGenerator.cs`.
   - Create a new `StaticEgyptianCityGenerator.cs`.
2. **Static Layout Logic:**
   - Instead of `Random.value`, use a mathematical layout (e.g., a predefined grid array or noise function with fixed seeds) to place roads, the temple, and houses predictably.
3. **Complex Houses:**
   - Modify the house generation logic to compose houses from multiple blocks (e.g., a 2x1 base, with a 1x1 second floor).
4. **Pillars:**
   - Integrate `egyptian_pillar_column.glb` into the generation, placing them along main roads or around the temple.

## Phase 4: Player Health
1. **Health System:**
   - Implement a basic health script on the Player.
2. **Health UI:**
   - Add a health bar or text to the `MobileHUD` (or desktop HUD).
