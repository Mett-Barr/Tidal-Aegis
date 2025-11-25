# 飞弹系统架构 (Missile System Architecture)

## 1. 三段式飞行路径 (Three-Phase Flight Profile)

导弹采用标准的**垂直发射 → 水平巡航 → 俯冲攻顶 (VLS → Cruise → Top-Attack)** 轨迹。

### Phase 0: 垂直发射 (Vertical Launch)
- **触发条件**: 初始发射，当前高度 < `VerticalLaunchHeight` (20m)
- **行为**: 强制向上飞行 (`desiredDir = Vector3.up`)
- **转换条件**: 高度 ≥ 20m → 进入 Phase 1

### Phase 1: 水平巡航 (Cruise - True Horizontal Flight)
- **核心设计**: **锁定俯仰角为 0°**，只在 XZ 平面导航
- **关键实现**:
  ```csharp
  cruiseDir.y = 0f; // 强制水平
  dirToTarget.y = 0f; // 忽略目标高度差
  ```
- **转向速度**: 300°/s (极度灵敏，可快速对准目标方位)
- **转换条件**: **水平距离** < `TerminalHomingDistance` (150m) → 进入 Phase 2

### Phase 2: 终端俯冲 (Terminal Dive & Homing)
- **导航逻辑**: 完整 3D 预测性追踪 (Predictive Pursuit)
- **转向速度**: 240°/s (足够快速完成 90° 俯冲)
- **预测算法**: `predictedPos = targetPos + targetVel × timeToImpact`

---

## 2. 关键设计决策

### 2.1 为什么巡航阶段要"锁定水平"？

**❌ 错误做法** (旧版本):
```csharp
float heightError = cruiseHeight - currentPos.y;
cruiseDir.y = Mathf.Clamp(heightError * 0.5f, -0.5f, 0.5f);
```
**问题**: 导弹会不断调整俯仰角来"维持"巡航高度，造成持续的**上下振荡 (Oscillation)**，形成"蛇形"轨迹。

**✅ 正确做法** (当前版本):
```csharp
cruiseDir.y = 0f; // 完全锁定水平
```
**效果**: 导弹在完成垂直爬升后，立即转为**完美水平飞行**，不再调整高度。保持当前高度（约 20m）水平巡航，直到进入终端距离。

### 2.2 为什么用"水平距离"判断终端？

**❌ 错误做法**:
```csharp
float distToTarget = Vector3.Distance(currentPos, targetPos); // 3D 距离
if (distToTarget < terminalDist) { phase = 2; }
```
**问题**: 当导弹在高空 (20m)，目标在海面 (0m) 时，即使水平距离只有 30m，3D 距离也会达到 `√(30² + 20²) ≈ 36m`。如果 `terminalDist = 50m`，看似应该触发，但实际高度差会延迟触发时机，导致导弹**飞过目标上空**才开始俯冲。

**✅ 正确做法**:
```csharp
Vector3 currentPosXZ = new Vector3(currentPos.x, 0, currentPos.z);
Vector3 targetPosXZ = new Vector3(targetPos.x, 0, targetPos.z);
float horizontalDist = Vector3.Distance(currentPosXZ, targetPosXZ);
if (horizontalDist < terminalDist) { phase = 2; }
```
**效果**: 忽略高度差，只看水平距离。导弹会在**正确的水平位置**（距离目标水平 150m）开始俯冲，无论高度差多少。

### 2.3 终端距离的计算 (Terminal Distance Calculation)

**公式**:
```
转向角度 (θ) = 90° (从水平到垂直)
转向速度 (ω) = 240°/s
所需时间 (T) = θ / ω = 90 / 240 = 0.375 秒
飞行距离 (D) = V × T = 290 m/s × 0.375s = 109 米
安全裕度 = 150 - 109 = 41 米
```

**配置值**: `TerminalHomingDistance = 150m`
- **最小距离**: 109m (理论值)
- **实际设定**: 150m (36% 安全裕度)

---

## 3. 当前配置参数

| 参数 | 值 | 说明 |
|------|-----|------|
| `VerticalLaunchHeight` | 20m | 垂直爬升目标高度 |
| `CruiseHeight` | 15m | (已废弃) 巡航阶段不再维持高度 |
| `TerminalHomingDistance` | 150m | 水平距离触发终端俯冲 |
| `CruiseTurnRate` | 300°/s | 巡航转向速度 |
| `TerminalTurnRate` | 240°/s | 终端转向速度 |
| `ProjectileSpeed` | 290 m/s | 导弹飞行速度 |

---

## 4. 常见问题排查

### Q: 导弹在巡航阶段上下晃动？
**A**: 检查 `MovementFunctions.GuidedMissile` Phase 1 代码，确保 `cruiseDir.y = 0f;` 而不是基于高度差计算。

### Q: 导弹飞过目标才开始俯冲？
**A**: 检查终端判断是否使用**水平距离**而非 3D 距离。

### Q: 导弹俯冲转向太慢？
**A**: 增加 `terminalTurnRate` 或减少 `TerminalHomingDistance`。公式: `T = θ / ω`, `D = V × T`。

### Q: 短距离测试看不到巡航阶段？
**A**: `TerminalHomingDistance` 过大。对于 300m 测试场景，建议设为 150m，可观察到完整三段轨迹。

---

*最后更新: 2025-11-26*
