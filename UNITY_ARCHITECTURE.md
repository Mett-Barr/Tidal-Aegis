# Tidal Aegis - Project Architecture Documentation

## 1. Project Structure
The project follows a standard Unity architecture, grouping user content under `Assets/_Project` to separate it from third-party assets.

```text
Assets/
├── _Project/
│   ├── Art/                # Visual Assets
│   │   ├── Materials/
│   │   ├── Models/
│   │   ├── Shaders/
│   │   └── Textures/
│   ├── Audio/              # Audio Assets
│   ├── Data/               # ScriptableObject Data Instances
│   ├── Prefabs/            # Game Object Prefabs
│   │   ├── Enemies/
│   │   ├── Projectiles/
│   │   └── Weapons/
│   ├── Scenes/             # Game Scenes
│   ├── Scripts/            # C# Source Code
│   └── Settings/           # Project Settings & Pipeline Assets
```

## 2. Code Architecture (`Assets/_Project/Scripts`)
The codebase is organized by domain and responsibility.

### `Core`
Central systems that manage the game loop and global state.
- **GameManager**: Singleton. Manages game state (Playing, Paused, GameOver), player flagship reference, and global events.
- **Interfaces**: `IDamageable` (Unified interface for damage handling).

### `Data`
Data definitions (ScriptableObjects).
- **WeaponStatsSO**: Defines weapon properties (Range, Cooldown, Damage, ProjectilePrefab).
- **SquadronTemplateSO**: Defines squadron composition.

### `Entities`
Game objects with physical presence and behavior.
- **Units**:
    - `BaseUnit`: Abstract base for all ships. Handles HP and Rigidbody.
    - `FlagshipController`: Player-controlled ship.
    - `EnemyUnit`: AI-controlled enemies.
    - `KamikazeController`: Enemy that rams the player.
    - `SquadronUnit`: Friendly AI following the flagship.
- **Components**:
    - `WeaponController`: Handles targeting and firing.
        - **Targeting Logic**: Prioritizes the **nearest** enemy within range.
        - **Aiming**: Uses **predictive aiming** (quadratic intercept) to lead moving targets.
- **Projectiles**:
    - `ProjectileBehavior`: Handles movement (ballistic/linear) and collision.

### `Systems`
Pure logic or manager classes that operate on data or collections of entities.
- **SpawningSystem**: Manages enemy waves and spawn logic.
- **CameraSystem**: RTS-style camera control (Follow, Zoom, Shake).
- **SpatialGridSystem**: Spatial partitioning for efficient target queries.

### `UI`
User Interface logic.
- **HUDManager**: Displays HP, Score, etc.
- **TacticalOverlay**: World-space UI elements (Range rings).

### `Utils`
Helper classes and extensions.

## 3. Key Systems Detail

### Targeting System
The `WeaponController` uses the `SpatialGridSystem` to efficiently find potential targets.
1.  **Query**: Requests targets within range from the Spatial Grid.
2.  **Filter**: Selects the **nearest** valid target.
3.  **Prediction**: Calculates the intercept point based on target velocity and projectile speed to ensure high accuracy against moving enemies.

### Spawning System
The `SpawningSystem` handles procedural enemy generation.
- Spawns enemies at a randomized distance from the player.
- Increases difficulty over time (or based on wave count).

## 4. Future Considerations
- **Object Pooling**: Ensure `PoolManager` is utilized for all projectiles and temporary effects to minimize GC.
- **Event Bus**: Consider decoupling systems further using a global event bus for complex interactions.
