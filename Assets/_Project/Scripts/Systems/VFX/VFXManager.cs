using UnityEngine;
using NavalCommand.Infrastructure;
using System.Collections.Generic;

namespace NavalCommand.Systems.VFX
{
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [SerializeField] private VFXLibrarySO _library;
        
        [Header("Trail VFX Prefabs")]
        [SerializeField] private GameObject _missileTrailPrefab;
        [SerializeField] private GameObject _torpedoBubblesPrefab;
        [SerializeField] private GameObject _tracerGlowPrefab;
        [SerializeField] private GameObject _muzzleFlashPrefab;  // NEW: Yellow particle flash
        
        [Header("Pool Settings")]
        [SerializeField] private int _trailVFXPoolSize = 20; // Per type

        // Trail VFX Pools
        private Dictionary<NavalCommand.VFX.VFXType, Queue<GameObject>> _trailVFXPools;
        private Dictionary<NavalCommand.VFX.VFXType, GameObject> _trailVFXPrefabs;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeTrailVFXPools();
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

        private void InitializeTrailVFXPools()
        {
            _trailVFXPools = new Dictionary<NavalCommand.VFX.VFXType, Queue<GameObject>>();
            _trailVFXPrefabs = new Dictionary<NavalCommand.VFX.VFXType, GameObject>();
            
            // Register prefabs
            if (_missileTrailPrefab != null)
            {
                _trailVFXPrefabs[NavalCommand.VFX.VFXType.MissileTrail] = _missileTrailPrefab;
                _trailVFXPools[NavalCommand.VFX.VFXType.MissileTrail] = new Queue<GameObject>();
            }
            if (_torpedoBubblesPrefab != null)
            {
                _trailVFXPrefabs[NavalCommand.VFX.VFXType.TorpedoBubbles] = _torpedoBubblesPrefab;
                _trailVFXPools[NavalCommand.VFX.VFXType.TorpedoBubbles] = new Queue<GameObject>();
            }
            if (_tracerGlowPrefab != null)
            {
                _trailVFXPrefabs[NavalCommand.VFX.VFXType.TracerGlow] = _tracerGlowPrefab;
                _trailVFXPools[NavalCommand.VFX.VFXType.TracerGlow] = new Queue<GameObject>();
            }
            if (_muzzleFlashPrefab != null)
            {
                _trailVFXPrefabs[NavalCommand.VFX.VFXType.MuzzleFlash] = _muzzleFlashPrefab;
                _trailVFXPools[NavalCommand.VFX.VFXType.MuzzleFlash] = new Queue<GameObject>();
            }
            
            // Pre-warm pools
            PrewarmTrailVFXPools();
        }

        private void PrewarmTrailVFXPools()
        {
            foreach (var kvp in _trailVFXPrefabs)
            {
                var vfxType = kvp.Key;
                var prefab = kvp.Value;
                var pool = _trailVFXPools[vfxType];
                
                for (int i = 0; i < _trailVFXPoolSize; i++)
                {
                    GameObject vfx = Instantiate(prefab, transform);
                    vfx.SetActive(false);
                    pool.Enqueue(vfx);
                }
            }
            
            Debug.Log($"[VFX_DEBUG] VFXManager initialized: {_trailVFXPrefabs.Count} types, {_trailVFXPoolSize} each");
        }

        /// <summary>
        /// Spawn a trail VFX that follows a target.
        /// </summary>
        public GameObject SpawnTrailVFX(NavalCommand.VFX.VFXType vfxType, Transform target)
        {
            if (vfxType == NavalCommand.VFX.VFXType.None) return null;
            if (!_trailVFXPools.ContainsKey(vfxType))
            {
                Debug.LogError($"[VFX_DEBUG] ERROR: VFX type {vfxType} not registered in pool!");
                return null;
            }

            GameObject vfx = GetFromTrailPool(vfxType);
            if (vfx == null)
            {
                Debug.LogError($"[VFX_DEBUG] ERROR: Failed to get {vfxType} from pool!");
                return null;
            }

            vfx.SetActive(true);
            vfx.transform.position = target.position;
            vfx.transform.rotation = target.rotation;

            // Setup auto-follow and recycling
            var autoFollow = vfx.GetComponent<NavalCommand.VFX.AutoFollowVFX>();
            if (autoFollow != null)
            {
                autoFollow.StartFollowing(target);
                
                // Register recycling callback
                autoFollow.OnDetached = () => ReturnToTrailPool(vfx, vfxType);
            }
            else
            {
                Debug.LogError($"[VFX_DEBUG] ERROR: {vfx.name} missing AutoFollowVFX component!");
            }

            return vfx;
        }

