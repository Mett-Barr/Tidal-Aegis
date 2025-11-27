# ADR-007: Smart Laser Firing Strategy Implementation

**Status:** Implemented  
**Date:** 2025-11-27  
**Deciders:** System Architecture Review + User Approval  

## Context

基於真實海軍雷射武器系統（LaWS/HELIOS）與知名遊戲設計（X4/Stellaris/FTL）的研究，需要實現智能雷射開火策略以解決兩個關鍵場景：

1. **超速目標**：當目標移速超過砲塔旋轉追蹤速度時的行為
2. **目標切換**：擊殺目標後的光束管理與重定向

## Decision

實施**精確駐留光束 (Precision Dwell Beam)** 策略：

### 核心原則

> **"Dwell Time" Doctrine**（駐留時間原則）  
> 雷射必須在同一瞄準點持續照射足夠時間才能累積熱量達成擊殺。如果砲塔無法追上目標，光束會在表面**掃過**而非**駐留**，無法造成有效傷害。

### 實現方案

#### 1. 追蹤能力檢測 (Tracking Capability Detection)

```csharp
// WeaponController.cs:256-295
private bool CanEffectivelyTrack(Transform target)
{
    // 計算目標橫向速度
    Vector3 lateralVelocity = Vector3.ProjectOnPlane(targetVelocity, toTarget.normalized);
    
    // 計算所需角速度（deg/s）
    float requiredAngularVel = Mathf.Rad2Deg * (lateralVelocity.magnitude / distance);
    
    // 與砲塔旋轉速度比較（20% 安全餘量）
    if (requiredAngularVel > maxTrackingSpeed * 1.2f)
    {
        return false;  // 無法維持有效駐留時間
    }
    
    return true;
}
```

#### 2. 智能停火邏輯 (Smart Cease-Fire)

```csharp
// WeaponController.cs:290-337
private void FireBeam(IDamageable target)
{
    // 檢測追蹤能力
    if (!CanEffectivelyTrack(targetTransform))
    {
        // 目標移速過快 - 停止發射避免能源浪費
        if (activeBeam != null && activeBeam.gameObject.activeSelf)
        {
            activeBeam.Deactivate();
        }
        return;
    }
    
    // 只有在能有效追蹤時才發射
    // ...
}
```

#### 3. 擊殺後冷卻重定向 (Cooldown-Based Retargeting)

```csharp
// LaserBeamController.cs:148-204
private void ApplyDamage(float deltaTime)
{
    if (wasAlive && isDeadNow)
    {
        SpawnExplosionVFX(potentialHitPoint);
        
        // 立即停用光束（視覺清晰）
        Deactivate();
        
        // WeaponController 會在 0.1s 冷卻後自動重定向新目標
    }
}
```

## Consequences

### Positive ✅

1. **符合物理真實性**：遵循真實雷射武器的駐留時間原則
2. **避免能源浪費**：無法追蹤時停火，不做無效掃射
3. **清晰視覺反饋**：擊殺後光束立即消失，玩家能清楚看到擊殺效果
4. **自動化火控**：冷卻後自動尋找新目標，無需手動介入
5. **戰術深度**：目標機動性成為雷射防禦的弱點，增加遊戲策略性

### Negative ⚠️

1. **對高機動目標效能較低**：飛彈末端轉向時可能完全無法命中
2. **複雜度增加**：需要即時計算角速度並比較

### Neutral 📊

1. **與 CIWS 差異化**：
   - **CIWS**：可預測射擊，即使追不上也能發射
   - **LaserCIWS**：必須精確追蹤，追不上就停火

## Implementation

### Modified Files

1. [`WeaponController.cs`](file:///Users/mac/Documents/UnityProjects/Tidal-Aegis/Assets/_Project/Scripts/Entities/Components/WeaponController.cs)
   - **L256-295**: 新增 `CanEffectivelyTrack()` 方法
   - **L290-337**: 修改 `FireBeam()` 添加追蹤檢測
   
2. [`LaserBeamController.cs`](file:///Users/mac/Documents/UnityProjects/Tidal-Aegis/Assets/_Project/Scripts/Systems/Weapons/LaserBeamController.cs)
   - **L148-204**: 修改 `ApplyDamage()` 立即停用光束

### Behavior Matrix

| 場景 | 行為 | 原則 |
|------|------|------|
| 砲塔成功追蹤 | ✅ 持續開火 + 累積傷害 | 駐留時間原則 |
| 砲塔無法追蹤（目標超速 20%） | ❌ 停止開火 | 避免能源浪費 |
| 目標被擊殺 | ⏸️ 立即停火 → 0.1s 冷卻 → 🔄 自動鎖定新目標 | 自動化火控 |
| 目標脫離射程 | ❌ 停止開火 | 範圍限制 |

## Verification Plan

### Test Scenarios

1. **高速機動目標測試**
   - 發射飛彈攻擊旗艦
   - 觀察飛彈末端轉向時雷射行為
   - ✅ 預期：雷射應停火（角速度超過追蹤能力）

2. **擊殺後重定向測試**
   - 同時發射多枚飛彈
   - 觀察雷射擊殺第一枚後的行為
   - ✅ 預期：光束立即消失，0.1秒後鎖定第二枚

3. **除錯日誌驗證**
   - 檢查 Console 輸出
   - ✅ 預期：每 2 秒顯示「Target too agile」訊息

### Runtime Verification

執行 "Rebuild World" 後測試：
```
1. 選擇 LaserCIWS 艦船
2. 發射飛彈群攻擊
3. 觀察雷射攔截行為
```

## References

- [Laser Firing Strategy Analysis](file:///Users/mac/.gemini/antigravity/brain/7d33182b-5047-4a85-b064-513afa0912fa/laser_firing_strategy_analysis.md) - 設計分析報告
- [ADR-006: Laser Beam Direction Fix](file:///Users/mac/Documents/UnityProjects/Tidal-Aegis/Documentation/ADR/ADR-006-laser-beam-direction-fix.md) - 光束方向修正
- US Navy LaWS/HELIOS - 真實系統參考
- X4: Foundations, Stellaris - 遊戲設計參考

## Notes

此實現完全基於用戶批准的設計方案，結合真實世界物理原則與遊戲設計最佳實踐。

雷射現在是**真正的精確能量武器**，而非萬能追蹤系統。
