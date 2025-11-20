# Unity Migration Architecture Blueprint (v2.0)

## 1. Overview & Philosophy
This document serves as the technical bridge between the `GAME_DESIGN_SPEC.md` (Logic/Math) and the Unity Engine implementation.
*   **Goal**: Port the React/Three.js prototype to a high-performance Unity architecture.
*   **Language**: C# (Strict Typing).
*   **Localization**: All user-facing text (UI, Logs, Names) must be in **Traditional Chinese (繁體中文)**.

---

## 2. Folder Structure & Namespace Mapping

```text
Assets/
├── Scripts/ (Namespace: NavalCommand)
│   ├── Core/               # Singleton Managers, Game Loop, Global Events
│   ├── Data/               # ScriptableObjects (Stats, Configs)
│   ├── Entities/           # Base Classes, Units, Components
│   ├── Systems/            # Pure Logic Systems (Spatial, Spawning)
│   ├── Infrastructure/     # Optimization (Pooling, Input Wrappers)
│   ├── UI/                 # View Layer (HUD, Menus)
│   └── Utils/              # Extensions, Math Helpers
├── Prefabs/
│   ├── Units/
│   ├── Projectiles/
│   └── UI/
└── Resources/              # Dynamic Loading (if necessary)
```

---

## 3. Data Layer (ScriptableObjects)
*Maps `constants.ts` to Unity Assets.*

### `NavalCommand.Data`

#### `WeaponStatsSO`
*   **Purpose**: Configuration for weapons.
*   **Fields**:
    *   `string DisplayName`: UI Name (e.g., "海軍艦炮").
    *   `float Range`, `float Cooldown`, `float Damage`, `float ProjectileSpeed`.
    *   `TargetLayer TargetMask`: Enum (Surface, Air).
    *   `GameObject ProjectilePrefab`: Reference to the visual prefab.

#### `SquadronTemplateSO`
*   **Purpose**: Configuration for squadron types.
*   **Fields**:
    *   `string SquadronName`: UI Name (e.g., "導彈突擊隊").
    *   `string Description`: UI Description.
    *   `int UnitCount`.
    *   `GameObject UnitPrefab`.
    *   `WeaponStatsSO WeaponConfig`.

---

## 4. Core Architecture (The Backbone)

### `NavalCommand.Core`

#### `GameManager` (Singleton)
*   **Role**: Central coordinator.
*   **Responsibilities**:
    *   Maintain `GameState` (Intro, Playing, Paused, Ended).
    *   Reference `PlayerFlagship` (The main player object).
    *   Handle Level Up logic (Pause time, Show UI).

#### `EventManager` (Static / Singleton)
*   **Role**: Decouples UI from Logic. **[CRITICAL for modularity]**
*   **Events**:
    *   `public static event Action<int> OnScoreChanged;`
    *   `public static event Action<float, float> OnFlagshipHPUpdate;` // Current, Max
    *   `public static event Action<int, float> OnXPGained;` // Level, Progress%
    *   `public static event Action OnLevelUpAvailable;`
    *   `public static event Action<bool> OnPauseToggled;`

---

## 5. Infrastructure & Optimization (The "Missing Specs")

### `NavalCommand.Infrastructure`

#### `PoolManager` (Singleton)
*   **Role**: Prevents GC spikes by reusing objects.
*   **Requirement**: DO NOT use `Instantiate/Destroy` at runtime for projectiles/enemies.
*   **API**:
    *   `T Spawn<T>(T prefab, Vector3 pos, Quaternion rot)`
    *   `void Despawn(GameObject obj)`
*   **Implementation**: Use a Dictionary of Queues `Dictionary<int, Queue<GameObject>>` keyed by Prefab Instance ID.

#### `InputReader` (ScriptableObject or Monobehaviour)
*   **Role**: Abstracts input Hardware.
*   **Requirement**: Support easy switching between Legacy Input and New Input System.
*   **API**:
    *   `Vector2 MoveDirection`: Returns normalized Vector2.
    *   `bool IsFiring`: Returns true if fire button held.
    *   `float ZoomDelta`: Returns scroll wheel value.
    *   `event Action OnPausePressed;`

