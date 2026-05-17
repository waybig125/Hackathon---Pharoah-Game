# Rigging, UI, & Game Start Screen Task Checklist

- [ ] **1. Editor Asset Optimization & Build Settings**
  - [ ] Modify `EditorAutoLoader.cs` to optimize both `egypt_themed_icons` and `egyptian_items` asset folders as uncompressed crisp Sprites.
- [ ] **2. Rigging & Non-Looping Death Animations**
  - [ ] Modify `StaticEgyptianCityGenerator.cs` to add `mummy_death.fbx` humanoid configuration.
  - [ ] Update extraction helper in `StaticEgyptianCityGenerator.cs` to support non-looping configuration.
  - [ ] Add the `"Die"` state mapped to the native non-looping `mummy_death` animation in the Animator Controller.
- [ ] **3. Targeting Reticle, Layout Pivots, & Settings Button**
  - [ ] Modify `MobileHUDButtons.cs` to correct the pivots of the health (`Top-Left`) and ammo (`Bottom-Left`) panels.
  - [ ] Modify `MobileHUDButtons.cs` to load `settings_icon` from `egyptian_items/` and reposition next to the minimap at `(-320, -70)`.
  - [ ] Add a beautiful procedurally generated gold targeting reticle with a ruby dot center to `MobileHUDButtons.cs`.
  - [ ] Implement `GetRobustFont()` helper in `MobileHUDButtons.cs` to prevent legacy font initialization crashes.
- [ ] **4. Game Start Screen & Menu System**
  - [ ] Add a full-screen Game Start Menu to `MobileHUDButtons.cs` using `GameStartImage.jpeg`.
  - [ ] Add an elegant gold `"START VOYAGE"` button and `"QUIT GAME"` button.
  - [ ] Implement start-screen locking: pause gameplay (`Time.timeScale = 0f`), unlock cursor, and hide gameplay HUD until start is clicked.
- [ ] **5. Crash-Free Death Screen Overlay**
  - [ ] Modify `PlayerHealth.cs` to implement `GetRobustFont()` to prevent crashes.
  - [ ] Modify `PlayerHealth.cs` to recursively assign the UI layer (`5`) to `DeathCanvas` and its children for flawless rendering.
