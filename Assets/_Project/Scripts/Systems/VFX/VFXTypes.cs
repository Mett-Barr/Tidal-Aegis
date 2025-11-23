using UnityEngine;

namespace NavalCommand.Systems.VFX
{
    public enum ImpactCategory
    {
        Kinetic,    // Bullets, Shells
        Explosive,  // HE Shells, Missiles
        Energy,     // Lasers, Plasma (Future)
        EMP         // Electronic Warfare (Future)
    }

    public enum ImpactSize
    {
        Small,      // 20-30mm
        Medium,     // 76-127mm
        Large,      // 200mm+
        Massive     // Anti-Ship Missiles, Torpedoes
    }

    public enum SurfaceType
    {
        Default,
        Water,
        Armor_Metal,
        Armor_Composite, // Future
        Shield,          // Future
        Air              // For airbursts/misses
    }

    [System.Serializable]
    public struct ImpactProfile
    {
        public ImpactCategory Category;
        public ImpactSize Size;

        public ImpactProfile(ImpactCategory category, ImpactSize size)
        {
            Category = category;
            Size = size;
        }
    }

    public struct HitContext
    {
        public ImpactProfile Impact;
        public SurfaceType Surface;
        public Vector3 Position;
        public Vector3 Normal;
        
        // Flags for special handling
        public bool IsRicochet;
        public bool IsAirburst;
        public bool IsDud;

        public HitContext(ImpactProfile impact, SurfaceType surface, Vector3 position, Vector3 normal)
        {
            Impact = impact;
            Surface = surface;
            Position = position;
            Normal = normal;
            IsRicochet = false;
            IsAirburst = false;
            IsDud = false;
        }
    }
}
