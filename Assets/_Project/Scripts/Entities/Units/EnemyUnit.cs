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
            
            // Ensure Collider exists and is tall enough
            var col = GetComponent<Collider>();
            if (col == null)
            {
                var capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.height = 4f; // Make it tall enough to catch projectiles
                capsule.center = new Vector3(0, 2f, 0); // Center it up
            }
            else if (col is CapsuleCollider cap)
            {
                // Adjust existing capsule if it's too short
                if (cap.height < 3f)
                {
                    cap.height = 4f;
                    cap.center = new Vector3(0, 2f, 0);
                }
            }
            else if (col is BoxCollider box)
            {
                if (box.size.y < 3f)
                {
                    box.size = new Vector3(box.size.x, 4f, box.size.z);
                    box.center = new Vector3(box.center.x, 2f, box.center.z);
                }
            }
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