        private GameObject GetFromTrailPool(NavalCommand.VFX.VFXType vfxType)
        {
            var pool = _trailVFXPools[vfxType];
            
            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }
            else
            {
                // Pool exhausted, create new instance
                Debug.LogWarning($"[VFXManager] Trail VFX pool exhausted for {vfxType}, creating new instance");
                GameObject vfx = Instantiate(_trailVFXPrefabs[vfxType], transform);
                return vfx;
            }
        }

        private void ReturnToTrailPool(GameObject vfx, NavalCommand.VFX.VFXType vfxType)
        {
            if (vfx == null) return;
            
            vfx.SetActive(false);
            vfx.transform.SetParent(transform);
            _trailVFXPools[vfxType].Enqueue(vfx);
        }

        /// <summary>
        /// Public method for VFX to return themselves to pool (used by AutoRecycleVFX).
        /// </summary>
        public void ReturnVFXToPool(GameObject vfx, NavalCommand.VFX.VFXType vfxType)
        {
            ReturnToTrailPool(vfx, vfxType);
        }

        /// <summary>
        /// Spawn muzzle flash VFX at position (one-shot effect, auto-recycles).
        /// </summary>
        public GameObject SpawnMuzzleFlashVFX(Vector3 position, Quaternion rotation)
        {
            if (!_trailVFXPools.ContainsKey(NavalCommand.VFX.VFXType.MuzzleFlash))
            {
                Debug.LogError("[VFX_DEBUG] ERROR: MuzzleFlash not registered! Check VFXManager Inspector: _muzzleFlashPrefab field must be assigned!");
                return null;
            }

            GameObject vfx = GetFromTrailPool(NavalCommand.VFX.VFXType.MuzzleFlash);
            if (vfx == null) return null;

            vfx.transform.position = position;
            vfx.transform.rotation = rotation;
            vfx.SetActive(true);

            // AutoRecycleVFX component will handle returning to pool
            var autoRecycle = vfx.GetComponent<NavalCommand.VFX.AutoRecycleVFX>();
            if (autoRecycle == null)
            {
                Debug.LogWarning("[VFX_DEBUG] MuzzleFlash VFX missing AutoRecycleVFX component!");
            }

            return vfx;
        }

        /// <summary>
        /// Spawn impact VFX (existing system, uses PoolManager).
        /// </summary>
        public void SpawnVFX(ImpactPayload context)
        {
            if (_library == null)
            {
                Debug.LogError("[DEBUG_VFX] SpawnVFX called but _library is NULL!");
                return;
            }

            VFXRule rule = _library.GetBestRule(context);
            
            GameObject prefabToSpawn = null;
            AudioClip clipToPlay = null;
            float scale = 1f;

            if (rule != null)
            {
                prefabToSpawn = rule.VFXPrefab;
                clipToPlay = rule.SFXClip;
                scale = rule.ScaleMultiplier;
                Debug.Log($"[DEBUG_VFX] Found rule for {context.Impact.Category}/{context.Impact.Size}, prefab={prefabToSpawn?.name}");
            }
            else
            {
                // Fallback
                prefabToSpawn = _library.FallbackVFX;
                clipToPlay = _library.FallbackSFX;
                Debug.LogWarning($"[DEBUG_VFX] No rule found for {context.Impact.Category}/{context.Impact.Size}, using fallback");
            }

            // Spawn Visuals
            if (prefabToSpawn != null)
            {
                Debug.Log($"[DEBUG_VFX] Spawning VFX: {prefabToSpawn.name} at {context.Position}");
                if (PoolManager.Instance != null)
                {
                    GameObject vfx = PoolManager.Instance.Spawn(prefabToSpawn, context.Position, Quaternion.LookRotation(context.Normal));
                    vfx.transform.localScale = Vector3.one * scale;
                    Debug.Log($"[DEBUG_VFX] VFX spawned from pool: {vfx.name}, active={vfx.activeSelf}");
                }
                else
                {
                    GameObject vfx = Instantiate(prefabToSpawn, context.Position, Quaternion.LookRotation(context.Normal));
                    vfx.transform.localScale = Vector3.one * scale;
                    Destroy(vfx, 5f); // Safety destroy if no pool
                    Debug.Log($"[DEBUG_VFX] VFX instantiated (no pool): {vfx.name}");
                }
            }
            else
            {
                Debug.LogError($"[DEBUG_VFX] No VFX prefab to spawn for {context.Impact.Category}/{context.Impact.Size}!");
            }

            // Play Audio
            if (clipToPlay != null)
            {
                AudioSource.PlayClipAtPoint(clipToPlay, context.Position);
            }
        }
    }
}
