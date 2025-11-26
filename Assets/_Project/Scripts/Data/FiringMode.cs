namespace NavalCommand.Data
{
    /// <summary>
    /// Firing mode: How the weapon delivers damage
    /// </summary>
    public enum FiringMode
    {
        Projectile,  // Spawns projectile object (ballistic, missile, torpedo, tracer)
        Beam         // Continuous beam (laser) - instant raycast, DOT damage
    }
}
