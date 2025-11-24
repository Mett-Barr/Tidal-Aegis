using UnityEngine;
using NavalCommand.Infrastructure;

namespace NavalCommand.Systems.VFX
{
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [SerializeField] private VFXLibrarySO _library;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            if (_library == null)
            {
                Debug.LogError("[VFXManager] VFX Library is missing!");
            }
        }

        public void SpawnVFX(ImpactPayload context)
        {
            if (_library == null) return;

            VFXRule rule = _library.GetBestRule(context);
            
            GameObject prefabToSpawn = null;
            AudioClip clipToPlay = null;
            float scale = 1f;

            if (rule != null)
            {
                prefabToSpawn = rule.VFXPrefab;
                clipToPlay = rule.SFXClip;
                scale = rule.ScaleMultiplier;
            }
            else
            {
                // Fallback
                prefabToSpawn = _library.FallbackVFX;
                clipToPlay = _library.FallbackSFX;
            }

            // Spawn Visuals
            if (prefabToSpawn != null)
            {
                if (PoolManager.Instance != null)
                {
                    GameObject vfx = PoolManager.Instance.Spawn(prefabToSpawn, context.Position, Quaternion.LookRotation(context.Normal));
                    vfx.transform.localScale = Vector3.one * scale;
                }
                else
                {
                    GameObject vfx = Instantiate(prefabToSpawn, context.Position, Quaternion.LookRotation(context.Normal));
                    vfx.transform.localScale = Vector3.one * scale;
                    Destroy(vfx, 5f); // Safety destroy if no pool
                }
            }

            // Play Audio
            if (clipToPlay != null)
            {
                AudioSource.PlayClipAtPoint(clipToPlay, context.Position);
            }
        }
    }
}
