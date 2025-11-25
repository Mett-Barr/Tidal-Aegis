# 项目开发规范

[← 返回主文档](../ARCHITECTURE.md)

---

## ⚠️ 核心原则

### 1. Rebuild World 是唯一真理来源

**所有资源生成和配置必须集成到 Rebuild World 流程中。**

```
Ctrl+Shift+T → Rebuild World → 一切就绪
```

**禁止：**
- ❌ 手动生成Prefabs
- ❌ 手动配置Manager
- ❌ 手动删除旧资源
- ❌ 要求用户执行多个步骤

**正确：**
- ✅ 所有流程自动化到 Rebuild World
- ✅ 一键完成所有设置
- ✅ Play Mode 立即可用

---

## 🔧 Rebuild World 完整流程

**触发：** `Ctrl+Shift+T`

**执行顺序：**
```
HierarchyRestorer.RestoreHierarchy()
  ↓
ContentRebuilder.RebuildAllContent()
  ├─> WeaponAssetGenerator.GenerateAllWeapons()
  ├─> VFXPrefabGenerator.GenerateAll()
  └─> VFXManagerConfigurator.ConfigureVFXManager()
  ↓
FixProjectilePrefabs()
  ↓
RestorePoolManager()
RestoreWorldPhysicsSystem()
RestoreSpatialGridSystem()
RestoreGameManager()
RestoreSpawningSystem()
RestoreHUD()
RestoreEventSystem()
RestoreLighting()
RestoreCamera()
```

**结果：**
- ✅ 所有武器资源已生成
- ✅ 所有VFX Prefabs已生成
- ✅ VFXManager已配置
- ✅ 场景完整可用
- ✅ 直接Play Mode测试

---

## 📝 添加新功能规范

### 如果需要生成资源

**必须集成到 ContentRebuilder.RebuildAllContent()**

```csharp
// ContentRebuilder.cs
public static void RebuildAllContent()
{
    // 现有流程...
    
    // 添加你的生成器
    MyNewGenerator.GenerateAll();
    
    // 如果需要配置Manager
    MyManagerConfigurator.Configure();
}
```

### 如果需要配置场景对象

**必须集成到 HierarchyRestorer.RestoreHierarchy()**

```csharp
// HierarchyRestorer.cs
public static void RestoreHierarchy()
{
    ContentRebuilder.RebuildAllContent();
    
    // 现有流程...
    
    // 添加你的配置
    RestoreMyNewSystem();
}
```

---

## ❌ 绝对禁止的做法

### 1. 分离的手动步骤

```
❌ 错误示例：

"请执行以下步骤：
1. Tools → Generate → My Prefabs
2. 手动配置 MyManager
3. 删除旧文件
4. Play Mode 测试"
```

**为什么错误：**
- 用户会忘记步骤
- 容易出错
- 浪费时间
- 不可重复

**正确做法：**
```csharp
// 全部自动化到 Rebuild World
ContentRebuilder.RebuildAllContent()
{
    MyPrefabGenerator.GenerateAll();
    MyManagerConfigurator.Configure();
    CleanOldFiles();
}
```

### 2. 要求手动删除文件

```
❌ "请手动删除 Assets/xxx/old.prefab"
✅ 在代码中自动删除
```

### 3. 要求手动配置Inspector

```
❌ "请在Inspector中拖入Prefab到Manager"
✅ 使用 SerializedObject 自动赋值
```

---

## ✅ 测试规范

### Play Mode 测试必须零配置

**用户流程：**
```
1. Ctrl+Shift+T (Rebuild World)
2. Play Mode
3. 测试功能
```

**不允许：**
```
1. Ctrl+Shift+T
2. Tools → Generate → XXX
3. 手动配置 YYY
4. 删除旧文件
5. Play Mode
```

### 验证清单

在提交代码前，验证：
- [ ] Ctrl+Shift+T 后立即 Play Mode 可用
- [ ] 无需任何手动步骤
- [ ] 无需查看文档
- [ ] 所有资源自动生成
- [ ] 所有Manager自动配置

---

## 🔍 AI 助手规范

### 在实现新功能时

**必须问自己：**
1. ✅ 这个流程是否已集成到 Rebuild World？
2. ✅ 用户是否只需要 Ctrl+Shift+T？
3. ✅ 是否有任何手动步骤？

**如果答案是"有手动步骤"：**
- ❌ 立即停止
- ✅ 重新设计为自动化
- ✅ 集成到 Rebuild World

### 在修复Bug时

**不允许的解决方案：**
- ❌ "请手动删除XXX"
- ❌ "请执行Tools → XXX"
- ❌ "请在Inspector中配置XXX"

**正确的解决方案：**
- ✅ 修改代码使其自动化
- ✅ 集成到 Rebuild World
- ✅ 用户只需 Ctrl+Shift+T

---

## 📚 示例：VFX 系统

### ❌ 错误方式（旧）

```
用户步骤：
1. Ctrl+Shift+T
2. Tools → Generate → VFX Prefabs
3. 删除旧的 VFX_MuzzleFlash.prefab
4. Hierarchy → VFXManager → 拖入新Prefab
5. Ctrl+S 保存场景
6. Play Mode
```

### ✅ 正确方式（新）

```
用户步骤：
1. Ctrl+Shift+T
2. Play Mode
```

**实现：**
```csharp
ContentRebuilder.RebuildAllContent()
{
    WeaponAssetGenerator.GenerateAllWeapons();
    VFXPrefabGenerator.GenerateAll();  // 自动生成
    VFXManagerConfigurator.ConfigureVFXManager();  // 自动配置
}
```

---

## 📖 相关文档

- [编辑器工具系统](EDITOR_TOOLS.md)
- [VFX 系统架构](VFX_SYSTEM.md)
