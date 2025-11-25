using UnityEngine;

namespace NavalCommand.VFX
{
    /// <summary>
    /// Auto-recycles VFX GameObject to VFXManager pool after a delay.
    /// Used for short-lived effects like muzzle flashes.
    /// </summary>
    public class AutoRecycleVFX : MonoBehaviour
    {
        public float RecycleDelay = 0.15f;
        public VFXType VFXTypeToRecycle = VFXType.MuzzleFlash;

        private void OnEnable()
        {
            Invoke(nameof(Recycle), RecycleDelay);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(Recycle));
        }

        private void Recycle()
        {
            // Return to VFXManager pool
            if (NavalCommand.Systems.VFX.VFXManager.Instance != null)
            {
                NavalCommand.Systems.VFX.VFXManager.Instance.ReturnVFXToPool(gameObject, VFXTypeToRecycle);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
