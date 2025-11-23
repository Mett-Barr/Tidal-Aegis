using UnityEngine;
using NavalCommand.Infrastructure;

namespace NavalCommand.Systems.VFX
{
    [RequireComponent(typeof(ParticleSystem))]
    public class VFXAutoDespawn : MonoBehaviour
    {
        private ParticleSystem _ps;
        private bool _isDespawning = false;

        private void Awake()
        {
            _ps = GetComponent<ParticleSystem>();
        }

        private void OnEnable()
        {
            _isDespawning = false;
            if (_ps != null)
            {
                _ps.Play();
            }
        }

        private void Update()
        {
            if (_isDespawning) return;

            if (_ps != null && !_ps.IsAlive(true))
            {
                Despawn();
            }
        }

        private void Despawn()
        {
            _isDespawning = true;
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Despawn(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
