---
name: unity-workflow
description: Prepare and develop the Fabricated Dawn Unity 6 game, install its companion Editor toolkit into a user-selected local project, inspect project files, run scoped batch validation and prototype point-and-click story mechanics. Use for Unity C#, scene and content setup, checkpoint bugs or project preparation.
---

# Unity cho Fabricated Dawn

Use Vietnamese explanations. Read [project-brief.md](references/project-brief.md) before making story-dependent changes. Use the companion toolkit in `templates/com.4koq.fabricated-dawn/` as a starting point, not a finished game.

## Permission and scope

- The user requires explicit authorization for actions. Honor the concrete current request. A read/review request permits reads only; preparation or implementation permits files needed for that deliverable, not unrelated installation, source-document edits or publication.
- Google Docs source remains read-only. Use the document-reader skill to refresh it. Do not edit, comment on or share it from this workflow.
- Resolve the exact Unity project before writing. Inspect `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, `Assets/` and local AGENTS.md. If multiple projects are plausible, ask which one.
- Preserve user changes and `.meta` GUIDs. Never regenerate a live project's assets wholesale. Work in `Assets/FabricatedDawn` and the companion embedded package unless a broader change was requested.
- Do not install Unity, upgrade a project's Unity version, add paid assets or change render/input pipelines without authorization. Inspect installed versions; use the project's version when available.

## Tools shipped in this plugin

Paths below resolve from this skill's directory. Node.js is required. Use an absolute project path and the actual installed Unity executable.

```text
node tools/unity.mjs inspect --project "C:\path\Project"
node tools/install-toolkit.mjs --project "C:\path\Project"
node tools/unity.mjs scaffold --project "C:\path\Project" --unity "C:\path\Editor\Unity.exe"
node tools/unity.mjs validate --project "C:\path\Project" --unity "C:\path\Editor\Unity.exe"
node tools/unity.mjs verify --project "C:\path\Project" --unity "C:\path\Editor\Unity.exe"
```

- `inspect` is read-only. `install-toolkit` copies a new embedded package and refuses to overwrite an existing package. It does not change the project manifest or existing assets.
- `scaffold` creates missing asset definitions and a separate M4 developer scene. Existing catalog/assets are preserved. `validate` writes a JSON content report. `verify` also scaffolds and runs behavioral checks. These commands write project cache/logs and require a closed project.
- Batch helpers refuse a project with `Temp/UnityLockfile`. Do not kill an editor or remove its lock. For an open project use `Tools > Fabricated Dawn > Toolkit` manually or available authorized computer-use tools.
- Reports: `Assets/FabricatedDawn/Reports/content-report.json`, `verification-report.json`. Logs: `Logs/FabricatedDawn/<action>.log`.
- There is no bundled MCP server or live Unity connection. Do not claim to see the Unity Hierarchy, Console or Play mode through these scripts. File reports and successful batch runs are the available evidence.

## Implementation rules

1. Keep the game 2D fixed-view point-and-click; avoid creating an FPS controller or combat system.
2. Separate content definitions (ScriptableObjects) from mutable runtime state. Give rooms, items, puzzles, events and hotspots stable unique IDs.
3. PRESENT → PAST_ENCLOSED occurs once at M4, after loop and side-door puzzles, on opening the door. It changes the whole visual layer. Ordinary travel cannot reverse it. Sanctuary protection is independent and breaks at M6 after the dream.
4. Wrong item use does not consume resources. One-shot pickups/events cannot respawn on revisits. Optional interactions cannot block the main path.
5. Checkpoints serialize the whole story state in one snapshot: chapter, room, inventory and uses, clues, puzzles, events, unlocked rooms, choices, world, sanctuary, relationships and protection charges. Validate before replacing current state.
6. The memory knife affects only the wife/husband in the past space and cannot kill them or harm the child. Past-only items disappear on escape; journal evidence persists.
7. Endings require prior evidence. Keep True Ending disabled until its narrative rules are approved. The starter ending evaluator uses provisional flags; do not present it as the final design.
8. Follow the existing Unity pipeline. Add packages only for a demonstrated need. Prefer mouse-accessible UI with readable Vietnamese text, stable aspect ratio and visual equivalents for audio clues.

## Finish with evidence

Run the narrow meaningful checks for the change. Do not report compilation, Play mode, UI rendering or a build as verified unless that specific operation ran successfully. Report expected placeholder warnings separately from errors. Give the exact files created/changed, test counts and remaining open design decisions. The toolkit is preparatory infrastructure and a developer prototype; it does not implement eight finished playable chapters.
