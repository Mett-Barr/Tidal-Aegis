# ADR-001: 文档架构设计 (Living Documentation)

**日期**: 2025-11-26  
**状态**: ✅ 已采纳  
**决策者**: 项目团队

---

## 背景

项目初期存在多个重复、混乱的文档文件（5 个顶层 .md 文件），导致：
- 信息查找困难
- 文档不一致
- AI 助手难以理解项目结构

需要建立标准化的文档体系。

---

## 决策

采用**业界黄金标准 - Living Documentation 模式**。

### 文档结构
```
README.md               # 项目入口（QuickStart）
├── GDD.md              # Game Design Document（游戏设计）
├── TDD.md              # Technical Design Document（技术架构）
└── CONTRIBUTING.md     # 开发规范 + Meta Documentation
    ↓
Documentation/*.md      # 详细技术文档
```

### 核心原则
1. **单一入口**: README.md 作为唯一起点
2. **分层信息**: 顶层概览 → 详细规范
3. **链接关联**: 摘要 + 链接 → 详细文档
4. **反向更新**: 代码改动 → 文档同步

---

## 替代方案

### 方案 A: 单一巨型文档
- ❌ 信息过载
- ❌ 难以维护
- ❌ 查找成本高

### 方案 B: Wiki 系统
- ⚠️ 需要额外基础设施
- ⚠️ 与代码仓库分离
- ✅ 搜索功能强大

### 方案 C: 选中的 Living Documentation
- ✅ 与代码仓库同步
- ✅ 版本控制
- ✅ 分层信息密度
- ✅ 业界标准

---

## 影响

### 正面影响
1. **新人友好**: 单一入口，渐进式学习
2. **AI 协作**: 明确的文档结构，易于理解
3. **可维护性**: 文档与代码同步更新
4. **可扩展性**: 新增系统仅需添加 `Documentation/*.md`

### 负面影响
1. **学习成本**: 团队需要适应新规范
2. **维护成本**: 需要手动保持文档同步（未来可自动化）

---

## 验证标准

- [x] README.md 包含醒目的"必读文档"提示
- [x] CONTRIBUTING.md 包含完整的文档更新规范
- [x] TDD.md 包含 Documentation 索引章节
- [x] 所有详细文档位于 `Documentation/` 目录

---

## 相关文档

- [CONTRIBUTING.md - 文档维护规范](../CONTRIBUTING.md#文档维护规范meta-documentation)
- [TDD.md - 详细技术文档索引](../TDD.md#详细技术文档索引-detailed-documentation)

---

*本 ADR 记录了文档架构的设计决策，未来如需调整请创建新的 ADR。*
