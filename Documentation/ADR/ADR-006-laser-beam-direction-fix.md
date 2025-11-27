# ADR-006: Laser Beam Direction Physics Fix

**Status:** Implemented  
**Date:** 2025-11-27  
**Deciders:** System Architecture Review  

## Context

雷射砲塔系統存在嚴重的技術架構問題：光束發射方向**直接指向目標中心**（魔法追蹤），而非**沿著砲管實際朝向**發射，違反物理真實性原則。

### 問題表現

```
當前錯誤行為（Before）:
  砲塔朝向: →
  目標位置:       ●
  光束方向:     ╱  （強制指向目標，無視砲管朝向）
  結果: 視覺不一致，破壞「指向即命中」原則

期望正確行為（After）:
  砲塔朝向: →
  目標位置:       ●
  光束方向: →      （沿砲管發射）
  結果: 砲塔對齊則命中，未對齊則脫靶
```

### 根本原因

[`LaserBeamController.GetHitPoint()`](file:///Users/mac/Documents/UnityProjects/Tidal-Aegis/Assets/_Project/Scripts/Systems/Weapons/LaserBeamController.cs#L116-L146) 使用錯誤的方向計算：

```csharp
// ❌ 錯誤實現
Vector3 direction = (targetPos - origin.position).normalized;
Ray ray = new Ray(origin.position, direction);
```

這導致光束永遠指向目標中心，無視砲塔實際旋轉狀態。

## Decision

實施**物理基礎光束追蹤 (Physics-Based Beam Tracing)**：

### 核心原則

> **"Point-to-Hit" Accuracy Principle**  
> 光束必須沿著砲管物理朝向發射。命中與否取決於砲塔對齊狀態，而非程式邏輯強制。

### 實現變更

#### 1. 光束方向計算 (GetHitPoint)

```diff
- Vector3 direction = (targetPos - origin.position).normalized;
+ Vector3 beamDirection = origin.forward;  // 使用砲管實際朝向
Ray ray = new Ray(origin.position, beamDirection);
```

#### 2. 命中驗證 (ApplyDamage)

```csharp
// 僅在光束真正命中目標時才造成傷害
if (Physics.Raycast(ray, out RaycastHit hit, maxRange))
{
    if (hit.collider.transform.IsChildOf(targetComponent.transform))
    {
        isHittingTarget = true;
    }
}

if (!isHittingTarget)
{
    return;  // 砲塔未對齊，不造成傷害
}
```

## Consequences

### Positive ✅

1. **物理真實性**：光束行為符合光學設備物理特性
2. **視覺一致性**：光束視覺效果與砲塔朝向完全一致
3. **戰術清晰性**：「砲塔對齊 → 光束命中」因果關係明確
4. **防止穿牆**：光束無法繞過障礙物攻擊目標

### Negative ⚠️

1. **命中率下降**：砲塔旋轉速度成為瓶頸（設計預期）
2. **需要精確對齊**：玩家需更謹慎管理火控系統（增加技巧深度）

### Neutral 📊

1. **與 CIWS 一致**：雷射砲塔與傳統 CIWS 同樣受限於旋轉速度
2. **光速優勢保留**：在**已對齊後**仍享有零飛行時間優勢

## Implementation

### Modified Files

1. [`LaserBeamController.cs:116-146`](file:///Users/mac/Documents/UnityProjects/Tidal-Aegis/Assets/_Project/Scripts/Systems/Weapons/LaserBeamController.cs#L116-L146)
   - ✅ 修改 `GetHitPoint()` 使用 `origin.forward`
   - ✅ 實現物理 raycast 沿砲管方向
   
2. [`LaserBeamController.cs:148-174`](file:///Users/mac/Documents/UnityProjects/Tidal-Aegis/Assets/_Project/Scripts/Systems/Weapons/LaserBeamController.cs#L148-L174)
   - ✅ 修改 `ApplyDamage()` 驗證命中後才造成傷害
   - ✅ 實現「指向未命中則不開火」原則

### Code Changes Summary

```diff
LaserBeamController.cs
@@ GetHitPoint()
- direction = (target.position - origin.position).normalized
+ beamDirection = origin.forward
+ Physics.Raycast(ray along beamDirection)

@@ ApplyDamage()
+ if (!Physics.Raycast hits target) return;  // No damage if not aligned
```

## Verification Plan

### Test Scenarios

| 場景 | 預期行為 | 驗證方法 |
|------|----------|----------|
| 砲塔完全對齊目標 | 光束命中 + 造成傷害 | 觀察 HP 減少 |
| 砲塔旋轉中 | 光束脫靶 + 無傷害 | 確認目標 HP 不變 |
| 目標高速機動 | 光束可能脫靶 | 觀察光束軌跡 |
| 目標被障礙物遮擋 | 光束擊中障礙物 | 確認無穿牆傷害 |

### Runtime Verification

在遊戲中執行 "Rebuild World" 後：

1. 部署 LaserCIWS 艦船
2. 發射飛彈攻擊
3. 觀察雷射攔截行為：
   - ✅ 光束應沿砲管發射（視覺一致）
   - ✅ 砲塔對齊時成功攔截
   - ✅ 砲塔未對齊時光束脫靶

## Related Documents

- [Laser CIWS Investigation Report](file:///Users/mac/.gemini/antigravity/brain/7d33182b-5047-4a85-b064-513afa0912fa/laser_ciws_investigation.md) - 初始問題調查
- [Beam Direction Issue Diagnosis](file:///Users/mac/.gemini/antigravity/brain/7d33182b-5047-4a85-b064-513afa0912fa/laser_beam_direction_issue.md) - 技術問題診斷
- [WEAPON_SYSTEM.md](file:///Users/mac/Documents/UnityProjects/Tidal-Aegis/Documentation/WEAPON_SYSTEM.md) - 武器系統架構文檔

## Notes

此修正實現了用戶要求的核心原則：

> **「追蹤指向命中及開火命中，指向未命中則不開火」**

雷射現在是真正的物理光學武器，而非魔法追蹤系統。
