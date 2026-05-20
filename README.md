# The Alchemist’s Crypt

A tactical "Agentic FPS" where a centralized Gemini 3 Flash Live Hive-Mind controls a tomb of mummies. The player uses elemental alchemy to fight back while the AI adapts to their strategy.

## 🏺 Concept
In this ancient tomb, you are not just fighting monsters; you are fighting a living, thinking entity. The Hive Mind (powered by Gemini 3 Flash) observes your every move, adapts to your elemental choices, and coordinates the mummy guards to suppress your progress.

## ⚔️ Gameplay Systems
### The Alchemy System
Wield the Alchemical Focus with three distinct firing modes:
- 🔥 **Sulfur**: High AOE Fire Damage. Effective against groups, but the Hive Mind will command enemies to scatter.
- 💧 **Mercury**: Slows enemies. Used for crowd control, but triggers ambush responses.
- 💎 **Salt**: Stuns and Purifies. Critical for "High-Priest" enemies; triggers protective guard maneuvers.

### The Hive Mind (Council of Three)
- **Strategist (Pharaoh)**: Analyzes player behavior and coordinates tactical bundles (Flank, Ambush, Retreat).
- **Arbiter (Referee)**: Ensures fairness and prevents "impossible" scenarios (e.g., hard rule: no wipeout if player health < 30).
- **Empathy Agent (Reward)**: Maintains the "Flow State" by injecting mercy commands (refills/retreats) when the player is struggling.

## 🏗️ System Architecture & Tech Stack
To keep mobile and desktop frame rates perfectly smooth, heavy generative AI inference is decoupled from the local game engine and shifted entirely to the cloud.

- **Engine**: Unity 6 (Universal Render Pipeline).
- **Visuals**: PSX/Retro-style 3D models.
- **Target Platform**: Mobile (Primary) & Desktop.
- **AI Cloud Backend**: FastAPI (Python) deployed live on Render.
- **Orchestration**: Gemini 3 Flash via Google Antigravity.
- **Communication Link**: Unity client makes standard REST `POST` requests to the cloud server every 2 seconds, passing live `GameState` JSON matrices.

## 🧠 Live AI Integration Links
Our backend infrastructure is fully operational and listening for the Unity client framework:
- 🌐 **Live Server URL**: https://alchemists-crypt-backend.onrender.com
- 📑 **Interactive Swagger API Docs**: https://alchemists-crypt-backend.onrender.com/docs

Whenever an agent needs a tactical blueprint, Unity fires a request to the `/api/v1/hive-mind` endpoint, parses the negotiated JSON trace, and dynamically shifts enemy behavior loops.

## 📂 Project Structure
- `Artifacts/`: Implementation plans, task lists, and research notes.
- `Docs/`: Project documentation and guides.
- `Phases/`: Detailed phase-by-phase sprint documentation.
- `Assets/`: Unity project assets.

---
*Built with Google Antigravity | Challenge 4: Agentic Game Quest*
```"
