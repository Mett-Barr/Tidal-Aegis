# Unity Migration Architecture Blueprint (v1.0)

## 1. High-Level Folder & Namespace Structure
This structure defines the mapping between Unity `Assets/Scripts` paths and C# Namespaces.

```text
Assets/Scripts/
├── Core/               # Core systems (Singleton, Managers, Global State)
├── Data/               # Static Data Definitions (ScriptableObjects)
├── Entities/           # Entity Behaviors (MonoBehaviours)
│   ├── Units/          # Ship Units (Flagship, Squadrons, Enemies)
│   └── Projectiles/    # Projectile Logic
├── Systems/            # Logic Systems (Non-Monobehaviour or Pure Logic)
├── UI/                 # User Interface Logic
└── Utilities/          # Helper Classes & Extensions
```

---

## 2. Data Layer (ScriptableObjects)
Maps the `constants.ts` and `types.ts` from the web prototype into Unity's data-driven assets.

### `Package: NavalCommand.Data`

#### Class: `WeaponStatsSO` (Inherits: ScriptableObject)
*   **Responsibility**: Defines static properties for a specific weapon type.
*   **Properties**:
    *   `string DisplayName`: The name shown in UI [Traditional Chinese] (e.g., "海軍艦炮").
    *   `WeaponType Type`: Enum (FlagshipGun, Missile, etc.).
    *   `float Range`: Attack range.
    *   `float Cooldown`: Fire interval (seconds).
    *   `float Damage`: Damage per hit.
    *   `GameObject ProjectilePrefab`: Reference to the projectile prefab.
    *   `TargetLayer TargetMask`: Mask defining valid targets (Air/Surface).

#### Class: `SquadronTemplateSO` (Inherits: ScriptableObject)
*   **Responsibility**: Defines the composition and stats of a squadron.
*   **Properties**:
    *   `string Name`: Squadron display name [Traditional Chinese] (e.g., "炮艇中隊").
    *   `string Description`: Flavor text description [Traditional Chinese].
    *   `WeaponType WeaponConfig`: The weapon equipped by this squadron.
    *   `WeightClass Weight`: Physics weight class (Heavy/Medium/Light).
    *   `int UnitCount`: Number of units in this squadron.
    *   `GameObject UnitPrefab`: Reference to the ship unit prefab.

---

## 3. Core System (Architecture Backbone)
Handles the main game loop and state management.

### `Package: NavalCommand.Core`

#### Class: `GameManager` (Singleton)
*   **Responsibility**: Coordinates game state, Pause/Resume, and Win/Loss conditions.
*   **Dependencies**: `LevelingSystem`, `SpawningSystem`, `UIManager`.
*   **Properties**:
    *   `GameState CurrentState`: Enum (Playing, Paused, GameOver).
    *   `FlagshipController PlayerFlagship`: Reference to the player's flagship.
*   **Methods**:
    *   `RegisterUnit(UnitController unit)`: Adds unit to a global spatial hash grid or list for optimized targeting.
    *   `UnregisterUnit(UnitController unit)`: Cleanup on death.
    *   `TogglePause()`: Toggles `Time.timeScale` and notifies UI.

#### Interface: `IDamageable`
*   **Responsibility**: Unified interface for all damageable objects.
*   **Methods**:
    *   `void TakeDamage(float amount)`: Subtracts HP.
    *   `bool IsDead()`: Checks survival status.
    *   `Team GetTeam()`: Distinguishes between Player and Enemy.

---

## 4. Entity Layer (MonoBehaviours)
Maps to `game/physics/movement.ts` and `renderers/models/*.ts`.

### `Package: NavalCommand.Entities.Units`

#### Abstract Class: `BaseUnit` (Inherits: MonoBehaviour, Implements: IDamageable)
*   **Responsibility**: Handles generic movement physics (Rigidbody), HP, and Death logic.
*   **Properties**:
    *   `float CurrentHP`
    *   `Rigidbody Rb`: Reference to Unity Physics component.
*   **Abstract Methods**:
    *   `void Move()`: Movement logic to be implemented by subclasses.

#### Class: `FlagshipController` (Inherits: BaseUnit)
*   **Responsibility**: The core unit controlled by the player.
*   **Dependencies**: `InputSystem`.
*   **Behaviors**:
    *   **Movement**: Very slow, inertial movement. Acts as the "Anchor" for the entire fleet.
    *   **Weapons**: Hosts multiple `WeaponController` components (Omni-weapon system).

