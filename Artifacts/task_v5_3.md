# Task Checklist - Pharaoh Game V5.3 Mechanics Polish & HUD Redesign

## Phase 1: Physics & Movement Stabilization (Jumping & Movement Fix)
- [ ] Modify `Movement.cs` Stand/Walk/Run capsule settings: `height = 1.8f`, `center = (0f, 0.9f, 0f)`
- [ ] Modify `Movement.cs` Crouch capsule settings: `height = 1.0f`, `center = (0f, 0.5f, 0f)`
- [ ] Replace `Time.deltaTime` with `Time.fixedDeltaTime` inside `FixedUpdate()` in `Movement.cs`
- [ ] Verify physics compilation and save changes.

## Phase 2: EventSystem & UI Raycasting (Mobile Button Clicks Fix)
- [ ] Modify `MobileHUDButtons.cs` to auto-inject `EventSystem` if null
- [ ] Ensure all custom buttons explicitly enable `raycastTarget = true`
- [ ] Disable raycasting on blocking transparent default UI overlay panels
- [ ] Verify button clickability and save changes.

## Phase 3: Spooky Minimap & Compass UI
- [ ] Implement `MinimapUI.cs` with circular Obsidian-Gold UGUI design
- [ ] Connect minimap zoom toggle on tap/click (normal vs zoomed-out view)
- [ ] Implement reverse Y-rotation for dynamic compass behavior
- [ ] Procedurally map static buildings, pyramids, trees at start
- [ ] Continuously update dynamic green dot indicators for active zombies
- [ ] Integrate Minimap with `MobileHUDButtons.cs` and save changes.

## Phase 4: Combat Loop (Zombie damageability & Gun hits)
- [ ] Modify `ZombieAI.cs` to add health variables (`maxHealth = 100f`, `currentHealth = 100f`)
- [ ] Implement `TakeDamage` and death actions (ragdoll, sand sinking, disable nav agent) in `ZombieAI.cs`
- [ ] Update `Projectile.cs` and `ProjectileScript.cs` to detect zombie collision and deal 25 damage per hit
- [ ] Verify combat gameplay loop compile and save.

## Phase 5: Button Scale & HUD Polish
- [ ] Scale down HUD left side buttons by 20% to increase mobile swipe space
- [ ] Refine label layout: Health on top-left, Ammo on bottom-left with custom bars
- [ ] Perform a full compile refresh, git commit, and final validation.
