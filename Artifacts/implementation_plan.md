# Phase 1: Foundation & Mobile FPS Controller

Establish the core gameplay loop for "The Alchemist’s Crypt" with a mobile-first, high-performance Rigidbody controller and alchemical weapon systems.

## User Review Required

> [!IMPORTANT]
> - **Input System**: We are using the **Legacy Input System** as per the Constitution. I will implement a custom `MobileInputManager` to bridge UI Joysticks with the Rigidbody controller.
> - **Target Frame Rate**: I will set `Application.targetFrameRate = 30` globally for mobile stability.

## Proposed Changes

### Project Setup
- [NEW] `Assets/Scripts/Core/ObjectPooler.cs`: Generic object pooling system.
- [NEW] `Assets/Scripts/Player/PlayerController.cs`: Rigidbody-based movement and camera look.
- [NEW] `Assets/Scripts/Input/MobileInputManager.cs`: Handles UI Joystick and Touch Zone input.
- [NEW] `Assets/Scripts/Weapons/AlchemicalFocus.cs`: Manages the three shooting modes (Sulfur, Mercury, Salt).
- [NEW] `Assets/Scripts/Weapons/Projectile.cs`: Base class for alchemical projectiles.

### UI Implementation
- [NEW] `Assets/Prefabs/UI/MobileHUD.prefab`: Contains the Joystick (Left) and Touch Zone (Right).

### Scene Setup
- [NEW] `Assets/Scenes/MainGame.unity`: Initial testing environment with basic geometry.

## Verification Plan

### Automated Tests
- I will use `mcp_unityMCP_execute_code` to verify the ObjectPooler initialization and projectile retrieval.

### Manual Verification
- Test movement and looking in the Unity Editor using simulated touches or mouse/keyboard fallback.
- Verify the three shooting modes have distinct colors (Orange, Teal, White).
- Confirm the frame rate is capped at 30.
