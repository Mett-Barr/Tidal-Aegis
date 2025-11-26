using UnityEngine;
using System.Collections.Generic;

namespace NavalCommand.Systems.Weapons
{
    /// <summary>
    /// Object pool for laser beam instances.
    /// Manages creation and recycling of LaserBeamController objects.
    /// </summary>
    public class LaserBeamPool : MonoBehaviour
    {
        public static LaserBeamPool Instance { get; private set; }
        
        [SerializeField] private GameObject beamPrefab;
        private Queue<LaserBeamController> pool = new Queue<LaserBeamController>();
        private const int INITIAL_POOL_SIZE = 10;
        
        /// <summary>
        /// Auto-initialize pool on game start
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // Create LaserBeamPool GameObject if it doesn't exist
            if (Instance == null)
            {
                GameObject poolObj = new GameObject("LaserBeamPool");
                Instance = poolObj.AddComponent<LaserBeamPool>();
                DontDestroyOnLoad(poolObj);
                Debug.Log("[LaserBeamPool] Auto-initialized");
            }
        }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            InitializePool();
        }
        
        private void InitializePool()
        {
            // Create beam prefab if not assigned
            if (beamPrefab == null)
            {
                beamPrefab = CreateBeamPrefab();
            }
            
            // Pre-populate pool
            for (int i = 0; i < INITIAL_POOL_SIZE; i++)
            {
                CreateNewBeam();
            }
        }
        
        private GameObject CreateBeamPrefab()
        {
            GameObject prefab = new GameObject("LaserBeam_Prefab");
            prefab.AddComponent<LaserBeamController>();
            prefab.SetActive(false);
            return prefab;
        }
        
        private LaserBeamController CreateNewBeam()
        {
            GameObject beamObj = Instantiate(beamPrefab, transform);
            beamObj.name = $"LaserBeam_{pool.Count}";
            LaserBeamController beam = beamObj.GetComponent<LaserBeamController>();
            beamObj.SetActive(false);
            pool.Enqueue(beam);
            return beam;
        }
        
        public LaserBeamController GetBeam()
        {
            LaserBeamController beam;
            
            if (pool.Count > 0)
            {
                beam = pool.Dequeue();
            }
            else
            {
                // Pool exhausted, create new beam
                beam = CreateNewBeam();
                pool.Dequeue(); // Remove from pool since we're returning it
            }
            
            beam.gameObject.SetActive(true);
            return beam;
        }
        
        public void ReturnBeam(LaserBeamController beam)
        {
            if (beam == null) return;
            
            beam.Deactivate();
            beam.gameObject.SetActive(false);
            pool.Enqueue(beam);
        }
    }
}
