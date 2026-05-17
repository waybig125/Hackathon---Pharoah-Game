# Task Progress: Stabilizing Pharaoh Mobile Experience

- `[ ]` **Phase 1: Welcome & Start Screen Design**
  - `[ ]` Force set `SpriteImportMode.Single` on all texture imports under `egyptian_items` in [EditorAutoLoader.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Editor/EditorAutoLoader.cs)
  - `[ ]` Update `CreateStartScreen` in [MobileHUDButtons.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/UI/MobileHUDButtons.cs) to stretch the background, load `egyptian_items/GameStartImage` properly, and add Start / Quit buttons
  - `[ ]` Disable game controls and pause time scale on welcome screen; resume and enable HUD controls upon clicking "Start"

- `[ ]` **Phase 2: Custom Bullet Ammo HUD & Health Bar Layout**
  - `[ ]` Redesign the Ammunition HUD in [MobileHUDButtons.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/UI/MobileHUDButtons.cs) as a grid of 30 gold bullet indicators
  - `[ ]` Position the Health bar at the bottom-left and the Ammo grid at the bottom-right within mobile safe area boundaries
  - `[ ]` Add a beautiful central crosshair target pointer to the gameplay canvas

- `[ ]` **Phase 3: Zombie Rigging, Attack Loop, and Death Animations**
  - `[ ]` Improve `getOrCreateClip` in [StaticEgyptianCityGenerator.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Editor/StaticEgyptianCityGenerator.cs) to fallback robustly to the first humanoid clip inside `mummy_death.fbx`
  - `[ ]` Correct [ZombieAI.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/AI/ZombieAI.cs) to disable navigation completely, lock state to `"Die"`, and limit maximum health to 10f

- `[ ]` **Phase 4: Fully Transparent Death Screen & Post-Death Layout**
  - `[ ]` Integrate HUD callbacks in [PlayerHealth.cs](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Assets/Scripts/Player/PlayerHealth.cs) to hide active mobile controls upon player death
  - `[ ]` Design and spawn a gorgeous semi-transparent death screen overlay with a gold-leaf "Respawn" button in the center

- `[ ]` **Phase 5: Mobile Build & USB Debugging Guide**
  - `[ ]` Create [mobile_build_guide.md](file:///Users/mac/Documents/Hackathon/Hackathon%20-%20Pharoah%20Game/Artifacts/mobile_build_guide.md) with comprehensive configuration settings
