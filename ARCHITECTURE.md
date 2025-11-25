# Tidal-Aegis 项目架构文档

## � 文档索引

本文档提供项目架构的总览和各子系统的详细文档链接。

---

## 🏗️ 核心架构

### [⚠️ 开发规范（必读）](Documentation/DEVELOPMENT_STANDARDS.md)
- **Rebuild World 是唯一真理来源**
- 所有流程必须自动化
- 禁止手动步骤

### [编辑器工具系统](Documentation/EDITOR_TOOLS.md)
- 统一菜单管理（ToolRegistration）
- 工具添加规范
- 快捷键管理

### [VFX 系统架构](Documentation/VFX_SYSTEM.md)
- VFX 生成流程
- 材质和Shader管理
- 对象池系统

### [武器系统](Documentation/WEAPON_SYSTEM.md)
- 武器资源生成
- 弹道系统
- 炮塔控制架构 (2-Axis)
- 高级预判瞄準 (Advanced Predictive Aiming)
- VFX集成

### [飞弹系统](Documentation/MISSILE_SYSTEM.md)
- 三段式飞行路径 (VLS → Cruise → Top-Attack)
- 水平巡航设计
- 终端距离计算

---

## 📁 项目结构

```
Tidal-Aegis/
├── Assets/_Project/
│   ├── Scripts/
│   │   ├── Editor/              # 编辑器工具
│   │   │   ├── Tooling/         # 工具管理（ToolRegistration）
│   │   │   ├── Generators/      # 资源生成器
│   │   │   └── ...
│   │   ├── Entities/            # 游戏实体
│   │   ├── Systems/             # 游戏系统
│   │   └── VFX/                 # VFX运行时脚本
│   ├── Prefabs/
│   │   ├── VFX/Projectile/      # VFX Prefabs
│   │   └── Projectiles/         # 弹药 Prefabs
│   └── Data/                    # ScriptableObjects
└── Documentation/               # 📖 架构文档（本目录）
    ├── EDITOR_TOOLS.md          # 编辑器工具系统
    ├── VFX_SYSTEM.md            # VFX系统
    ├── WEAPON_SYSTEM.md         # 武器系统
    └── MISSILE_SYSTEM.md        # 飞弹系统
```

---

## 🎯 快速参考

### 常用快捷键
- `Ctrl+Shift+T` - Rebuild World
- `Ctrl+Shift+D` - Naval Command Dashboard

### 关键文件
- `ToolRegistration.cs` - 编辑器菜单注册中心
- `HierarchyRestorer.cs` - Rebuild World 核心逻辑
- `VFXPrefabGenerator.cs` - VFX Prefab 生成器
- `WeaponAssetGenerator.cs` - 武器资源生成器

---

## � 开发规范

### 添加新功能时
1. 查阅对应子系统文档
2. 遵循现有架构模式
3. 更新相关文档

### 修改核心系统时
1. ⚠️ 先阅读系统架构文档
2. ⚠️ 理解现有设计决策
3. ⚠️ 避免破坏现有功能

---

## 🔍 问题排查

### 编辑器菜单问题
→ 参考 [编辑器工具系统](Documentation/EDITOR_TOOLS.md)

### VFX 显示问题（粉色方块等）
→ 参考 [VFX 系统架构](Documentation/VFX_SYSTEM.md)

### 武器/弹药问题
→ 参考 [武器系统](Documentation/WEAPON_SYSTEM.md)

### 导弹飞行路径问题
→ 参考 [飞弹系统](Documentation/MISSILE_SYSTEM.md)

---

## 📅 文档维护

**最后更新：** 2025-11-26  
**维护者：** AI Assistant + 项目团队
