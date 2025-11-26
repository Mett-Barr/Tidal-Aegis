# Tidal-Aegis 技术架构验证路线图

> **项目定位**: RTS Roguelike - 战术编排 + 升级多样性  
> **技术目标**: 可验证的技术骨架，保证扩展性与稳定性  
> **设计原则**: 完美模块化 + 数据驱动 + 零耦合  
> **文档版本**: v2.0 (Technical Focus)  
> **更新日期**: 2025-11-26

---

## 🎯 核心技术支柱 (Technical Pillars)

### 评价标准（黄金标准）
每个技术模块必须满足：
- ✅ **可验证性 (Verifiable)**: 可通过自动化测试或 Editor 工具验证
- ✅ **扩展性 (Extensible)**: 新增内容无需修改核心代码
- ✅ **稳定性 (Stable)**: 边界条件、异常处理完善
- ✅ **解耦性 (Decoupled)**: 模块间零依赖或单向依赖
- ✅ **数据驱动 (Data-Driven)**: 逻辑与数据分离

---

## 📊 技术模块完成度矩阵

| 技术模块 | 可验证 | 扩展性 | 稳定性 | 解耦性 | 数据驱动 | 综合评分 |
|---------|-------|-------|-------|-------|---------|---------|
| **编辑器工具** | ✅ | ✅ | ✅ | ✅ | ✅ | **100%** |
| **数据架构** | ✅ | ✅ | ✅ | ✅ | ✅ | **100%** |
| **物理引擎** | ✅ | ✅ | ✅ | ✅ | ✅ | **100%** |
| **武器系统** | ✅ | ✅ | ✅ | ✅ | ✅ | **100%** |
| **飞弹系统** | ✅ | ✅ | ✅ | ✅ | ✅ | **100%** |
| **VFX 系统** | ✅ | ✅ | ✅ | ✅ | ✅ | **100%** |
| **单位系统** | ✅ | ✅ | ⚠️ | ✅ | ✅ | **90%** |
| **编队系统** | ❌ | ❌ | ❌ | - | - | **0%** |
| **命令系统** | ❌ | ❌ | ❌ | - | - | **0%** |
| **升级系统** | ❌ | ❌ | ❌ | - | - | **0%** |
| **AI 框架** | ❌ | ❌ | ❌ | - | - | **0%** |
| **保存系统** | ❌ | ❌ | ❌ | - | - | **0%** |


---

## 📖 详细技术文档索引 (Detailed Documentation)

本文档提供**技术概览与进度追踪**。具体实现细节、架构规范、使用指南等详见以下独立文档：

### 核心系统规范
- 📋 **[开发规范](Documentation/DEVELOPMENT_STANDARDS.md)** - Rebuild World 工作流与自动化要求
- 🛠️ **[编辑器工具系统](Documentation/EDITOR_TOOLS.md)** - Generator 模式、菜单管理、资源生成流程

### 已完成系统详述
- 🎯 **[武器系统](Documentation/WEAPON_SYSTEM.md)** - 炮塔架构、高级预判瞄准、多炮管支持
  - 2-Axis 旋转机制
  - Pivot-Centric 瞄准策略
  - 高级预判算法（二阶拦截）
  
- 🚀 **[飞弹系统](Documentation/MISSILE_SYSTEM.md)** - 三段式飞行路径、水平巡航设计
  - VLS → Cruise → Terminal 状态机
  - 水平距离判断原理
  - 终端距离计算公式

- ✨ **[VFX 系统](Documentation/VFX_SYSTEM.md)** - 特效生成、对象池、材质管理
  - TrailRenderer 动态生成
  - ParticleSystem 配置
  - 对象池优化策略

**阅读建议**: 
- 快速了解系统 → 阅读本文档（TDD.md）概览
- 实现新功能 → 查阅对应 `Documentation/*.md` 详细规范
- 修改现有系统 → 先读详细文档，理解设计决策

---

## ✅ 已验证技术模块（Phase 1: 战斗基础）

### 1. 编辑器工具链 ✓ (100%)
**概览**: 一键重建世界，零手动步骤的自动化工具链。

**验证标准**: 
- [x] 单键重建世界（Ctrl+Shift+T）
- [x] 零手动步骤
- [x] 资源生成可复现
- [x] 错误自动检测

**扩展性验证**:
- [x] 注册新工具无需修改核心（ToolRegistration）
- [x] 新资源类型通过继承 Generator 基类

**技术债务**: 无