#### Class: `SquadronUnit` (Inherits: BaseUnit)
*   **Responsibility**: AI units that follow the flagship in formation.
*   **Dependencies**: `FlagshipController`.
*   **Properties**:
    *   `Vector2 FormationOffset`: Local coordinates relative to the Flagship.
*   **Behaviors**:
    *   **Update Loop**: Calculates target world position every frame (`Flagship.Position + Rotation * Offset`).
    *   **Physics**: Applies **Boid Separation** (force to avoid neighbors) and **Spring Force** (force to reach target position).

#### Class: `EnemyUnit` (Inherits: BaseUnit)
*   **Responsibility**: Enemy AI behavior.
*   **Behaviors**:
    *   **AI State Machine**: Chase -> Engage (within range) -> Orbit/Stop.
    *   **Elite Logic**: Elite units (identified by `IsElite` flag) prioritize the Flagship and have different movement parameters.

---

## 5. Combat System
Maps to `game/combat.ts`.

### `Package: NavalCommand.Entities.Components`

#### Class: `WeaponController` (Component)
*   **Responsibility**: Attached to ships; handles cooldowns and targeting.
*   **Data**: References `WeaponStatsSO`.
*   **Methods**:
    *   `FindTarget()`: Scans `GameManager` unit lists based on `TargetLayer` to find the nearest/best target.
    *   `Fire()`: Instantiates a Projectile and initializes it.
    *   `CheckFiringArc()`: Ensures the target is within the turret's rotational limits.

### `Package: NavalCommand.Entities.Projectiles`

#### Class: `ProjectileBehavior` (Inherits: MonoBehaviour)
*   **Responsibility**: Movement logic and collision detection for shots.
*   **Properties**:
    *   `ProjectileType BehaviorType`: Enum (Ballistic, Homing, Straight).
    *   `Transform Target`: Locked target (required for Missiles/Torpedoes).
*   **Behaviors**:
    *   `OnCollisionEnter()`: Triggers `target.TakeDamage()` and spawns VFX.
    *   `Update()`:
        *   If `Ballistic`: Apply gravity to simulated Z-axis (Mapped to Unity Y-axis).
        *   If `Homing`: Apply `RotateTowards` logic (Top-attack logic for missiles).

---

## 6. Systems (Pure Logic / Managers)

### `Package: NavalCommand.Systems`

#### Class: `SpawningSystem`
*   **Responsibility**: Manages enemy waves.
*   **Logic**:
    *   Monitors `Time.time`.
    *   Calculates spawn probability based on `DifficultyParams` passed from Core.
    *   `Instantiate` enemy Prefabs at random positions outside the Flagship's radius R.

#### Class: `CameraSystem`
*   **Responsibility**: Controls the RTS camera view.
*   **Logic**:
    *   Smoothly follows the `Flagship`.
    *   Handles Mouse Wheel Zoom (Adjusting FOV or Y-height).
    *   Handles Screen Shake effects on impact.

---

## 7. UI Layer
Maps to React Components.

### `Package: NavalCommand.UI`

#### Class: `HUDManager`
*   **Responsibility**: Displays HP, Score, and Squadron status.
*   **Localization Note**: All UI labels must use [Traditional Chinese].
    *   Example: "Flagship Status" -> "旗艦狀態".
    *   Example: "Score" -> "分數".
*   **Dependencies**: Subscribes to `GameManager` events (`OnScoreChanged`, `OnHPChanged`).

#### Class: `TacticalOverlay`
*   **Responsibility**: Draws tactical info in World Space.
*   **Implementation**:
    *   Uses Unity `LineRenderer` for range rings.
    *   Uses `WorldSpace Canvas` for Squadron Numbers.

#### Class: `UpgradePanel`
*   **Responsibility**: Displays Level Up options.
*   **Localization Note**: Upgrade titles and descriptions must use [Traditional Chinese].

---

## 8. Migration Steps (Execution Plan)

1.  **Asset Setup**: Create the folder structure and `WeaponStatsSO` to define all weapon data.
2.  **Core Implementation**: Implement `GameManager` and `BaseUnit` abstract class.
3.  **Physics Porting**: Port the Boid algorithms from `movement.ts` into `SquadronUnit.FixedUpdate()`.
4.  **Visuals**: Import 3D models and configure Prefabs.
5.  **Combat**: Implement `WeaponController` and `ProjectileBehavior`, replacing the logic from `game/combat.ts`.
6.  **UI Integration**: Rebuild the HUD using Unity UI Toolkit, ensuring all text is localized to [Traditional Chinese].
