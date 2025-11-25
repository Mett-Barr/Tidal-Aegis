# 武器系统架构 (Weapon System Architecture)

## 1. 核心组件

### WeaponController
负责武器的高层逻辑：
- 目标索敌 (`FindTarget`)
- 火控解算 (`CalculateFireSolution`)
- 射击控制 (`Fire`)
- 冷却管理

### TurretRotator (新)
负责炮塔的物理旋转，支持双轴独立控制：
- **Azimuth (Yaw)**: 水平旋转（底座）
- **Elevation (Pitch)**: 垂直俯仰（炮身）
- **自动识别**: 会自动在层级中寻找 `TurretBase` 和 `TurretGun`。

## 2. 炮塔架构标准 (Turret Architecture Standard)

### 2.1 几何结构 (Geometry)
为了消除视差 (Parallax Error) 并确保瞄准精度，所有炮塔模型必须遵循 **"轴线对齐 (Axis-Aligned)"** 原则：

1.  **TurretBase (Yaw Pivot)**:
    - 负责水平旋转。
    - 通常位于甲板平面 (Y=0)。
2.  **TurretGun (Pitch Pivot)**:
    - 负责垂直俯仰。
    - **关键规则**: 其位置必须与**炮管轴线 (Barrel Axis)** 高度一致。
    - 也就是说，`Barrels` 和 `FirePoint` 相对于 `TurretGun` 的局部 Y 坐标必须为 **0**。
3.  **FirePoint**:
    - 必须是 `TurretGun` 的子物体（支持深层嵌套）。
    - 必须位于炮管末端。
    - 局部旋转应为 Identity (0,0,0)，即沿 Z 轴发射。

### 2.2 瞄准逻辑 (Aiming Logic)
采用 **"以转轴为中心 (Pivot-Centric)"** 的瞄准策略 (Option A+)：

- **AimAt**: 旋转 `TurretBase` 和 `TurretGun`，使其 Z 轴指向目标。
- **IsAligned**: 检查 **`TurretGun` (转轴)** 的朝向是否与目标一致，**而不是**检查 `FirePoint` (枪口)。
    - *原因*: 枪口通常有物理偏移（如位于转轴前方），在近距离会产生几何视差。如果检查枪口，会导致"炮塔转对了，但枪口判定未对准"的死锁。
    - *结论*: 只要炮塔转轴对准了目标，就允许开火。

### 2.3 俯仰限制 (Pitch Limits)
- **CIWS / Autocannon**:
    - `MinPitch`: **-30° ~ -45°** (允许向下攻击贴水面目标，如神风船)。
    - `MaxPitch`: **89°** (允许垂直向上攻击高空导弹)。
- **Main Gun**:
    - `MinPitch`: -10°
    - `MaxPitch`: 60°

## 3. 资源生成 (Asset Generation)
`ShipAssetGenerator` 和 `ShipBuilder` 已更新以符合上述标准：
- 自动生成包含 `TurretBase` -> `TurretGun` -> `FirePoint` 的层级。
- 自动修正 CIWS 的 Pivot 高度以消除视差。
- 递归搜索 `FirePoint` 以支持复杂模型结构。

## 4. 常见武器类型配置

## 3. 资源生成 (Asset Generation)

所有武器资源通过 `WeaponAssetGenerator` 和 `ShipBuilder` 自动生成。

- **WeaponStatsSO**: 定义武器数据（射程、伤害、转速等）。
- **Prefabs**: 自动生成包含正确层级的 Prefab。
- **VFX**: 独立的 VFX 系统（详见 [VFX_SYSTEM.md](VFX_SYSTEM.md)）。

## 4. 常见武器类型

| 类型 | 旋转逻辑 | 备注 |
|------|----------|------|
| **FlagshipGun** | 2-Axis | 标准主炮 |
| **CIWS** | 2-Axis | 高射速，独立俯仰 |
| **Missile (VLS)** | None | 垂直发射，不需要旋转 |
| **Torpedo** | 1-Axis | 通常只有水平旋转 |

---
*最后更新: 2025-11-26*