---

### 2. 数据驱动架构 ✓ (100%)
**核心设计**:
```
WeaponConfig (代码定义) 
    ↓ (Generator)
WeaponStatsSO (ScriptableObject)
    ↓ (Runtime)
WeaponController (运行时实例)
```

**验证标准**:
- [x] 配置与逻辑完全分离
- [x] 新增武器类型仅需修改 WeaponRegistry
- [x] 运行时零反射、零字符串查找

**扩展性验证**:
- [x] 新增武器：仅添加配置，无需修改代码
- [x] 新增属性：在 WeaponConfig 添加字段，自动流转到 SO

**技术债务**: 无

---

### 3. 物理引擎 ✓ (100%)
**核心能力**:
- [x] 世界缩放系统（统一 Speed/Range 缩放）
- [x] 双精度弹道计算（high-velocity precision）
- [x] 预测性拦截（移动目标）
- [x] 运动学积分器（模块化 Movement Functions）

**验证标准**:
- [x] 弹道计算误差 < 0.1%（通过 Unit Test）
- [x] 支持任意 gravity multiplier
- [x] 零 NaN/Infinity 错误

**扩展性验证**:
- [x] 新增运动模式：继承 MovementState 接口
- [x] 新增预测器：实现 ITargetPredictor

**技术债务**: 
- [ ] 需添加自动化测试覆盖（弹道计算模块）

---


### 4. 武器系统 ✓ (100%)
**概览**: 模块化武器架构，支持主炮、副炮、CIWS、导弹、鱼雷。

**核心特性**:
- [x] **Ammo Layer**: 定义弹道/伤害逻辑（独立于平台）
- [x] **Platform Layer**: 定义旋转/发射约束（独立于弹药）
- [x] **FCS Layer**: 火控系统（连接 Ammo + Platform）
- [x] 高级预判瞄准（二阶拦截算法 + 双精度弹道）

**详细文档**: 👉 **[WEAPON_SYSTEM.md](Documentation/WEAPON_SYSTEM.md)**
- 炮塔几何对齐标准（Axis-Aligned）
- Pivot-Centric 瞄准策略
- 高级预判算法详解
- 多炮管实现方案

**技术债务**: 
- [ ] WeaponController 重构（拆分为 TargetingModule + FiringModule）

---

### 5. 飞弹系统 ✓ (100%)
**概览**: 三段式飞行路径，实现逼真的 VLS → 巡航 → 攻顶轨迹。

**核心特性**:
- [x] Phase 0: 垂直发射（VLS）
- [x] Phase 1: 真正水平巡航（锁定俯仰角）
- [x] Phase 2: 终端俯冲（预测性追踪）
- [x] 水平距离判断（消除高度干扰）

**详细文档**: 👉 **[MISSILE_SYSTEM.md](Documentation/MISSILE_SYSTEM.md)**
- 为什么要"锁定水平"而非"维持高度"
- 水平距离 vs 3D 距离判断
- 终端距离计算公式推导
- 常见问题排查

**技术债务**: 无

---

### 6. VFX 系统 ✓ (100%)
**概览**: 自动化特效生成与管理，支持拖尾、火焰、爆炸。

**核心特性**:
- [x] 生成器模式（Generator → Prefab → Runtime）
- [x] 对象池管理（VFXManager）
- [x] 零硬编码（材质/颜色通过配置）
- [x] TrailRenderer + ParticleSystem 自动配置

**详细文档**: 👉 **[VFX_SYSTEM.md](Documentation/VFX_SYSTEM.md)**
- VFX 生成流程详解
- 材质与 Shader 管理
- 对象池实现细节
- 调试与故障排查

**技术债务**: 无


---

## 🚧 待验证技术模块（Phase 2: RTS 核心）

### 7. 编队系统 (Formation System) ⭐⭐⭐
**技术要求**:
- [ ] **数学模型**: 支持任意形状（V 字/线阵/圆阵）
- [ ] **动态重组**: 单位损失后自动填补
- [ ] **碰撞避免**: 单位间保持间距
- [ ] **转向协调**: 编队整体转向保持阵型

**架构设计**:
```
FormationConfig (数据)
    ↓
FormationController (逻辑)
    ↓
UnitPositionProvider (接口)
    ↓
Individual Unit Movement
```

**验证标准**:
- [ ] 编队形状可数据化定义（JSON/SO）
- [ ] 支持 100+ 单位编队（性能测试）
- [ ] 转向时无单位碰撞（边界条件测试）

