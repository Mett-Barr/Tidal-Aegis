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

所有武器资源通过 `WeaponAssetGenerator` 和 `ShipBuilder` 自动生成。

- **WeaponStatsSO**: 定义武器数据（射程、伤害、转速等）。
- **Prefabs**: 自动生成包含正确层级的 Prefab。
- **VFX**: 独立的 VFX 系统（详见 [VFX_SYSTEM.md](VFX_SYSTEM.md)）。

## 4. 常见武器类型

| 类型 | 旋转逻辑 | 备注 |
|------|----------|------|
| **FlagshipGun** | 2-Axis | 标准主炮，厚重手感 (15°/s) |
| **CIWS** | 2-Axis | 高射速，独立俯仰，极速 (120°/s) |
| **Autocannon** | 2-Axis | 灵敏手感 (80°/s) |
| **Missile (VLS)** | None | 垂直发射，不需要旋转 |
| **Torpedo** | 1-Axis | 通常只有水平旋转 |

## 5. 高级预判瞄準系统 (Advanced Aiming System)

為了最大化對移動目標的命中率，系統引入了 **Advanced Predictive Aiming** 算法。

### 5.1 核心算法：二階攔截 (Iterative Intercept)
不同於簡單的線性前置量 (Linear Lead)，本系統同時計算 **飛行時間 ($T_{flight}$)** 與 **炮塔旋轉時間 ($T_{turn}$)**。

1.  **初始猜測**: $T_{total} = T_{flight}$ (對準當前目標位置)。
2.  **迭代求解** (4次):
    - 預測目標在 $T_{total}$ 後的位置。
    - 計算炮塔轉到該角度所需時間 $T_{turn}$。
    - 計算彈丸飛到該距離所需時間 $T_{flight}$。
    - 更新 $T_{total} = T_{turn} + T_{flight}$。
3.  **結果**: 炮塔會直接轉向目標的**未來位置**，消除旋轉滯後。

### 5.2 雙精度彈道 (Double Precision Ballistics)
針對高初速武器 (如 CIWS, $V=1100m/s$)，採用 **`double` 精度** 進行彈道解算。
- **問題**: 在 `float` 精度下，高初速導致 $V^4$ 極大 ($10^{12}$)，與重力項相減時產生截斷誤差，導致仰角計算為 0。
- **解決**: 使用 `double` 進行中間計算，確保即使在 2.5km 極限射程下也能精確計算出微小的仰角 (Ballistic Arc)。

### 5.3 配置標準
- **GravityMultiplier**: 統一為 **1.0** (物理真實)。
- **RotationAcceleration**: 暫時禁用 (使用線性旋轉以保證最高精度)。

---
*最后更新: 2025-11-26*
