using UnityEngine;
using NavalCommand.Core;

namespace NavalCommand.Entities.Units
{
    public class EnemyUnit : BaseUnit
    {
        [Header("AI Settings")]
        public float ChaseRange = 50f;
        public float AttackRange = 20f;
        public bool IsElite = false;

        [Header("Movement Settings")]
        public float MoveSpeed = 7f; // Faster than Flagship (5f)

        private Transform target;

        protected override void Awake()
        {
            base.Awake();
            UnitTeam = Team.Enemy;
        }

        private void Start()
        {
            // Find target (Player Flagship)
            if (GameManager.Instance != null && GameManager.Instance.PlayerFlagship != null)
            {
                target = GameManager.Instance.PlayerFlagship.transform;
            }
        }

        private void FixedUpdate()
        {
            Move();
        }

        public override void Move()
        {
            if (target == null) return;

            float distance = Vector3.Distance(transform.position, target.position);

            if (distance > AttackRange)
            {
                // Chase
                Vector3 direction = (target.position - transform.position).normalized;
                Rb.MovePosition(transform.position + direction * MoveSpeed * Time.fixedDeltaTime);
                transform.LookAt(target);
            }
            else
            {
                // Orbit or Stop (Placeholder)
            }
        }
    }
}
