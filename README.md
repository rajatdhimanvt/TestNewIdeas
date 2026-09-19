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
│       ├── Data/                 # ScriptableObjects definition
│       │   ├── BallDataSO.cs     # Tier properties (size, color, score)
│       │   └── LevelDataSO.cs    # Level bounds, tiers pool, drop rules
│       ├── Managers/             # Central game controllers & services
│       │   ├── DependencyManager.cs # Service locator / DI container
│       │   ├── GameManager.cs       # Master game lifecycle coordinator
│       │   ├── AudioManager.cs      # BGM & SFX playback + volume management
│       │   ├── PlayerDataManager.cs # Coins, Level Stars & Shop persistence
│       │   ├── BallKitManager.cs    # Active Ball Kit manager
│       │   └── ThemeManager.cs      # Active Environment Theme manager
│       ├── Gameplay/             # Ball Merge 2D physics mechanics
│       │   ├── Ball.cs              # Physics entity & collision merge trigger
│       │   ├── BallDropper.cs       # Horizontal aiming & drop release
│       │   ├── BallMergeManager.cs  # Midpoint merge & pop scale spawner
│       │   ├── ContainerBoundary.cs # 3 physical box colliders (walls + floor)
│       │   └── GameplayController.cs# Session loop & score tracking
│       ├── UiScripts/            # UI Panel stack & screens
│       │   ├── UIBasePanel.cs       # Base animated panel class
│       │   ├── UIManager.cs         # Panel navigation, popups & overlays
│       │   ├── MainMenuPanel.cs     # Main menu screen
│       │   ├── LevelSelectionPanel.cs# 50-Level Campaign Grid Map
│       │   ├── DailyRewardPanel.cs  # 7-Day Login Streak & Physics Spin Wheel
│       │   ├── BallKitShopPanel.cs  # Ball Kits Shop screen
│       │   ├── BallKitPreviewPanel.cs# Ball Kit Tiers Inspection Modal
│       │   ├── ThemeShopPanel.cs    # Environment Theme Shop screen
│       │   ├── ThemePreviewPanel.cs  # Environment Theme Live Preview Modal
│       │   ├── SettingsPanel.cs     # Audio & volume controls
│       │   └── GenericPopupPanel.cs # Reusable alert/confirm modal dialog
│       ├── Editor/               # Unity Editor tools
│       │   └── BallMergeAssetCreator.cs # 1-click default scene & UI canvas generator
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

## 🎯 5. Ball Merge Gameplay System (`Scripts/Gameplay/` & `Scripts/Data/`)

Yeh Suika-style 2D Physics Ball Merge game mechanics ka core architecture hai:

### 🔹 1. Data-Driven ScriptableObjects (`Scripts/Data/`)
- **`BallDataSO`**: Har ball tier ka physical aur visual data hold karta hai:
  - `tier`: Tier index (0 = Cherry, 1 = Plum, 2 = Orange, 3 = Apple, 4 = Melon, 5 = Watermelon...)
  - `ballName`: Ball ka display name
  - `radius`: Visual scale aur physical collider radius
  - `ballColor`: Procedural color tint (custom sprite na hone par bhi har ball alag color me dikhti hai)
  - `scoreValue`: Merge hone par milne wale points
  - `mass`, `bounciness`, `friction`: Physics tuning
- **`LevelDataSO`**: Level rules aur setup define karta hai:
  - `allTiers`: Ordered list of all available tiers
  - `droppableTiers`: Subset of lower tiers jo player dropper se spawn ho sakte hain (e.g. Tier 0, 1, 2)
  - `dropCooldown`: Delay between drops
  - `containerWidth`, `containerHeight`, `wallThickness`, `dropHeight`
  - `GetNextTier(currentTier)`: Next higher tier resolve karta hai

### 🔹 2. Physical Container (`ContainerBoundary.cs`)
- Container ke **3 physical colliders** (Left Wall, Right Wall, Bottom Floor) generate karta hai.
- Dimensions `LevelDataSO` se dynamically bind hoti hain.
- Sprites ke through box visuals render karta hai.

### 🔹 3. Dropper & Spawner (`BallDropper.cs`)
- Container ke top par rehta hai.
- Pointer/Mouse input ko world coordinates me convert karke horizontal axis par follow karta hai.
- Aim position ko container walls ke andar clamp karta hai taaki ball bahar na jaye.
- Click/Tap release par ball ko physics gravity me drop karta hai (`Drop()`).
- Cooldown timer ke baad next ball automatically load karta hai.

