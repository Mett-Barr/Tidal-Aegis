# 编辑器工具系统架构

[← 返回主文档](../ARCHITECTURE.md)

---

## 📋 统一菜单管理系统

### 核心文件
**`ToolRegistration.cs`** - 所有编辑器工具的注册中心

**路径：** `Assets/_Project/Scripts/Editor/Tooling/ToolRegistration.cs`

---

## ⚠️ 重要规则

### ❌ 不要直接使用 [MenuItem]

```csharp
// ❌ 错误方式 - 会导致菜单重复和快捷键冲突
[MenuItem("Tools/My Tool")]
public static void MyTool() { }
```

### ✅ 正确方式：通过 ToolRegistration 注册

```csharp
// ✅ 正确方式
// 1. 创建工具类（不添加 MenuItem）
public static class MyTool
{
    public static void Execute()
    {
        // 工具逻辑
    }
}

// 2. 在 ToolRegistration.cs 中注册
registry.Register("分类", "工具名称", () => {
    MyTool.Execute();
}, "工具描述");
```

---

## 🔧 添加新工具流程

### Step 1: 创建工具类

```csharp
// Assets/_Project/Scripts/Editor/MyNewTool.cs
namespace NavalCommand.Editor
{
    public static class MyNewTool
    {
        // 注意：不要添加 [MenuItem]！
        public static void Execute()
        {
            Debug.Log("Tool executed!");
        }
    }
}
```

### Step 2: 注册到 ToolRegistration

```csharp
// ToolRegistration.cs
public static void RegisterAllTools(EditorToolRegistry registry)
{
    // ... 现有工具 ...
    
    // 添加新工具
    registry.Register("工具分类", "我的新工具", () => {
        MyNewTool.Execute();
    }, "工具功能描述");
}
```

### Step 3: 测试
- 重启Unity或重新编译
- 检查 `Tools` 菜单
- 确认工具出现在正确分类下

---

## 🎯 当前已注册工具

### 世界生成 (World Gen)
- **重建世界 (Rebuild World)** - `Ctrl+Shift+T`
  - 调用: `HierarchyRestorer.RestoreHierarchy()`
  - 功能：完整场景重建

- **生成空船殼 (Generate Hulls)**
  - 调用: `ContentRebuilder.GenerateEmptyHulls()`

### VFX 工具 (VFX Tools)
- **清理 VFX Prefabs**
- **诊断 VFX Prefabs**
- **修复 VFX 材质**

---

## ⚙️ 快捷键管理

### 当前快捷键
- `Ctrl+Shift+T` - Rebuild World
- `Ctrl+Shift+D` - Naval Command Dashboard

### 添加快捷键

```csharp
// 在 ToolRegistration 中无法直接设置快捷键
// 如需快捷键，必须在工具类中使用 MenuItem（例外情况）

// 例外：Dashboard 等独立窗口工具
[MenuItem("Tools/My Window %#w")]  // Ctrl+Shift+W
public static void OpenWindow() { }
```

**注意：** 只有独立窗口工具可以使用 MenuItem，普通工具必须通过 ToolRegistration！

---

## 🐛 常见问题

### 问题 1: 菜单中出现重复工具

**原因：**
- 工具类有 `[MenuItem]`
- ToolRegistration 也注册了

**解决：**
- 移除工具类的 `[MenuItem]`
- 只保留 ToolRegistration 中的注册

### 问题 2: 快捷键冲突

**症状：** 按快捷键时弹出多个窗口或执行多个操作

**解决：**
```bash
# 搜索所有 MenuItem
grep -r "MenuItem.*%#" Assets/_Project/Scripts/Editor/

# 检查是否有重复的快捷键定义
```

### 问题 3: 工具未出现在菜单中

**检查清单：**
1. ✅ 是否在 ToolRegistration 中注册？
2. ✅ 代码是否编译成功？
3. ✅ Unity 是否重启？

---

## 📚 相关文档

- [VFX 系统架构](VFX_SYSTEM.md)
- [武器系统](WEAPON_SYSTEM.md)
