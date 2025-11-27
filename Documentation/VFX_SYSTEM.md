# VFX 系统架构 (VFX System Architecture)

[← 返回技术文档](../TDD.md)

---

## 🎨 VFX 系统概述

VFX系统负责所有视觉效果的生成、管理和渲染。

---

## 📁 系统组成

### 生成器（Editor）
- **`VFXPrefabGenerator.cs`** - 生成VFX Prefabs
- **`VFXManagerConfigurator.cs`** - 自动配置VFXManager

### 运行时（Runtime）
- **`VFXManager.cs`** - VFX对象池管理
- **`AutoFollowVFX.cs`** - 跟随目标的VFX
- **`AutoRecycleVFX.cs`** - 自动回收VFX

### Prefabs
- **`VFX_MissileTrail.prefab`** - 导弹烟雾轨迹
- **`VFX_TorpedoBubbles.prefab`** - 鱼雷气泡
- **`VFX_TracerGlow.prefab`** - 曳光弹发光
- **`VFX_MuzzleFlash.prefab`** - 枪口火焰

---

## ⚠️ Shader 和材质问题

### 粉色方块问题

**症状：** VFX显示为粉色/洋红色方块

**根本原因：** Shader missing 或不兼容

### Shader 兼容性

Unity有多种渲染管线，shader名称不同：

| 渲染管线 | Particle Shader |
|---------|----------------|
| Built-in | `Particles/Standard Unlit` |
| URP | `Universal Render Pipeline/Particles/Unlit` |
| Legacy | `Legacy Shaders/Particles/Additive` |

**当前问题：**
- `GenerateMuzzleFlashVFX` 使用 `Particles/Standard Unlit`
- 如果项目不是Built-in管线，shader会missing → 粉色

### 解决方案

**方案1：使用最兼容的shader**
```csharp
// 使用 Legacy shader（所有管线都支持）
Shader.Find("Legacy Shaders/Particles/Additive")
```

**方案2：检测渲染管线**
```csharp
// 自动检测并使用正确shader
Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
if (shader == null)
{
    shader = Shader.Find("Particles/Standard Unlit");
}
if (shader == null)
{
    shader = Shader.Find("Legacy Shaders/Particles/Additive");
}
```

---

## 🔧 VFX 生成流程

### 手动生成（推荐）

```
Unity菜单 → Tools → Generate → VFX Prefabs
```

**执行内容：**
1. 生成 VFX_MissileTrail
2. 生成 VFX_TorpedoBubbles
3. 生成 VFX_TracerGlow
4. 生成 VFX_MuzzleFlash

### 自动配置 VFXManager

通过 ToolRegistration 调用（或手动）：
```csharp
VFXManagerConfigurator.ConfigureVFXManager();
```

**功能：**
- 自动加载所有VFX Prefabs
- 赋值到VFXManager的字段
- 保存场景

---

## 🐛 调试VFX问题

### 检查Shader是否存在

```csharp
Shader shader = Shader.Find("Particles/Standard Unlit");
if (shader == null)
{
    Debug.LogError("Shader not found!");
}
```

### 检查Material

在Unity中：
1. 选中VFX Prefab
2. 展开ParticleSystemRenderer
3. 查看Material
   - 如果显示"None"或粉色 → Shader missing
   - 如果正常 → Shader存在

### 检查VFXManager配置

```
Hierarchy → VFXManager → Inspector
```

确认所有4个Prefab字段都已赋值：
- Missile Trail Prefab
- Torpedo Bubbles Prefab
- Tracer Glow Prefab
- **Muzzle Flash Prefab** ← 检查这个

---

## 🚨 关键问题排查：VFX 完全失效

### 症状

- ✅ Impact VFX（爆炸）正常工作
- ❌ Trail VFX（烟雾轨迹）完全不出现

### 根本原因

**`HierarchyRestorer.RestoreHierarchy()` 缺少 VFXManager 恢复逻辑**

VFX 系统分为两个子系统：
1. **Impact VFX**: 通过 `VFXLibrarySO` 管理（爆炸、水花等）
2. **Trail VFX**: 通过 `VFXManager` 对象池管理（烟雾、轨迹等）

