# Egyptian Gold Mobile HUD & Rigging Animation Optimization Plan

This implementation plan details the steps to fully resolve the remaining mobile HUD layout, texture crispness, looping rigging animations, and minimap visual aesthetics issues.

## Proposed Changes

We will systematically modify five key scripts across the project to implement high-fidelity optimizations:

---

### 1. Editor Scene & Build Settings Setup

#### [MODIFY] [EditorAutoLoader.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Editor/EditorAutoLoader.cs)
- Automatically add `Assets/Scenes/MainGame.unity` as the active scene 0 in Unity's Build Settings.
- Implement an automated `TextureOptimizer` routine that scans all themed PNG assets in `Assets/Resources/egypt_themed_icons/` on load, forcing their import settings to be **Sprite (2D and UI)**, **Uncompressed (None)**, and **Disable Mip Maps** for absolute pixel-perfect quality.

---

### 2. Mummy/Zombie Rigging & Animation Loops

#### [MODIFY] [StaticEgyptianCityGenerator.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Editor/StaticEgyptianCityGenerator.cs)
- Refactor the read-only FBX animation extraction method. Instead of applying temporary changes to read-only FBX sub-assets (which Unity ignores on play), we will programmatically **duplicate** the walk, idle, and attack clips into dedicated native `.anim` files under `Assets/Mummy_Assets/` (`Walk_loop.anim`, `Idle_loop.anim`, `Attack_loop.anim`).
- Force `loopTime = true` and `loop = true` on these native serialized assets.
- Assign these looping native animation assets directly to the Animator Controller states.

#### [MODIFY] [ZombieAI.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/AI/ZombieAI.cs)
- Wrap parameter settings in try-catch to completely suppress warnings.
- Ensure that the attack loop is fully stable and transitions seamlessly between states.

---

### 3. Crisp Mobile HUD & Layout Refinements

#### [MODIFY] [MobileHUDButtons.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/UI/MobileHUDButtons.cs)
- **Buttons Enlargement**: Increase Fire Button size to `380` and adjust position. Increase Reload, Swap, Sprint, and Jump button sizes to `200`-`220` with comfortable spacing to prevent finger overlaps.
- **Settings Icon Relocation**: Reposition the Settings Medallion to the top-right corner (`anchorMin = anchorMax = Vector2.one`, `anchoredPosition = new Vector2(-60, -60)`), directly above the minimap.
- **Layout Safety Padding**: Add a comfort offset of `-50, 50` to the action button container to prevent cutoff on notch/bezel mobile screens.
- **Aggressive Canvas Purging**: Update `Update()` to continuously disable competing UI Canvas objects (including clones containing `lpsp`, `hud`, or `canvas` in their names), keeping only `MobileHUD_Root` active.

---

### 4. High-Fidelity Egyptian Minimap Pointer

#### [MODIFY] [MinimapUI.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/UI/MinimapUI.cs)
- Redesign `CreatePlayerArrowSprite` to render a gorgeous, high-fidelity Egyptian golden pointer:
  - Rich 3D gold border utilizing horizontal coordinate shadow factors for realistic metallic shading.
  - Glowing Red Ruby gem at the core of the pointer.
  - Deep glittering Lapis Lazuli inlay with procedural specular highlight/glint reflection.

## Verification Plan

### Automated Verification
- We will verify script compilation using the Unity Asset Database refresh tool.
- We will inspect the generated looping `.anim` assets.

### Manual Verification
- Run the game and visually inspect the button crispness, scaled-up joystick, redesigned metallic gold minimap arrow, and looping mummy walking/attack rigging animations.
