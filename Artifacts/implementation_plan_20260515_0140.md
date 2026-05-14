# Asset Polish & Advanced Systems Plan

Based on your feedback, we need to address the TMP errors, fix the continuous movement bug, implement advanced movement (jump, crouch, sprint), add sound and weapon swapping, and expand the UI. 
As requested, I have split this into manageable sub-phases.

## User Review Required
> [!IMPORTANT]
> The `P_LPSP_FP_CH` asset actually **does not** have jumping or crouching implemented in its `Movement.cs` script (the comment in the script is misleading). I will have to write the jumping and crouching physics logic from scratch and inject it into the asset pack's scripts. Please confirm you are okay with me heavily modifying the pack's `Movement.cs` and `Character.cs`.

## Proposed Changes

### Phase 1.1: Core Bug Fixes
- **TMP Import Error**: The error occurs because TextMeshPro tries to auto-import its essential resources while you are in Play Mode. I will execute a script to force-stop Play Mode and automatically import the TMP Essential Resources.
- **Continuous Movement Bug**: 
  - [MODIFY] `MobileInputManager.cs` -> Reset `h` and `v` axes to `0` when no desktop keys are pressed, preventing the character from moving infinitely.
  - Fix look input swallowing so the mouse can control FOV/Look when testing on desktop.

### Phase 1.2: Advanced Movement Injection
- [MODIFY] `Assets/Infima Games/.../Movement.cs`
  - Add `JumpForce` and `CrouchSpeed`.
  - Implement `rigidBody.AddForce` for jumping when grounded.
  - Implement capsule height adjustment for crouching.
- [MODIFY] `Assets/Infima Games/.../Character.cs`
  - Inject Jump and Crouch states.
  - Expose public methods `SetSprinting(bool)`, `SetCrouching(bool)`, `TriggerJump()` so the Mobile HUD can call them.

### Phase 1.3: Audio & Weapon Systems
- The asset pack already has high-quality sounds configured on the weapons and footsteps. They just need to be unmuted/triggered.
- [MODIFY] `Character.cs` -> Expose a `SwapWeapon()` method that calls `inventory.EquipNext()`.

### Phase 1.4: Mobile HUD Expansion
- [MODIFY] `MobileHUD` (Scene)
  - Add 4 new buttons: **Sprint**, **Jump**, **Crouch**, and **Swap Weapon**.
  - These buttons will only be visible when `Application.isMobilePlatform` is true (or simulated).
  - Since you requested no AI-generated SVGs, I will construct visually clean buttons using Unity's built-in standard UI shapes (Circles, Rounded Rectangles) and carefully tuned colors/transparencies to make them look professional.

## Verification Plan
1. Enter Play Mode.
2. Verify TMP errors are gone.
3. Verify the player stops moving when keys are released.
4. Verify the new Jump and Crouch mechanics work properly.
5. Verify the Mobile UI buttons trigger the correct actions and hide themselves when on Desktop.
