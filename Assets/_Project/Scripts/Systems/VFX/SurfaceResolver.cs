using UnityEngine;

namespace NavalCommand.Systems.VFX
{
    public static class SurfaceResolver
    {
        public static SurfaceType Resolve(Collider collider)
        {
            if (collider == null) return SurfaceType.Default;

            // 1. Check Layer (Water -> Water)
            // Assuming "Water" layer is index 4 (standard) or named "Water"
            if (collider.gameObject.layer == LayerMask.NameToLayer("Water"))
            {
                return SurfaceType.Water;
            }

            // 2. Check Tag (Metal/Wood/etc)
            switch (collider.tag)
            {
                case "Water":
                    return SurfaceType.Water;
                case "Metal":
                case "Ship":
                case "Player":
                case "Enemy":
                    return SurfaceType.Armor_Metal;
                case "Wood":
                    return SurfaceType.Default; // Fallback for now
                default:
                    return SurfaceType.Armor_Metal; // Default for combatants
            }
        }
    }
}
