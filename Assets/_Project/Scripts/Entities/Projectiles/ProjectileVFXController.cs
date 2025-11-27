using UnityEngine;

namespace NavalCommand.Entities.Projectiles
{
    /// <summary>
    /// Simplified VFX controller using unified VFX Pool architecture.
    /// Spawns trail VFX via VFXManager pooling system.
    /// </summary>
    public class ProjectileVFXController : MonoBehaviour
    {
        [Header("VFX Configuration")]
        [Tooltip("Type of trail VFX for this projectile")]
        public NavalCommand.VFX.VFXType VFXType = NavalCommand.VFX.VFXType.None;

        private GameObject _activeVFX;

        /// <summary>
        /// Called when the projectile is fired.
        /// Spawns trail VFX from VFXManager pool.
        /// </summary>
        public void OnLaunch()
        {
            Debug.Log($"[VFX_DEBUG] Projectile OnLaunch: {gameObject.name}, VFXType={VFXType}");
            
            if (VFXType == NavalCommand.VFX.VFXType.None)
            {
                Debug.LogWarning($"[VFX_DEBUG] VFXType is NONE for {gameObject.name} - no trail will spawn!");
                return;
            }

            // Spawn VFX
            if (NavalCommand.Systems.VFX.VFXManager.Instance != null)
            {
                Debug.Log($"[VFX_DEBUG] Spawning {VFXType} trail for {gameObject.name}");
                _activeVFX = NavalCommand.Systems.VFX.VFXManager.Instance.SpawnTrailVFX(VFXType, transform);
                
                if (_activeVFX == null)
                {
                    Debug.LogError($"[VFX_DEBUG] ERROR: Failed to spawn {VFXType} trail!");
                    Debug.LogError($"[VFX_DEBUG] VFXManager state - check if prefabs are assigned in Inspector!");
                }
                else
                {
                    Debug.Log($"[VFX_DEBUG] Successfully spawned {VFXType} trail: {_activeVFX.name}");
                }
            }
            else
            {
                Debug.LogError("[VFX_DEBUG] ERROR: VFXManager.Instance is null!");
            }
        }

        /// <summary>
        /// Called when the projectile hits something or expires.
        /// VFX will automatically detach, fade, and return to pool.
        /// </summary>
        public void OnImpact()
        {
            if (_activeVFX == null) return;

            // Detach VFX (it will auto-recycle via event)
            var autoFollow = _activeVFX.GetComponent<NavalCommand.VFX.AutoFollowVFX>();
            if (autoFollow != null)
            {
                autoFollow.DetachAndFade();
            }

            // Clear reference (VFX will recycle itself)
            _activeVFX = null;
        }

        /// <summary>
        /// Reset for object pooling.
        /// </summary>
        public void Reset()
        {
            // If there's still an active VFX (projectile pooled before impact),
            // detach it properly
            if (_activeVFX != null)
            {
                var autoFollow = _activeVFX.GetComponent<NavalCommand.VFX.AutoFollowVFX>();
                if (autoFollow != null)
                {
                    autoFollow.DetachAndFade();
                }
                _activeVFX = null;
            }
        }

        private void OnDestroy()
        {
            // Cleanup: if projectile destroyed, detach VFX
            if (_activeVFX != null)
            {
                var autoFollow = _activeVFX.GetComponent<NavalCommand.VFX.AutoFollowVFX>();
                if (autoFollow != null)
                {
                    autoFollow.DetachAndFade();
                }
            }
        }
    }
}
