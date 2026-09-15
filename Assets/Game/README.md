# 🎮 Ball Merge Game Architecture & Guide

Root directory ka complete document yaha bhi available hai:
👉 **[Root README.md](../../README.md)**

### 📂 Quick Directory Reference
- `Scripts/Core/`: `GameEvents.cs`, `GameState.cs`
- `Scripts/Data/`: `BallDataSO.cs`, `LevelDataSO.cs`
- `Scripts/Managers/`: `GameManager.cs`, `AudioManager.cs`, `DependencyManager.cs`
- `Scripts/Gameplay/`: `Ball.cs`, `BallDropper.cs`, `BallMergeManager.cs`, `ContainerBoundary.cs`, `GameplayController.cs`
- `Scripts/UiScripts/`: `UIBasePanel.cs`, `UIManager.cs`, `MainMenuPanel.cs`, `SettingsPanel.cs`, `GenericPopupPanel.cs`
- `Scripts/Editor/`: `BallMergeAssetCreator.cs`
- `Scripts/Utils/`: `Singleton.cs`

### 💡 Ball Merge Core Mechanics
- **Aim & Drop:** `BallDropper` screen pointer follow karta hai aur click par ball container me release karta hai.
- **Physical Container:** `ContainerBoundary` (Left Wall, Right Wall, Bottom Floor colliders) balls ko box me contain karta hai.
- **Merge on Collision:** Jab do identical tier ki `Ball` aapas me takrayengi, `BallMergeManager` unhe midpoint par merge karke next tier ball spawn karta hai + score add karta hai.
- **Level & Tiers SO:** `BallDataSO` (tier, size, color, score) aur `LevelDataSO` (droppable pool, container size, drop delay).

### 🛠️ Quick Asset Generation:
Unity Editor ke top menu me jayein:
👉 `Tools -> Ball Merge -> Generate Default Level Assets` (saare default tiers aur level data SO ek click me create ho jayenge).
