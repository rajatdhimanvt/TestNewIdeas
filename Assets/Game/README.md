# 🎮 Ball Merge Game Architecture & Guide

Root directory ka complete document yaha bhi available hai:
👉 **[Root README.md](../../README.md)**

### 📂 Quick Directory Reference
- `Scripts/Core/`: `GameEvents.cs`, `GameState.cs`
- `Scripts/Data/`: `BallDataSO.cs`, `BallKitSO.cs`, `ThemeDataSO.cs`, `LevelDataSO.cs`
- `Scripts/Managers/`: `GameManager.cs`, `AudioManager.cs`, `DependencyManager.cs`, `BallKitManager.cs`, `ThemeManager.cs`
- `Scripts/Gameplay/`: `Ball.cs`, `BallDropper.cs`, `BallMergeManager.cs`, `ContainerBoundary.cs`, `DangerLine.cs`, `FloatingScoreManager.cs`, `GameplayController.cs`
- `Scripts/UiScripts/`: `UIBasePanel.cs`, `UIManager.cs`, `MainMenuPanel.cs`, `SettingsPanel.cs`, `GenericPopupPanel.cs`
- `Scripts/Editor/`: `BallMergeAssetCreator.cs`
- `Scripts/Utils/`: `Singleton.cs`

### 💡 Ball Merge Core Mechanics
- **Independent Visual Themes:** `ThemeDataSO` (Dark, Ocean, Sky, Sunset) aur `ThemeManager` environment visuals (Camera background, Container walls color, Danger line tint) ko handle karta hai. (Ball Kits se bilkul independent!).
- **Ball Kits System:** `BallKitSO` (Classic, Ocean, Sky) aur `BallKitManager` equipped ball skins + PlayerPrefs persistence handle karta hai.
- **Aim & Drop:** `BallDropper` screen pointer follow karta hai aur click par ball container me release karta hai.
- **Physical Container:** `ContainerBoundary` (Left Wall, Right Wall, Bottom Floor colliders) balls ko box me contain karta hai.
- **Danger Line / Overflow Limit:** `DangerLine` container me overflow line monitor karta hai. Settled balls > 3.0s rehene par warning line flash hoti hai aur Game Over trigger hota hai.
- **Floating Score Popups:** `FloatingScoreManager` merge midpoint par `+10`, `+32` pop-scale & fade animations spawn karta hai.
- **Merge on Collision:** Jab do identical tier ki `Ball` aapas me takrayengi, `BallMergeManager` unhe midpoint par merge karke next tier ball spawn karta hai + score add karta hai.

### 🛠️ Quick Asset & Scene Setup:
Unity Editor ke top menu me jayein:
👉 `Tools -> Ball Merge -> 2. Auto Setup Complete Game Scene` (3 Ball Kits, 4 Environment Themes, ThemeManager, BallKitManager, ScriptableObjects, DangerLine, FloatingScoreManager ke sath poori scene auto-setup ho jayegi!).
