# 🎮 Production-Grade Unity Game Architecture & Framework

Yeh document project ke current architecture, folder structure, core managers aur workflows ki complete guide hai. Jab bhi project me new features ya systems add honge, is document ko accordingly update rakha jayega.

---

## 📁 1. Project Directory Structure

```text
Assets/
├── Game/
│   ├── Art/                      # Materials, Textures, Sprites
│   │   ├── Materials/
│   │   └── Sprites/
│   ├── Audio/                    # Music tracks & Sound effects
│   ├── Data/                     # ScriptableObjects, Configs, Game Data
│   ├── Prefabs/                  # Reusable UI & Gameplay prefabs
│   ├── Scene/                    # Unity scenes (GameTest, Boot, Gameplay)
│   └── Scripts/
│       ├── Core/                 # Foundational architecture patterns
│       │   ├── GameEvents.cs     # Global decoupled event bus
│       │   └── GameState.cs      # High-level game state enum
│       ├── Managers/             # Central game controllers & services
│       │   ├── DependencyManager.cs # Service locator / DI container
│       │   ├── GameManager.cs       # Master game lifecycle coordinator
│       │   └── AudioManager.cs      # BGM & SFX playback + volume management
│       ├── UiScripts/            # UI Panel stack & screens
│       │   ├── UIBasePanel.cs       # Base animated panel class
│       │   ├── UIManager.cs         # Panel navigation, popups & overlays
│       │   ├── MainMenuPanel.cs     # Main menu screen
│       │   ├── SettingsPanel.cs     # Audio & volume controls
│       │   └── GenericPopupPanel.cs # Reusable alert/confirm modal dialog
│       ├── Gameplay/             # Game-specific logic
│       │   └── GameplayController.cs # Starter session loop & score tracker
│       └── Utils/                # Utilities & helpers
│           └── Singleton.cs         # Persistent & safe generic Singleton
└── Plugins/
    └── Demigiant/DOTween/        # High-performance tweening engine
```

---

## ⚙️ 2. Core Architecture Systems (`Scripts/Core/` & `Scripts/Utils/`)

### 🔹 `Singleton<T>`
- **Location:** `Assets/Game/Scripts/Utils/Singleton.cs`
- **Kaam:** Generic MonoBehaviour Singleton pattern jo duplicate gameobjects ko safely destroy karta hai, memory leak prevent karta hai, aur optional `dontDestroyOnLoad` support karta hai.
- **Usage:**
  ```csharp
  public class MyManager : Singleton<MyManager>
  {
      // Accessible via MyManager.Instance anywhere
  }
  ```

### 🔹 `DependencyManager` (Service Locator)
- **Location:** `Assets/Game/Scripts/Managers/DependencyManager.cs`
- **Kaam:** Production-grade Service Locator jo MonoBehaviours aur pure C# classes/interfaces dono ko centrally register aur resolve karta hai.
- **Key Methods:**
  - `DependencyManager.Instance.Register<T>(service)`
  - `DependencyManager.Instance.Resolve<T>()`
  - `DependencyManager.Instance.TryResolve<T>(bool searchInScene = true)`
  - `DependencyManager.Instance.Unregister<T>()`

### 🔹 `GameEvents` (Decoupled Event Bus)
- **Location:** `Assets/Game/Scripts/Core/GameEvents.cs`
- **Kaam:** Tight coupling ko khatam karta hai. Scripts bina direct reference liye ek dusre ko notify kar sakti hain.
- **Available Events:**
  - `OnGameStateChanged(GameState newState, GameState oldState)`
  - `OnGamePaused(bool isPaused)`
  - `OnScoreChanged(int currentScore)`
  - `OnHighScoreChanged(int highScore)`
  - `OnPlaySFX(string soundKey)`
  - `OnPlayMusic(string musicKey)`
  - `OnLevelStarted(int level)`
  - `OnLevelCompleted(int level, bool success)`

### 🔹 `GameState`
- **Location:** `Assets/Game/Scripts/Core/GameState.cs`
- **Enum States:** `Boot`, `MainMenu`, `Loading`, `InGame`, `Paused`, `GameOver`.

---

## 🕹️ 3. Core Managers (`Scripts/Managers/`)

