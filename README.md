#!/usr/bin/env text
# The Alchemist’s Crypt

<img src="https://avatars.githubusercontent.com/u/242056456?s=80&v=4" width="64" alt="Google Antigravity" />

<sub>**GOOGLE ANTIGRAVITY HACKATHON — AGENTIC GAME QUEST, CHALLENGE 4**</sub>

# 🏺 The Alchemist's Crypt

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

*"The curse does not breathe, yet it feels your pulse."*

**A tactical FPS where a Council of Three AI agents controls an ancient Egyptian tomb — and adapts to destroy you in real time.**

[🌐 Live API](https://alchemists-crypt-backend.onrender.com) · [📑 Swagger Docs](https://alchemists-crypt-backend.onrender.com/docs) · [🎮 Unity Repo](https://github.com/waybig125/Hackathon---Pharoah-Game) · [⚙️ Backend Repo](https://github.com/KAreebaSherwani/alchemists-crypt-ai)

</div>

---
*Built with Google Antigravity | Challenge 4: Agentic Game Quest*
