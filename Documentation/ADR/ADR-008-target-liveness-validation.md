# ADR-008: Target Liveness Validation (Aegis CEC Implementation)

**Status:** Implemented  
**Date:** 2025-11-27  
**Deciders:** System Architecture Review + User Feedback  

## Context

系統存在嚴重缺陷：當目標被其他武器（如 CIWS）擊殺後，雷射仍持續照射屍體，浪費能源且無法轉向新威脅。

### 問題場景

```
T=0.0s: 飛彈進入射程
  → LaserCIWS 鎖定並開始照射
  → CIWS 同時鎖定

T=0.5s: CIWS 擊殺飛彈
  → 飛彈.IsDead() = true
  → ❌ LaserCIWS 不知道，繼續照射

T=0.5s-2.0s: 雷射浪費能源照射屍體
  → 無法轉向新威脅
  → 戰術效率低下
```

## Decision

實施**目標生存狀態驗證 (Target Liveness Validation)**，遵循真實 Aegis 戰鬥系統的 **Cooperative Engagement Capability (CEC)** 原則。

### 核心原則

> **Aegis CEC Doctrine**  
> 所有武器系統共享即時目標狀態。當任何武器擊殺目標，其他武器立即停止攻擊該目標並轉向新威脅。

### 實現方案

#### 方案 A：輕量級輪詢驗證（已實施）

```csharp
// LaserBeamController.cs:78-96
private void Update()
{
    if (!isActive) return;
    
    // ✅ 新增：檢查目標生存狀態
    if (target == null || target.IsDead() || !IsTargetInRange())
    {
        Deactivate();  // 立即停止照射
        return;
    }
    
    // 正常邏輯
    UpdateBeamPosition();
    ApplyDamage(Time.deltaTime);
}
```

**檢查順序**：
1. `target == null` - GameObject 已被銷毀
2. `target.IsDead()` - ✅ **新增**：目標已死亡（可能被他武器擊殺）
3. `!IsTargetInRange()` - 目標超出射程

## Consequences

### Positive ✅

1. **即時響應擊殺事件**
   - 延遲：~16ms（1 幀）
   - CIWS 擊殺 → 下一幀雷射停止

2. **避免能源浪費**
   - 不再照射屍體 1-2 秒
   - 立即轉向新威脅

3. **符合真實火控原則**
   - 遵循 Aegis CEC 多武器協同
   - 預防過度殺傷（Overkill）

4. **實現簡單**
   - 只需一行代碼
   - 性能影響極小（每幀一次接口調用）

### Negative ⚠️

1. **輪詢開銷**
   - 每幀調用 `IsDead()`
   - 影響：極低（簡單布爾檢查）

2. **非零延遲**
   - 最多 16ms 延遲
   - 可接受（人眼無法察覺）

## Implementation

### Modified Files

[`LaserBeamController.cs:78-96`](file:///Users/mac/Documents/UnityProjects/Tidal-Aegis/Assets/_Project/Scripts/Systems/Weapons/LaserBeamController.cs#L78-L96)

```diff
- if (target == null || !IsTargetInRange())
+ if (target == null || target.IsDead() || !IsTargetInRange())
{
    Deactivate();
    return;
}
```

### Behavior Comparison

| 時間點 | 修復前 | 修復後 ✅ |
|--------|--------|----------|
| T=0.0s | CIWS + Laser 鎖定 | CIWS + Laser 鎖定 |
| T=0.5s | CIWS 擊殺目標 | CIWS 擊殺目標 |
| T=0.516s (下一幀) | ❌ Laser 持續照射 | ✅ Laser.Update() 檢測 IsDead() → Deactivate() |
| T=0.6s (冷卻完成) | ❌ 仍在照射屍體 | ✅ 已尋找新目標 |

## Verification Plan

### Test Scenarios

1. **雙武器協同測試**
   ```
   配置：1x LaserCIWS + 1x CIWS
   發射：2枚飛彈
   觀察：
   - ✅ CIWS 擊殺飛彈 1 → Laser 立即停止
   - ✅ Laser 0.1s 後鎖定飛彈 2
   ```

2. **能源效率測試**
   ```
   記錄：雷射照射時間
   預期：
   - 修復前：照射屍體 ~1.5s
   - 修復後：立即停止（~16ms 延遲）
   ```

## Future Enhancements

### 事件驅動架構（長期）

```csharp
// 建立類似 Aegis 的中央事件系統
public static class CombatEvents
{
    public static event Action<IDamageable> OnUnitKilled;
}

// 雷射訂閱事件（零延遲）
private void OnEnable()
{
    CombatEvents.OnUnitKilled += OnTargetKilled;
}
```

**優勢**：
- 零延遲（<1ms）
- 完整實現 CEC 原則
- 更易擴展（多武器協同）

## References

- [Target Validation Analysis](file:///Users/mac/.gemini/antigravity/brain/7d33182b-5047-4a85-b064-513afa0912fa/target_validation_analysis.md) - 深度分析報告
- Aegis Combat System - Cooperative Engagement Capability
- HELIOS Laser Weapon Integration with Aegis

## Notes

此修復是基於用戶反饋的關鍵發現：**雷射在目標被他武器擊殺後仍持續射擊**。

實現遵循真實海軍火控系統（Aegis CEC）的協同原則，確保多武器高效協同而非相互浪費。