### 1. `GameManager`
- **Lifecycle & State Machine:**
  - `BootSequenceRoutine()`: Boot sequence me sabhi systems ko guaranteed order me initialize karta hai (Audio -> Save -> SceneLoader -> UI) aur `GameState.MainMenu` me switch karta hai.
  - `StartGame()`: State ko `GameState.InGame` me le jata hai.
  - `SetPause(bool)` / `TogglePause()`: Game ko freeze/unfreeze karta hai (`Time.timeScale`).
  - `TriggerGameOver(int score)`: Score save karta hai aur GameOver state me le jata hai.
  - `QuitGame()`: Standalone aur Editor dono me safely exit karta hai.

### 2. `AudioManager`
- **BGM & SFX Management:**
  - Dedicated AudioSources for background music aur sound effects.
  - Inspector me `soundEffects` aur `musicTracks` define karne ki library.
  - Volume control (Master, Music, SFX) with auto-save in PlayerPrefs.
  - `PlaySFX("Click")`, `PlayMusic("Theme")`, `PlayClip(AudioClip)`.
  - Mute toggle.

---

## 🖥️ 4. UI Framework (`Scripts/UiScripts/`)

### 🔹 `UIBasePanel`
- Sabhi UI screens ka base class.
- **Features:**
  - CanvasGroup ke through smooth DOTween alpha fade transition (`fadeDuration`).
  - Optional punchy scale animation (`animateScale = true`).
  - Animation ke dauran clicks ko block karna (`blocksRaycasts = false`) taaki double tap glitch na ho.
  - Optional background dim overlay trigger (`showBackgroundDim = true`).
  - Lifecycle hooks: `OnInit(UIManager)`, `OnShow()`, `OnShown()`, `OnHide()`, `OnHidden()`.

### 🔹 `UIManager`
- Screen stack navigation system.
- **Key Methods:**
  - `ShowPanel<T>()`: Panel show karta hai aur purane panel ko history stack me push karta hai.
  - `GoBack()`: Previous screen par wapas le jata hai (Back button logic).
  - `ShowPopup(title, message, onConfirm, onCancel)`: Modal dialog box open karta hai.
  - `ShowLoader(bool)`: Loading overlay on/off karta hai.
  - CanvasScaler aspect ratio auto-matching.
  - Automatically har child button par click sound hook kar deta hai.

### 🔹 Built-in Panels:
1. **`MainMenuPanel`**: Play, Settings, High Score Display, Quit button with confirm modal.
2. **`SettingsPanel`**: Master Volume, Music Volume, SFX Volume sliders, Mute toggle, aur Back button.
3. **`GenericPopupPanel`**: Dynamic alert / confirmation box (Title, Description, OK, Cancel).

---

## 🎯 5. Gameplay Foundation (`Scripts/Gameplay/`)

### 🔹 `GameplayController`
- Ready-to-extend starter controller.
- `GameEvents.OnGameStateChanged` ko listen karta hai.
- `GameState.InGame` hone par `StartSession()` trigger karta hai.
- `AddScore(int points)` score update aur high-score check handle karta hai.
- Session end ya player defeat hone par `TriggerGameOver()` call karta hai.

---

## 🚀 6. Quick Start: New Game Kaise Banayein?

### A. New Manager / Service Add Karna
1. Apna manager class banayein:
   ```csharp
   public class InventoryManager : Singleton<InventoryManager>
   {
       public void Init() => DependencyManager.Instance.Register(this);
   }
   ```
2. `GameManager.BootSequenceRoutine()` me register/initialize karein.

### B. New UI Panel Add Karna
1. Prefab banayein jisme `CanvasGroup` ho.
2. Script banayein jo `UIBasePanel` ko inherit kare:
   ```csharp
   public class GameOverPanel : UIBasePanel
   {
       [SerializeField] private Button restartBtn;
       public override void Init(UIManager manager)
       {
           base.Init(manager);
           restartBtn.onClick.AddListener(() => GameManager.Instance.RestartGame());
       }
   }
   ```
3. Canvas ke andar is panel ko attach karein, `UIManager` ise auto-detect kar lega!

### C. Sound Play Karna
- Direct: `AudioManager.Instance.PlaySFX("JumpSound");`
- Decoupled (Recommended): `GameEvents.OnPlaySFX?.Invoke("JumpSound");`

### D. Score Add Karna
- `GameplayController` me `AddScore(10);` call karein jo `GameEvents.OnScoreChanged` dispatch karega.

---

## 📋 Current Compilation Status
- **Unity Version:** Unity 6 (`6000.0.67f1`)
- **Compile Errors:** **0 Errors** (Clean build)
- **External Dependencies:** Only clean standard Unity UGUI, Input System, URP 2D, and DOTween (Clean configured).
