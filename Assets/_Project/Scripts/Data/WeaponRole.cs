namespace NavalCommand.Data
{
    /// <summary>
    /// Weapon Role: Tactical function and platform constraints (orthogonal to Payload)
    /// </summary>
    public enum WeaponRole
    {
        MainGun,         // Primary battery: long-range, heavy damage, slow traversal
        Secondary,       // Secondary battery: medium-range sustained fire
        PointDefense,    // CIWS: short-range, high tracking, anti-missile/aircraft
        MissileLauncher, // VLS or guided missile platform
        TorpedoLauncher  // Underwater guided munitions
    }
}
