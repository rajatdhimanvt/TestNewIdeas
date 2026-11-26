# Ball Merge – Visual Level Editor & Destructible Blocks Guide

## Quick Start (2 Steps)
1. In Unity Editor, wait for scripts to compile (block sprites, `BlockLibrary`, 5 sample levels, and `LevelDatabase` auto-generate on compile).
2. Open the Visual Level Editor via top menu:
   - **`Tools -> Ball Merge -> 3. Open Visual Level Editor`** (Shortcut: `Ctrl+Shift+L`)
   - To open & populate the playable example scene anytime: **`Tools -> Ball Merge -> 5. Setup Block Testing Scene`** (`Assets/Scenes/Block testing.unity`).

---

## Core Gameplay Mechanics
- **Merge-Triggered Destruction:** Players drop balls from the top dropper (with an aim guide line). Whenever two identical balls **merge**, a glowing shockwave pulse damages all destructible blocks within `Merge Blast Radius`!
- **HP & Progressive Cracks:** Each block tracks its own `HP`. When damaged, the block shakes, flashes, spawns chip debris, shows `-1` floating text, updates its HP badge, and renders progressive crack sprites (`Crack_Stage1` → `Crack_Stage2` → `Crack_Stage3`). Reaching `0 HP` shatters the block with particles and score rewards.
- **Static Platforms / Shelves:** Brown wooden ledges (`Wood Shelf / Ledge`) and optional per-block attached shelves remain static (no gravity) to support crates and deflect falling balls.
- **Special Blocks:**
  - **Bomb Crate:** Explodes when destroyed, dealing area damage to nearby blocks in a chain reaction.
  - **Bonus Drop Crate:** Grants `+2` extra ball drops when destroyed.
  - **Ice Block:** Low-friction slippery frozen block with crystal crack visuals and frost shatter sound.
  - **Moving Platform:** Smoothly glides back and forth along a customizable travel path (drag the cyan **`END`** handle directly in the Level Editor!).
  - **Key Crate & Locked Vault:** Locked Vaults stay immune until a matching-channel Key Crate is destroyed, launching a flying glowing Key Orb to unlock them.
  - **Magnet Core:** Gently pulls (or repels with negative force) nearby balls inside its field radius for trick-shot merges.
  - **Bounce Bumper:** Pinball-style neon pad that kicks colliding balls across the container.
  - **Teleport Portals (Entry → Exit):** Balls entering a `Portal Entry` smoothly warp out of matching-channel (1–5) `Portal Exit` blocks behind barriers or into hidden pockets, shown with dotted channel links inside the Level Editor.
- **Audio, Combos, Animated Backgrounds & Rewards:**
  - **Theme-Aware Animated Background:** `ThemeAnimatedBackground` automatically renders subtle floating stars (`dark_theme`), rising ocean bubbles (`ocean_theme`), drifting clouds (`sky_theme`), or warm embers (`sunset_theme`) synced to `ThemeManager`.
  - **Procedural Arcade SFX:** Built-in synthesized sound effects (`ProceduralAudioGenerator`) play automatically for drops, combo-pitched merges, hits, breaks, ice cracks, explosions, key unlocks, portal warps, coin ticks, chest fanfare, and win/lose audio.
  - **Combo Streak & Camera Shake:** Rapid merges display **`COMBO x2!` / `MEGA x4!`** banners with rising chime pitches and subtle 2D camera micro-shake (`CameraShake2D`).
  - **Unused Drops Coin Bonus:** Winning a level awards base completion coins plus **`+20 Coins`** per remaining unused ball drop, displayed with an animated coin roll-up breakdown (`LevelWinPanel` & `BlockTestingController`).
  - **Milestone Chests (Every 5 Levels):** Completing Level 5, 10, 15, ... opens an interactive Treasure Chest popup (`MilestoneChestModal`) that unlocks a free unowned Ball Kit or Theme (plus bonus coins, or a coin jackpot once all kits/themes are owned).
- **Win & Loss Rules:**
  - **Win:** Clear all required destructible blocks (`DestroyAllBlocks` win condition).
  - **Lose:** Run out of `Max Drops` (checked after final balls & merges finish settling) or overflow past the `Danger Line`.

---

## Visual Level Editor Controls & Shortcuts

| Action | Shortcut / Control |
| :--- | :--- |
| Select / Move Tool | `Q` |
| Brush (Click to Place) | `B` or click any block in **Block Palette** |
| Erase Tool | `E` or `Right-Click` to cancel back to Select |
| Drag & Drop Placement | Drag any block card from **Block Palette** onto Canvas |
| Free Placement Mode | Toggle **Snap** off, or hold `Alt` while dragging/placing |
| Zoom In / Out | `Mouse Scroll Wheel` (zooms around cursor) or Toolbar slider |
| Pan Canvas | `Middle Mouse Drag`, `Right Drag`, `Alt + Left Drag`, or `Space + Left Drag` |
| Multi-Select | `Shift + Click` or Drag empty canvas for **Box Marquee Select** |
| Select All | `Ctrl + A` |
| Resize Block | Drag any of the 4 corner handles (`Shift` locks aspect ratio) |
| Rotate Block | Drag top circular handle (`Shift`/Snap locks to `15°` steps) |
| Nudge Selected | `Arrow Keys` (hold `Shift` for larger steps) |
| Copy / Paste / Duplicate | `Ctrl + C` / `Ctrl + V` / `Ctrl + D` |
| Delete Selected | `Delete` or `Backspace` |
| Undo / Redo | `Ctrl + Z` / `Ctrl + Y` |
| Save Level | `Ctrl + S` (or keep **Auto-Save** enabled) |
| Align / Distribute / Mirror | Select 2+ blocks → use Row 2 Toolbar (`Left`, `CtrX`, `Right`, `Top`, `MidY`, `Bot`, `Dist H`, `Dist V`, `Mirror X`) |

---

## Managing 100+ Levels & JSON
- **Level Browser (Left Panel):** Search levels, create new levels (`+ New`), duplicate (`Dup`), rename (`Rename`), delete (`Del`), and click **`Sync DB`** to register all levels into `Assets/Resources/BallMerge/LevelDatabase.asset`.
- **JSON Export / Import:** Export any level layout to `.json` or import `.json` into a new or existing `LevelDataSO`.
- **Live Testing:**
  - **`Preview Scene`**: Spawns the active level live in the currently open Unity scene.
  - **`▶ Play Level`**: Saves your level and immediately launches Play Mode in `Assets/Scenes/Block testing.unity` on that exact level!

---

## Architecture Reference
- **Data (`Assets/Game/Scripts/Data/`):** `BlockDefinitionSO`, `BlockLibrarySO`, `LevelBlockPlacement`, `LevelDataSO`, `LevelDatabaseSO`, `LevelJsonData`
- **Runtime (`Assets/Game/Scripts/Gameplay/Blocks/`):** `DestructibleBlock`, `LevelBlockManager`, `BlockRuntimeVisuals`, `BlockTestingController`
- **Editor (`Assets/Game/Scripts/Editor/`):** `BallMergeLevelEditorWindow`, `BallMergeLevelEditorWindow.Canvas`, `BlockSpriteGenerator`, `BlockLevelSetupUtility`
