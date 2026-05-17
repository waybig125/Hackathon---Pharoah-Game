# Implementation Plan: High-Fidelity Mobile HUD, Alchemy Ticks, & Production Build Readiness

This plan outlines the complete layout refactoring, premium charcoal & gold visual design, looping rigging/animation updates, and production APK packaging configurations.

---

## User Review Required

> [!IMPORTANT]
> **Premium charcoal matte texture**: All action buttons on the right side of the gameplay HUD, settings close/quit buttons, welcome screen buttons, and death screen buttons will be styled as block-rectangles with a procedural stone-textured matte charcoal background and alchemical gold text overlay, with absolutely NO borders.
> 
> **Zero-text HUD overlays**: The Health and Ammo HUD panels will have no text overlays (`healthText.text = "";` and `ammoText.text = "";`). In their place, a high-fidelity Left-Anchored Health Icon (`health_icon.png`) and an Active Element Icon (`sulphur.png`, `mercury.png`, or `salt.png`) will be dynamically loaded and displayed alongside the filled vitality bar and the 30 vertical alchemical ticks.
> 
> **Production APK / Unity Remote Compatibility**: To bypass editor-only API limitations, we will generate the fully configured Mummy prefab at editor-time and save it inside `Assets/Resources/egyptian_items/Mummy_Base.prefab`. During both editor and built standalone execution, the game will load mummies dynamically from Resources using `Resources.Load<GameObject>("egyptian_items/Mummy_Base")`, avoiding compilation/run crashes.

---

## Proposed Changes

### Component 1: Premium Charcoal Block HUD Redesign
We will replace all floating round action buttons with rectangular slabs, increase joystick sizes, and optimize the touch layout.

#### [MODIFY] [MobileHUDButtons.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/UI/MobileHUDButtons.cs)
- Implement `CreateCharcoalSprite` using procedural Perlin noise to generate a beautiful matte charcoal granite block texture.
- Style the HUD action buttons on the right (`FIRE`, `RELOAD`, `SWAP`, `SPRINT`, `JUMP`) as borderless charcoal block rectangles.
- Symmetrically size the buttons: `FIRE` (260x180), others (180x80), arranged in a clean, ergonomic mobile grid.
- Increase the procedural dimensions of the Dual Joysticks to **512x512** (Ring) and **256x256** (Knob) to eliminate pixelation and support ultra-crisp scaling.
- Position the settings icon cleanly in the top-right corner, loaded dynamically from `egyptian_items/settings_icon`.

---

### Component 2: Alchemy Ammo Ticks & Health Layout
We will remove text labels and implement left-anchored icons and 30 dynamic element ticks.

#### [MODIFY] [MobileHUDButtons.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/UI/MobileHUDButtons.cs)
- Anchor the Health and Ammo panels within mobile Safe Area bounds.
- Completely clear text overlays (`healthText.text = "";` and `ammoText.text = "";`).
- Create and place left-anchored icons inside the panels sized `64x64`:
  - Health Icon: Loads `health_icon` from `egyptian_items`.
  - Ammo Icon: Dynamically queries the player's `AlchemicalFocus.CurrentMode` and swaps the active element icon sprite (`sulphur`, `mercury`, `salt`).
- Redesign the Ammo fill bar into **30 individual vertical ticks**.
- Sync tick color dynamically in `Update()` to match the active element:
  - **Sulfur**: Alchemical Orange/Yellow (`new Color(1f, 0.65f, 0.1f, 0.95f)`)
  - **Mercury**: Glowing Cyan/Blue (`new Color(0.2f, 0.75f, 1f, 0.95f)`)
  - **Salt**: Pure Gold/White (`new Color(0.95f, 0.9f, 0.75f, 0.95f)`)
  - **Empty/Fired Ticks**: Translucent dark charcoal outline (`new Color(0.15f, 0.15f, 0.15f, 0.3f)`).

---

### Component 3: Looping Mummy Animator Rigging & Low Health
We will set loop parameters on animations and save a dedicated runtime prefab under Resources.

#### [MODIFY] [StaticEgyptianCityGenerator.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Editor/StaticEgyptianCityGenerator.cs)
- Force `loopTime = true` for Walk, Idle, and Attack animation clips in `getOrCreateClip()`.
- Procedurally instantiate, configure (Collider, NavMeshAgent, Rigidbody, ZombieAI), and save a master Mummy prefab to `Assets/Resources/egyptian_items/Mummy_Base.prefab` upon running city generation.

#### [MODIFY] [MummySpawner.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/AI/MummySpawner.cs)
- Replace all editor-only `#if UNITY_EDITOR` AssetDatabase loads with a unified `Resources.Load<GameObject>("egyptian_items/Mummy_Base")` call.
- This ensures 100% production build compliance and flawless execution inside built APK packages.

#### [MODIFY] [ZombieAI.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/AI/ZombieAI.cs)
- Set Mummy `maxHealth` and `currentHealth` to `10f` (10x lower than player's health).
- Block translation/sliding upon death, lock the animation state to `"Die"`, and disable navigation update loops completely.

---

### Component 4: Standalone Death Canvas Overlay
We will hide the interactive HUD entirely on death and render the spooky death screen inside a dedicated high-priority Canvas.

#### [MODIFY] [MobileHUDButtons.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/UI/MobileHUDButtons.cs)
- Create a dedicated `"DeathCanvas"` with `sortingOrder = 1100` on death.
- Deactivate `hudRootGo` completely on player death to clear the viewport of all gameplay buttons, labels, and joysticks.
- Style the Restart Voyage button as a premium borderless charcoal block with gold text.

---

### Component 5: Startup Scene Safeguard
We will verify that the active scene is always `MainGame` to prevent loading legacy scene files.

#### [MODIFY] [EditorAutoLoader.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Editor/EditorAutoLoader.cs)
- Add a startup check: if the open scene path is not `"Assets/Scenes/MainGame.unity"`, automatically open it inside the Editor to safeguard development progress.

---

## Verification Plan

### Automated Verification
- Recompile scripts and run the City Generator menu item inside the Editor.
- Verify that `Mummy_Base.prefab` is successfully saved under `Assets/Resources/egyptian_items/` and all animator loop settings are set correctly.

### Manual Verification
- Open the game: verify the welcome screen displays the full screen start image and contains borderless charcoal Start Voyage and Quit buttons.
- Click Start: verify that the gameplay HUD overlays (Dual Joysticks, Block Buttons, Vitality and Ammo Ticks) render beautifully without overlapping.
- Fire bullets: verify that the 30 vertical alchemical ticks decrease one by one and dynamically change colors when swapping element modes.
- Defeat mummies: verify that mummies play their humanoid death animation, stop moving, and do not slide.
- Die to a mummy: verify the mobile controls/joysticks fade out and the spooky borderless Restart screen appears.
