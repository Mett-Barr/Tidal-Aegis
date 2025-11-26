# Tidal-Aegis

> **RTS Roguelike** 现代海战策略游戏  
> **核心玩法**: 战术编排 + 升级多样性  
> **技术栈**: Unity 3D + 自研物理引擎

---

> [!IMPORTANT]
> **首次接触本项目？请务必先阅读：**
> 1. 📐 **[CONTRIBUTING.md](CONTRIBUTING.md)** - 开发规范与文档更新流程（必读）
> 2. 🔬 **[TDD.md](TDD.md)** - 技术架构与设计原则
> 3. 🎮 **[GDD.md](GDD.md)** - 游戏设计文档
>
> **AI 助手注意**：代码修改前请先阅读 `CONTRIBUTING.md` 和 `TDD.md - 架构设计原则`

---

## 🚀 快速开始

### 重建世界
```
Ctrl + Shift + T  # 一键重建所有资源
```

### 进入 Play Mode
1. 打开场景：`Assets/_Project/Scenes/MainScene`
2. 点击 Play
3. 使用 WASD 控制旗舰

---

## 📚 文档导航

### 游戏设计
👉 **[GDD.md](GDD.md)** - 游戏设计文档
- 核心玩法循环
- 系统概览（编队/升级/战斗）
- Roguelike 机制

### 技术架构
👉 **[TDD.md](TDD.md)** - 技术设计文档
- 已验证技术模块（武器/飞弹/VFX）
- RTS 核心支柱（编队/命令/升级）
- 架构设计原则（Deep Modules + FP）

### 开发规范
👉 **[CONTRIBUTING.md](CONTRIBUTING.md)** - 开发指南
- Rebuild World 工作流
- 代码标准
- 提交规范

---

## 🎯 当前进度

| 模块 | 状态 |
|------|------|
| 核心战斗系统 | ✅ 100% |
| 武器系统 | ✅ 100% |
| 飞弹系统 | ✅ 100% |
| VFX 系统 | ✅ 100% |
| 编队系统 | 🔄 进行中 |
| 升级系统 | ⏳ 待开始 |

---

## 🏗️ 项目结构

```
Tidal-Aegis/
├── Assets/_Project/
│   ├── Scripts/
│   │   ├── Editor/          # 编辑器工具
│   │   ├── Systems/         # 游戏系统
│   │   └── Entities/        # 游戏实体
│   └── Prefabs/             # 预制体
├── Documentation/           # 详细技术文档
├── GDD.md                   # 游戏设计文档
├── TDD.md                   # 技术设计文档
└── CONTRIBUTING.md          # 开发规范
```

---

## 🤝 贡献

详见 [CONTRIBUTING.md](CONTRIBUTING.md)

---

**维护者**: [Your Name]  
**License**: MIT
