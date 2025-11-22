using UnityEngine;
using NavalCommand.Core;

namespace NavalCommand.Entities.Units
{
    public class KamikazeController : BaseUnit
    {
        public float MoveSpeed = 15f;
        public float CollisionDamage = 50f;

        protected override void Awake()
        {
            base.Awake();
            UnitTeam = Team.Enemy;
        }

        public override void Move()
        {
            if (GameManager.Instance != null && GameManager.Instance.PlayerFlagship != null)
            {
                Vector3 direction = (GameManager.Instance.PlayerFlagship.transform.position - transform.position).normalized;
                Rb.velocity = direction * MoveSpeed;
                
                // Face movement
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }
            }
        }

        private void FixedUpdate()
        {
            Move();
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Check if we hit the player
            IDamageable target = collision.gameObject.GetComponent<IDamageable>();
            if (target != null && target.GetTeam() != UnitTeam)
            {
                target.TakeDamage(CollisionDamage);
                Die(); // Self-destruct
            }
        }
    }
}
