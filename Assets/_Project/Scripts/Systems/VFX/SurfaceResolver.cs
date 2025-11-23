using UnityEngine;
using NavalCommand.Entities.Units;
using NavalCommand.Core;

namespace NavalCommand.Systems.VFX
{
    public static class SurfaceResolver
    {
        // Cache Layer indices for performance
        private static int _waterLayer = -1;
        private static int _unitLayer = -1;
        // Force Recompile Timestamp: 1
        private static void InitializeLayers()
        {
            if (_waterLayer == -1) _waterLayer = LayerMask.NameToLayer("Water");
            if (_unitLayer == -1) _unitLayer = LayerMask.NameToLayer("Unit"); // Assuming 'Unit' or similar exists, fallback to check component
        }

        public static SurfaceType Resolve(Collider collider)
        {
            InitializeLayers();

            if (collider == null) return SurfaceType.Default;

            int layer = collider.gameObject.layer;

            // 1. Check Water
            if (layer == _waterLayer)
            {
                return SurfaceType.Water;
            }

            // 2. Check Unit/Armor
            // We can check layer, or look for IDamageable/BaseUnit component
            // 2. Check Unit/Armor
            // We can check layer, or look for IDamageable/BaseUnit component
            var unit = collider.GetComponentInParent<BaseUnit>();
            if (unit != null)
            {
                UnitType type = unit.GetUnitType();
                if (type == UnitType.Missile || type == UnitType.Aircraft)
                {
                    return SurfaceType.Air;
                }
                // In the future, we can check specific armor types on the unit
                return SurfaceType.Armor_Metal;
            }

            // 3. Check Tags (Fallback)
            // 3. Check Tags (Fallback) - Use .tag string comparison to avoid "Tag not defined" exception
            if (collider.tag == "Water") return SurfaceType.Water;
            if (collider.tag == "Player" || collider.tag == "Enemy") return SurfaceType.Armor_Metal;

            return SurfaceType.Default;
        }

        public static SurfaceType Resolve(int layer)
        {
            InitializeLayers();
            if (layer == _waterLayer) return SurfaceType.Water;
            return SurfaceType.Default;
        }
    }
}
