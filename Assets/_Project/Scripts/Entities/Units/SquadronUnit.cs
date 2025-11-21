using UnityEngine;
using NavalCommand.Core;

namespace NavalCommand.Entities.Units
{
    public class SquadronUnit : BaseUnit
    {
        [Header("Formation")]
        public Vector2 FormationOffset;
        public float SeparationDistance = 5f;

        private FlagshipController flagship;

        protected override void Awake()
        {
            base.Awake();
            UnitTeam = Team.Player;
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                flagship = GameManager.Instance.PlayerFlagship;
            }
        }

        private void FixedUpdate()
        {
            Move();
        }

        public override void Move()
        {
            if (flagship == null) return;

            // Calculate target position based on flagship position and rotation
            Vector3 targetPos = flagship.transform.position + 
                                flagship.transform.right * FormationOffset.x + 
                                flagship.transform.forward * FormationOffset.y;

            // Simple spring force to reach target
            Vector3 direction = (targetPos - transform.position).normalized;
            float distance = Vector3.Distance(targetPos, transform.position);
            
            if (distance > 0.1f)
            {
                Rb.AddForce(direction * distance * 10f); // Placeholder force
            }

            // TODO: Add Boid Separation here
        }
    }
}