### `NavalCommand.Systems`

#### `SpatialGridSystem`
*   **Role**: Optimization for Targeting ($O(N) \to O(1)$).
*   **Logic**:
    *   Divides the world into buckets (e.g., 50x50 units).
    *   Units register themselves to a bucket.
    *   `WeaponController` queries only adjacent buckets for targets.
*   **API**:
    *   `List<IDamageable> GetTargetsInRange(Vector3 origin, float range, TargetLayer layer)`

---

## 6. Entity Layer (MonoBehaviours)

### `NavalCommand.Entities`

#### `BaseUnit` (Abstract)
*   **Components**: `Rigidbody`, `Collider`.
*   **Implements**: `IDamageable`.
*   **Logic**:
    *   `TakeDamage(float amount)`: Deduct HP.
    *   `Die()`: Trigger particles, Score event, and call `PoolManager.Despawn()`.

#### `FlagshipController` : `BaseUnit`
*   **Logic**:
    *   Reads `InputReader.MoveDirection`.
    *   Physics-based movement (AddForce).
    *   Manages multiple `WeaponController` children (Omni-weapon).

#### `SquadronUnit` : `BaseUnit`
*   **Logic**:
    *   **Formation**: Calculates target position based on Flagship's transform.
    *   **Boid Physics**:
        *   *Separation*: Query `SpatialGrid` for neighbors to avoid overlap.
        *   *Arrival*: Spring force towards formation slot.

#### `EnemyUnit` : `BaseUnit`
*   **Logic**:
    *   **AI State**: Chase -> Attack -> Orbit.
    *   **Targeting**: Priority is usually Flagship, but uses `SpatialGrid` to check collision avoidance.

### `NavalCommand.Entities.Components`

#### `WeaponController`
*   **Role**: Attached to Turrets/Hardpoints.
*   **Logic**:
    *   `Update()`: Count down cooldown.
    *   `FindTarget()`: Use `SpatialGridSystem` to find nearest valid target.
    *   `Fire()`: Request projectile from `PoolManager`.

---

## 7. Projectiles & VFX

### `NavalCommand.Entities.Projectiles`

#### `ProjectileBase`
*   **Role**: Handles movement and collision.
*   **Logic**:
    *   **Ballistic**: `transform.position += velocity * dt; velocity.y -= gravity * dt;`
    *   **Homing**: `transform.rotation = Quaternion.RotateTowards(...)`
    *   **OnCollision**: 
        *   Check `IDamageable`.
        *   Spawn Hit VFX via `PoolManager`.
        *   Despawn self.

---

## 8. UI Layer (View)

### `NavalCommand.UI`

#### `HUDController`
*   **Role**: Passive view, listens to `EventManager`.
*   **Elements**:
    *   HP Bar (Filled Image).
    *   Score Text (TMP).
    *   Squadron List (Horizontal Layout Group).
*   **Localization**: Ensure labels like "HP", "Level", "Score" are localized or iconic.

#### `DamageNumberSystem`
*   **Role**: Floating combat text.
*   **Implementation**: Use `PoolManager` to spawn WorldSpace UI Canvas text elements at hit position.

---

## 9. Implementation Checklist (Step-by-Step)

1.  **Setup Core**: Create `GameManager`, `PoolManager`, `EventManager`.
2.  **Input**: Setup `InputReader`.
3.  **Player**: Implement `FlagshipController` + `Rigidbody` movement.
4.  **Camera**: Implement Top-Down Camera following Flagship.
5.  **Combat (Basic)**: Implement `WeaponController` firing dummy objects.
6.  **Pooling**: Connect Weapon firing to `PoolManager`.
7.  **Enemies**: Create `EnemyUnit` chasing player.
8.  **Optimization**: Implement `SpatialGridSystem` and refactor targeting to use it.
9.  **UI**: Bind HUD to Events.
