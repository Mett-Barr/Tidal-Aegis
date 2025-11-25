namespace NavalCommand.VFX
{
    /// <summary>
    /// Trail VFX types for projectiles.
    /// Used for VFX pooling and spawning.
    /// </summary>
    public enum VFXType
    {
        None,
        MissileTrail,    // Flame + Smoke for missiles
        TorpedoBubbles,  // Bubbles for torpedoes
        TracerGlow,      // Glow trail for tracers (Autocannon)
        MuzzleFlash      // Yellow particle flash for weapon firing
    }
}
