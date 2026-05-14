# Phase 2: The Hive Mind Bridge

## Goals
- Establish a centralized logic system: `HiveMindManager.cs`.
- Create a stateful WebSocket connection to Gemini 3 Flash Live.
- Implement the "Command Loop" (2-second intervals).
- Set up the "Council of Three" internal reasoning:
    - **Strategist**: Tactical maneuvers.
    - **Arbiter**: Fairness Veto system.
    - **Empathy Agent**: "Divine Mercy" injection for player flow.

## JSON Protocol
- **Input**: Game state (Chamber, Player Pos, Active Element, Health, Mummies Pos/HP).
- **Output**: Tactical Bundle (Hive tactic, reasoning trace, arbiter check, per-mummy actions, narration).

## Tasks
- [ ] Create `HiveMindManager.cs`.
- [ ] Implement WebSocket client for Gemini Flash Live.
- [ ] Define JSON serialization/deserialization models.
- [ ] Implement behavior assignment system for mummies.