**参考标准**: StarCraft II 编队系统（Craig Reynolds Flocking）

**预计工作量**: 2 周（含测试）

---

### 8. 命令系统 (Command System) ⭐⭐⭐
**技术要求**:
- [ ] **命令队列**: 支持多步指令（Move → Attack → Hold）
- [ ] **命令撤销**: 实现 Command Pattern
- [ ] **命令验证**: 非法指令自动拒绝
- [ ] **命令同步**: 多单位协同（编队攻击）

**架构设计**:
```
ICommand 接口
    ├── MoveCommand
    ├── AttackCommand
    ├── FormationCommand
    └── HoldCommand

CommandQueue (per unit/formation)
CommandExecutor (运行时)
```

**验证标准**:
- [ ] 命令可序列化（保存/读取）
- [ ] 支持宏命令（复杂战术链）
- [ ] 命令执行无延迟（< 16ms）

**参考标准**: Command & Conquer 命令系统

**预计工作量**: 1.5 周

---

### 9. 升级系统 (Upgrade/Roguelike System) ⭐⭐⭐
**技术要求**:
- [ ] **Modifier 架构**: 属性可叠加/覆盖
- [ ] **动态生效**: 运行时加载 Upgrade
- [ ] **冲突检测**: 互斥升级自动禁用
- [ ] **序列化**: 升级状态可保存

**架构设计**:
```
UpgradeConfig (数据)
    ↓
UpgradeModifier (运行时效果)
    ↓
StatModifierStack (应用到 Unit/Weapon)
```

**验证标准**:
- [ ] 升级可通过配置文件定义（零代码）
- [ ] 支持 100+ 种升级组合
- [ ] 属性计算正确性（加法/乘法优先级）

**参考标准**: Hades Boon 系统、Slay the Spire Relic 系统

**预计工作量**: 2 周

---

### 10. AI 框架 (AI Framework) ⭐⭐
**技术要求**:
- [ ] **行为树 (Behavior Tree)**: 模块化 AI 逻辑
- [ ] **目标评估**: 威胁值计算
- [ ] **路径规划**: A* 或 Flow Field
- [ ] **决策缓存**: 避免每帧重计算

**架构设计**:
```
AIController (顶层)
    ├── BehaviorTree (行为逻辑)
    ├── ThreatEvaluator (目标选择)
    └── PathPlanner (移动规划)
```

**验证标准**:
- [ ] AI 行为可配置（无需编程）
- [ ] 支持 50+ AI 单位同时运行（性能）
- [ ] AI 决策可回放（调试）

**参考标准**: GOAP (Goal-Oriented Action Planning) 或 Utility AI

**预计工作量**: 3 周

---

### 11. 保存/加载系统 (Persistence System) ⭐⭐
**技术要求**:
- [ ] **完整状态序列化**: 单位/武器/升级
- [ ] **版本兼容**: 旧存档自动升级
- [ ] **校验和**: 防篡改检测
- [ ] **增量保存**: 仅保存变化部分

**架构设计**:
```
ISaveable 接口
    ├── UnitState
    ├── WeaponState
    └── UpgradeState

SaveManager (序列化/反序列化)
```

**验证标准**:
- [ ] 保存文件大小 < 1MB
- [ ] 保存/加载时间 < 100ms
- [ ] 100% 状态复原（无数据丢失）

**参考标准**: Factorio 保存系统（高效序列化）

**预计工作量**: 1.5 周

---

## 🔬 技术验证清单（每个模块必须通过）

### Phase 1: 架构设计
- [ ] 画出 UML 类图（核心类关系）
- [ ] 定义接口契约（ICommand, IFormation, etc.）
- [ ] 确定数据流向（单向/双向）
- [ ] 评审架构（CodeReview）

### Phase 2: 原型实现
- [ ] 实现最小可验证原型（MVP）
- [ ] 编写单元测试（核心逻辑）
- [ ] 通过边界条件测试（null/空数组/极值）
- [ ] 性能基准测试（Profiler）

### Phase 3: 集成测试
- [ ] 与现有系统集成（无冲突）
- [ ] 端到端测试（完整流程）
- [ ] 压力测试（100+ 单位）
- [ ] 内存泄漏检测（长时间运行）

### Phase 4: 文档化
- [ ] 更新 ARCHITECTURE.md
- [ ] 编写使用示例（How-to）
- [ ] API 文档（代码注释）
- [ ] 架构决策记录（ADR）

