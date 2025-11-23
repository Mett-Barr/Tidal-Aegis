using UnityEngine;
using NavalCommand.Core;
using NavalCommand.Infrastructure;
using NavalCommand.Systems;

namespace NavalCommand.Entities.Units
{
    [RequireComponent(typeof(Rigidbody))]
    public abstract class BaseUnit : MonoBehaviour, IDamageable
    {
        [Header("Base Stats")]
        public float MaxHP = 100f;
        public float CurrentHP;
        public Team UnitTeam;

        protected Rigidbody Rb;

        protected virtual void Awake()
        {
            Rb = GetComponent<Rigidbody>();
        }

        protected virtual void OnEnable()
        {
            CurrentHP = MaxHP;
            isRegistered = false;
            if (SpatialGridSystem.Instance != null)
            {
                SpatialGridSystem.Instance.Register(this, transform.position);
                isRegistered = true;
            }
        }

        protected virtual void OnDisable()
        {
            if (SpatialGridSystem.Instance != null && isRegistered)
            {
                SpatialGridSystem.Instance.Unregister(this);
            }
            isRegistered = false;
        }

        private bool isRegistered = false;

        protected virtual void Update()
        {
            // Retry registration if failed in OnEnable (e.g. GridSystem wasn't ready)
            if (!isRegistered)
            {
                if (SpatialGridSystem.Instance != null)
                {
                    SpatialGridSystem.Instance.Register(this, transform.position);
                    isRegistered = true;
                }
            }

            // Update grid position if moving
            if (isRegistered && SpatialGridSystem.Instance != null && Rb.velocity.sqrMagnitude > 0.1f)
            {
                SpatialGridSystem.Instance.UpdatePosition(this, transform.position);
            }
        }

        public virtual void TakeDamage(float amount)
        {
            CurrentHP -= amount;
            if (IsDead())
            {
                Die();
            }
        }

        public bool IsDead()
        {
            return CurrentHP <= 0;
        }

        public Team GetTeam()
        {
            return UnitTeam;
        }

        public virtual UnitType GetUnitType()
        {
            return UnitType.Ship;
        }

        protected virtual void Die()
        {
            // Use PoolManager instead of Destroy
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Despawn(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public abstract void Move();
    }
}
