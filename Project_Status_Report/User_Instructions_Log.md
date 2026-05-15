# User Instructions & Standing Rules

This document logs the specific operational constraints and preferences provided by the USER for the Pharaoh Mobile project.

## 🐙 GitHub & Version Control
*   **Mandatory Commits:** "The user wants me to commit and push changes to GitHub after every successful modification to the project."
*   **Conflict Awareness:** "The user may be making changes to the codebase at the same time. I should be careful not to overwrite their changes and be mindful of other changes happening in the repository."
*   **Diff Verification:** "I should always check the diff before reverting any changes."

## 🐍 Environment & Tooling
*   **Python Command:** "The user wants to use `python` instead of `python3` for running python scripts."
*   **Unity Version:** The project targets **Unity 6 LTS**, requiring the use of modern APIs like `FindObjectsByType` instead of the obsolete `FindObjectsSortMode`.

## 🎨 Design Aesthetics
*   **Visual Excellence:** "The USER should be wowed at first glance by the design."
*   **No Placeholders:** Use generated images/assets instead of placeholder colored boxes.
*   **Premium Theme:** Focus on "Pharaoh Gold", "Egyptian Stone", and high-contrast palettes rather than generic browser colors.

## 🛠️ Implementation Workflow
1.  **Plan and Understand:** Outline features before coding.
2.  **Core Design System:** Establish tokens (colors/fonts) in CSS/Scripting first.
3.  **Optimization:** Ensure smooth mobile 60fps performance through efficient physics checks.
