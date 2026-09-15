# 🎮 Game Architecture Overview & Guide

Root directory ka complete document yaha bhi available hai:
👉 **[Root README.md](../../README.md)**

### 📂 Quick Directory Reference
- `Scripts/Core/`: `GameEvents.cs`, `GameState.cs`
- `Scripts/Managers/`: `GameManager.cs`, `AudioManager.cs`, `DependencyManager.cs`
- `Scripts/UiScripts/`: `UIBasePanel.cs`, `UIManager.cs`, `MainMenuPanel.cs`, `SettingsPanel.cs`, `GenericPopupPanel.cs`
- `Scripts/Gameplay/`: `GameplayController.cs`
- `Scripts/Utils/`: `Singleton.cs`

### 💡 Core Usage Cheat Sheet
- **Change State:** `GameManager.Instance.ChangeState(GameState.InGame);`
- **Show UI:** `UIManager.Instance.ShowPanel<SettingsPanel>();`
- **Show Popup:** `UIManager.Instance.ShowPopup("Title", "Message", onConfirmCallback);`
- **Play Audio:** `GameEvents.OnPlaySFX?.Invoke("Click");`
- **Restart Game:** `GameManager.Instance.RestartGame();`