### 🔹 4. Ball Entity (`Ball.cs`)
- `CircleCollider2D`, `Rigidbody2D` (Continuous 2D physics), aur `SpriteRenderer` se equipped.
- Procedural circle texture fallback built-in hai (bina kisi external art ke bhi perfect circular ball render hoti hai).
- **Atomic Merge Collision Check**: Jab do identical tier ki balls takrati hain, `GetInstanceID()` check se single merge lock lagta hai taaki duplicate event trigger na ho, aur `BallMergeManager` ko call karta hai.

### 🔹 5. Merge Manager (`BallMergeManager.cs`)
- Do identical balls ko collision midpoint par shrink animate karta hai.
- Midpoint position par **Next Tier ki Ball** spawn karta hai.
- DOTween `SetEase(Ease.OutBack)` ke sath juicy pop-scale animation play karta hai.
- `GameEvents.OnScoreChanged` aur `GameEvents.OnPlaySFX("Merge")` trigger karta hai.

### 🔹 6. Level Asset Generator Tool (`Scripts/Editor/BallMergeAssetCreator.cs`)
- Ek click me 6 tiers ki `BallDataSO`, Ball Kits, Themes, aur full Scene & Canvas visual hierarchy generate karne ka Editor tool:
  👉 Unity Top Menu: **`Tools -> Ball Merge -> 2. Auto Setup Complete Game Scene`**

---

## 🛍️ 6. Customization & Retention Systems (Play Store Release Ready)

### 🔹 1. In-Game Coin Economy & Persistence (`PlayerDataManager.cs`)
- Player ke Coins balance, Level Stars, High Scores, unlocked Kits/Themes, aur Daily Streak claim timestamps ka central persistence manager.
- Merges karne par (`+1` to `+10` coins), Levels clear karne par (`+50` to `+150` coins), aur Daily Wheel spin se coins earn hote hain.
- Coins se Shop mein locked Ball Kits aur Themes buy/unlock hoti hain.

### 🔹 2. Ball Kits & Inspection Modal (`BallKitManager.cs`, `BallKitShopPanel.cs`, `BallKitPreviewPanel.cs`)
- **Ball Kits**: Classic Fruits (Free), Ocean World (500 🪙), Sky High (1,000 🪙).
- **`🔍 VIEW TIERS` Modal**: Inspect button click karne par kit ke sabhi 6 ball tiers ki full spec sheet (Score, Mass, Radius, `✔ DROPPABLE` vs `★ MERGE ONLY` status) dikhati hai.

### 🔹 3. Environment Themes & Live Preview Modal (`ThemeManager.cs`, `ThemeShopPanel.cs`, `ThemePreviewPanel.cs`)
- **Themes**: Dark Cosmic (Free), Ocean Depth (300 🪙), Sky Breeze (600 🪙), Sunset Glow (1,000 🪙).
- **`🔍 PREVIEW` Modal**: Live mini gameplay box preview renders doing real-time background color, container wall colors, aur danger line warning colors rendering.

### 🔹 4. 50-Level Campaign Progression Map (`LevelSelectionPanel.cs`)
- **50-Level Grid**: Scrollable level selection map with unlocked/locked (`🔒`) states.
- **1-3 Star Ratings (`⭐`, `⭐⭐`, `⭐⭐⭐`)**: Level completion efficiency ke hisaab se stars calculate aur save hote hain.

### 🔹 5. Daily Rewards Calendar & Lucky Spin Wheel (`DailyRewardPanel.cs`)
- **7-Day Login Streak**: Har roz login karke 50 se 1000 coins tak claim kar sakte hain.
- **Physics Spin Wheel**: Smooth decelerating cubic ease-out rotation animation (`1 - (1-t)^3`) ke saath 8 reward slices (50, 100, 150, 200, 250, 300, 500, 1000 coins).

---

## 🚀 7. Quick Start: New Game Kaise Banayein?

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

## 📋 8. Current Compilation Status
- **Unity Version:** Unity 6 (`6000.0.67f1`)
- **Compile Errors:** **0 Errors** (Clean build)
- **External Dependencies:** Only clean standard Unity UGUI, Input System, URP 2D, and DOTween (Clean configured).