如果场景中没有 `VFXManager` GameObject：
- `VFXManagerConfigurator` 无法配置 Trail VFX Prefab 引用
- 所有 `SpawnTrailVFX()` 调用失败
- 只有 Impact VFX 能工作（因为它们通过 PoolManager）

### 解决方案

在 `HierarchyRestorer.cs` 添加 `RestoreVFXManager()` 方法：

```csharp
private static void RestoreVFXManager()
{
    var vfxManager = Object.FindObjectOfType<NavalCommand.Systems.VFX.VFXManager>();
    if (vfxManager == null)
    {
        GameObject go = new GameObject("VFXManager");
        vfxManager = go.AddComponent<NavalCommand.Systems.VFX.VFXManager>();
        Debug.Log("Created VFXManager");
    }

    // Assign VFX Library
    string libraryPath = "Assets/_Project/Data/VFX/DefaultVFXLibrary.asset";
    var library = AssetDatabase.LoadAssetAtPath<NavalCommand.Systems.VFX.VFXLibrarySO>(libraryPath);
    
    if (library != null)
    {
        SerializedObject so = new SerializedObject(vfxManager);
        so.FindProperty("_library").objectReferenceValue = library;
        so.ApplyModifiedProperties();
        Debug.Log("Assigned VFX Library to VFXManager");
    }
    
    EditorUtility.SetDirty(vfxManager);
}
```

并在 `RestoreHierarchy()` 中调用：

```csharp
public static void RestoreHierarchy()
{
    // ... existing code ...
    RestorePoolManager();
    RestoreWorldPhysicsSystem();
    RestoreSpatialGridSystem();
    RestoreVFXManager();  // ← 添加这行
    RestoreGameManager();
    // ...
}
```

### 诊断日志

在 Play Mode 启动时检查 Console：

**正常状态**：
```
[VFXManager] Initialized - Library: ✓
[VFXManager] Trail Prefabs - Missile: ✓, Torpedo: ✓, Tracer: ✓, MuzzleFlash: ✓
[VFX_DEBUG] VFXManager initialized: 4 types, 20 each
```

**异常状态**（VFXManager 缺失）：
```
[VFXManager] Trail Prefabs - Missile: ✗, Torpedo: ✗, Tracer: ✗, MuzzleFlash: ✗
[VFX_DEBUG] VFXManager initialized: 0 types, 20 each
[VFX_DEBUG] ERROR: VFX type MissileTrail not registered in pool!
```

---

## ⚠️ 关键开发规范：程序化生成材质

### ❌ 常见错误：内存材质

```csharp
// 错误：直接使用内存中的材质赋值给Prefab
Material mat = new Material(Shader.Find("..."));
renderer.material = mat;
PrefabUtility.SaveAsPrefabAsset(root, path);
// 结果：Prefab保存后，Material引用丢失 → 显示为粉色方块 (Material = NULL)
```

### ✅ 正确规范：必须保存为Asset

当通过代码生成Prefab并使用动态创建的材质时，**必须**遵循以下步骤：

1. **创建材质**：在内存中 `new Material(...)`
2. **保存Asset**：使用 `AssetDatabase.CreateAsset(...)` 保存到磁盘
3. **刷新数据库**：`AssetDatabase.SaveAssets()` 和 `AssetDatabase.Refresh()`
4. **重新加载**：使用 `AssetDatabase.LoadAssetAtPath<Material>(...)` 获取引用
5. **赋值**：将加载的 Asset 引用赋值给 Renderer

```csharp
// 正确范例
Material mat = new Material(shader);
string path = "Assets/.../MyMat.mat";

AssetDatabase.CreateAsset(mat, path); // 1. 保存
AssetDatabase.SaveAssets();           // 2. 写入磁盘
AssetDatabase.Refresh();              // 3. 刷新

mat = AssetDatabase.LoadAssetAtPath<Material>(path); // 4. 获取Asset引用
renderer.material = mat;              // 5. 赋值
```

---

## 📚 相关文档

- [编辑器工具系统](EDITOR_TOOLS.md)
- [武器系统](WEAPON_SYSTEM.md)
