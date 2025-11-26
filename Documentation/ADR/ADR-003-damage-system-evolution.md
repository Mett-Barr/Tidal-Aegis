# ADR-003: Damage System Evolution - Multi-Channel Roadmap

**Status**: ✅ Accepted  
**Date**: 2025-11-27  
**Deciders**: Project Team  
**Related**: Laser Interception VFX Fix, Game Design Depth

---

## Context

### Problem
- Laser beam weapons cannot trigger explosion VFX on missile kill
- Current one-hit-kill damage model prevents reliable `wasAlive` detection
- Game needs tactical depth: laser vs HE/AP differentiation

### Immediate Need
- Fix laser VFX **today** (30 minutes)
- Cannot wait 2-3 weeks for full architecture

###Future Vision
- Multi-Channel Damage Model:
  - Kinetic/Thermal/Explosive channels
  - Structural vs Volatile damage pools
  - Natural emergence of complex interactions

---

## Decision

**Adopt 3-Phase Evolution Plan**:

### Phase 1: Simple HP System (Today) ✅
**Time**: 30 minutes  
**Goal**: Fix laser VFX immediately

```csharp
// Projectile becomes health-based instead of one-hit-kill
private float currentHP;
public void TakeDamage(float amount) {
    currentHP -= amount;
    if (currentHP <= 0) Despawn();
}
```

**Benefits**:
- ✅ Fixes `wasAlive` detection
- ✅ Enables cumulative damage gameplay
- ✅ Foundation for future phases

---

### Phase 2: Dual-Pool Prototype (1 Week Later)
**Time**: 3-5 days  
**Goal**: Validate Multi-Channel concept

```csharp
// Structural vs Volatile pools
private float structuralHP;
private float volatileAccumulation;

public void TakeDamage(float amount, DamageType type) {
    if (type == Thermal) {
        volatileAccumulation += amount * thermalSensitivity;
        structuralHP -= amount * 0.2f;
    } else if (type == Kinetic) {
        structuralHP -= amount;
    }
}
```

**Benefits**:
- ✅ Proves laser-induced cook-off concept
- ✅ Identifies configuration challenges
- ✅ Low risk (small scope)

---

### Phase 3: Full Multi-Channel (2-3 Weeks Later)
**Time**: 2-3 weeks  
**Goal**: Complete architecture

```csharp
// Attack × Target × Context → Result
public static DamageResult ApplyDamage(
    AttackProfile attack,    // Kinetic/Thermal/Explosive channels
    TargetProfile target,    // Structural/Volatile vulnerabilities
    HitContext context       // Velocity/Angle/Exposure
) {
    // Multi-channel damage calculation
    // Natural emergence of complex behaviors
}
```

**Benefits**:
- ✅ Perfect abstraction (no hardcoded rules)
- ✅ Unlimited扩展性
- ✅ Physical realism

---

## Consequences

### Positive
- ✅ Immediate problem solved (Phase 1)
- ✅ Incremental validation (Phase 2)
- ✅ Long-term architecture excellence (Phase 3)
- ✅ Low risk (can stop at any phase)

### Negative
- ⚠️ Temporary technical debt (Phase 1 is simple)
- ⚠️ Need migration work (Phase 1→2→3)
- ⚠️ Configuration complexity increases

### Risks Mitigated
- ❌ **Avoided**: 3-week delay for laser fix
- ❌ **Avoided**: Big-bang architecture change
- ❌ **Avoided**: Over-engineering without validation

---

## Implementation Status

### Phase 1: ✅ COMPLETE
- [x] ProjectileBehavior HP system
- [x] Damage balancing (Laser: 2.5 DPS, CIWS: 2 HP/hit)
- [x] **Critical Fix**: Race condition in Despawn (removed isDespawning early-set in TakeDamage)
- [x] Verification testing (laser + CIWS interception working)

**Results**:
- ✅ Laser explosion VFX now triggers correctly
- ✅ Missiles properly despawn after HP depletion
- ✅ 2-second kill time for standard 5 HP missiles

**Key Lesson**: Avoid setting `isDespawning` flag before calling `Despawn()` - causes early-return race condition.

---

### Phase 2: 🔜 Planned (1 Week)
- [ ] Dual-pool prototype (Structural + Volatile)
- [ ] Thermal damage differentiation
- [ ] Balance data collection

### Phase 3: 📅 Scheduled (2-3 Weeks)
- [ ] AttackProfile system
- [ ] TargetProfile system
- [ ] DamageCalculator
- [ ] Full migration

---

## References

- **Multi-Channel Proposal**: See `/brain/multi_channel_evaluation.md`
- **CIWS Analysis**: See `/brain/ciws_laser_interception_analysis.md`
- **Phase 1 Plan**: See `/brain/implementation_plan.md`

---

**Next Review**: After Phase 1 completion (today)