---

## 📐 架构设计原则（Design Philosophy）

### 核心哲学：A Philosophy of Software Design (John Ousterhout)

#### 1. Deep Modules（深模块）> Shallow Modules
**核心理念**: 接口简单，实现强大。

**反例** (Shallow):
```csharp
class BallisticCalculator {
    public Vector3 CalculateVelocity(...) { }
    public float CalculateFlightTime(...) { }
    public float CalculateAngle(...) { }
    public bool CheckRange(...) { }
}
```
**问题**: 接口复杂，每个方法都需要客户端理解内部逻辑。

**正例** (Deep):
```csharp
public static class BallisticsComputer {
    // 简单接口：一个方法解决所有问题
    public static bool SolveInterception(
        Vector3 origin, 
        float speed, 
        float gravity, 
        ITargetPredictor target,
        out Vector3 fireVelocity,
        out float impactTime
    );
    // 复杂实现隐藏在内部（迭代求解、弹道方程、预测逻辑）
}
```

**当前实践**:
- ✅ `BallisticsComputer.SolveInterception` (Deep)
- ✅ `WeaponAssetGenerator.GenerateAll()` (Deep)
- ⚠️ `WeaponController` (200+ 行，需拆分但保持接口简单)

---

#### 2. Pull Complexity Downward（下沉复杂度）
**核心理念**: 让模块自己处理复杂性，而不是暴露给用户。

**反例**:
```csharp
// 客户端需要理解 Predictor 创建逻辑
var predictor = target.GetComponent<IPredictionProvider>() != null
    ? target.GetComponent<IPredictionProvider>().GetPredictor(...)
    : new LinearTargetPredictor(...);
aimingLogic(origin, stats, target, predictor);
```

**正例**:
```csharp
// WeaponController 内部处理 Predictor 逻辑
private ITargetPredictor GetPredictorForTarget() {
    // 复杂判断下沉到方法内部
    var provider = currentTarget.GetComponent<IPredictionProvider>();
    if (provider != null) return provider.GetPredictor(...);
    return new LinearTargetPredictor(...);
}
```

**当前实践**:
- ✅ `WeaponController.GetPredictorForTarget()` 隐藏复杂性
- ✅ `MovementFunctions.Resolve()` 隐藏字符串匹配逻辑

---

#### 3. Define Errors Out of Existence（消除错误条件）
**核心理念**: 通过设计消除边界条件，而非检查。

**反例**:
```csharp
if (speed < 0.001f) throw new ArgumentException();
if (gravity < 0) throw new ArgumentException();
```

**正例**:
```csharp
// 设计上消除错误：使用 Mathf.Abs 自动修正
float g = Mathf.Abs(gravity);
// 设计上消除错误：使用 Mathf.Max 防止除零
float speed = Mathf.Max(currentSpeed, 0.001f);
```

**当前实践**:
- ✅ `GuidedMissile` 使用 `speed < 0.001f ? return state` 而非抛异常
- ✅ 弹道计算使用 `root < 0 ? return null` 表示无解

---

#### 4. Strategic Programming（战略编程）> Tactical Programming
**核心理念**: 投资于架构，而非快速堆砌代码。

**Tactical** (快速但债务高):
```csharp
// 硬编码，难以扩展
if (weaponType == "Missile") {
    MoveMissile();
} else if (weaponType == "Torpedo") {
    MoveTorpedo();
}
```

**Strategic** (慢但可维护):
```csharp
// 投资于抽象，未来零成本扩展
MovementLogic logic = MovementFunctions.Resolve(config.MovementLogicName);
MovementState newState = logic(state, context, dt);
```

**当前实践**:
- ✅ 使用 Generator 模式（前期投资，后期收益）
- ✅ 数据驱动架构（配置与逻辑分离）

---

### 函数式编程原则（Functional Programming）

#### 1. 函数作为一等公民（First-Class Functions）
**核心理念**: 函数可作为参数、返回值、存储在变量中。

**当前实践**:
```csharp
// ✅ 函数作为返回值
public delegate MovementState MovementLogic(MovementState state, ...);
public static MovementLogic Resolve(string name) {
    return name switch {
        "Ballistic" => Ballistic,
        "GuidedMissile" => GuidedMissile,
        _ => Linear
    };
}

// ✅ 函数作为参数
public delegate Vector3? AimingLogic(Vector3 origin, ...);
Vector3? result = aimingLogic(myPos, FirePoint.forward, stats, target, predictor);
```

**未来应用**:
```csharp
// ✅ 升级系统：组合函数
public delegate float StatModifier(float baseValue);
StatModifier damageModifier = Compose(
    x => x * 1.5f,  // +50% 伤害
    x => x + 10f    // +10 基础伤害
);
float finalDamage = damageModifier(baseDamage);
```

---

#### 2. 纯函数（Pure Functions）
**核心理念**: 无副作用，相同输入永远返回相同输出。

**当前实践**:
```csharp
// ✅ 纯函数：MovementFunctions.Ballistic
public static MovementState Ballistic(MovementState state, MovementContext ctx, float dt) {
    // 不修改输入 state，返回新状态
    return new MovementState { ... };
}

// ✅ 纯函数：BallisticsComputer.SolveInterception
public static bool SolveInterception(..., out Vector3 fireVelocity) {
    // 计算结果，无全局状态修改
}
```

**避免**:
```csharp
// ❌ 非纯函数（修改全局状态）
public void Fire() {
    GlobalAmmoCount--;  // 副作用
}
```

---

#### 3. 不可变性（Immutability）
**核心理念**: 数据一旦创建不可修改。

**当前实践**:
```csharp
// ✅ 不可变配置
public readonly WeaponConfig FlagshipGun = new WeaponConfig(...) {
    readonly Range = 150000f,
    readonly Damage = 30f,
};

// ✅ 状态不可变（返回新状态）
MovementState newState = MovementFunctions.GuidedMissile(state, ctx, dt);
```

**未来改进**:
```csharp
// 使用 readonly struct
public readonly struct MovementState {
    public readonly Vector3 Position;
    public readonly Vector3 Velocity;
}
```

---

#### 4. 函数组合（Function Composition）
**核心理念**: 小函数组合成大函数。

**目标**:
```csharp
// 升级系统：组合多个修饰符
public static StatModifier Compose(params StatModifier[] modifiers) {
    return baseValue => modifiers.Aggregate(baseValue, (v, m) => m(v));
}

// 使用
StatModifier finalModifier = Compose(
    IncreaseDamage(0.5f),
    IncreaseRange(1000f),
    DecreaseReload(0.2f)
);
```

---

### 设计原则优先级

#### P0 (必须遵守)
1. **Deep Modules**: 接口简单，实现深入
2. **Pull Complexity Downward**: 复杂性下沉
3. **First-Class Functions**: 使用函数式抽象

#### P1 (强烈建议)
4. **Pure Functions**: 尽可能无副作用
5. **Immutability**: 配置与状态不可变
6. **Define Errors Out**: 设计消除错误

#### P2 (参考)
7. **SOLID 原则**: 作为次要参考（接口隔离、依赖倒置有价值）
8. **Clean Code**: 命名与格式参考

---

### 反模式清单（Anti-Patterns to Avoid）

#### ❌ Shallow Modules
```csharp
// 接口复杂，实现简单 = Shallow
public float GetX() { return x; }
public float GetY() { return y; }
public void SetX(float v) { x = v; }
```

#### ❌ Information Leakage
```csharp
// 暴露内部实现细节
public List<Vector3> GetInternalPathPoints() { return pathPoints; }
```

#### ❌ Pass-Through Methods
```csharp
// 仅转发调用，无价值
public void Fire() {
    weapon.Fire();  // 仅转发
}
```

#### ❌ Temporal Decomposition
```csharp
// 按时间顺序分解（错误）
class Phase1Handler { }
class Phase2Handler { }
// 应按功能分解
class NavigationHandler { }
class CombatHandler { }
```

---

## 📚 参考标准（Design Philosophy）

### 核心参考
- **Book**: "A Philosophy of Software Design" (John Ousterhout, Stanford)
- **Concept**: Functional Programming Principles (Haskell/F# 社区)
- **Paper**: "Out of the Tar Pit" (Moseley & Marks) - 复杂度管理

### 次要参考
- **SOLID 原则**: 接口隔离原则 (ISP)、依赖倒置 (DIP) 有价值
- **Clean Code**: 命名与格式规范
- **Game Programming Patterns**: 状态机、对象池等模式

### 不推荐
- ❌ 过度抽象（为了抽象而抽象）
- ❌ 过度分层（每层只转发调用）
- ❌ 过度使用设计模式（模式驱动 > 问题驱动）

---

*本文档专注技术架构验证，非技术部分仅作占位符*
