using UnityEngine;
using NavalCommand.Infrastructure;

namespace NavalCommand.Entities.Units
{
    public class FlagshipController : BaseUnit
    {
        [Header("Movement Settings")]
        public float MoveSpeed = 5f;
        public float TurnSpeed = 2f;

        protected override void Awake()
        {
            base.Awake();
            UnitTeam = Core.Team.Player;
        }

        private void Start()
        {
            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.PlayerFlagship = this;
            }
            else
            {
                Debug.LogError("FlagshipController: GameManager Instance is NULL! Cannot register.");
            }
        }

        private void FixedUpdate()
        {
            Move();
        }

        public override void Move()
        {
            if (InputReader.Instance == null) return;

            Vector2 input = InputReader.Instance.MoveDirection;
            Vector3 movement = new Vector3(input.x, 0, input.y) * MoveSpeed * Time.fixedDeltaTime;
            
            // Physics-based movement
            Rb.MovePosition(Rb.position + movement);

            if (movement != Vector3.zero)
            {
                Quaternion toRotation = Quaternion.LookRotation(movement, Vector3.up);
                Rb.rotation = Quaternion.RotateTowards(Rb.rotation, toRotation, TurnSpeed * Time.fixedDeltaTime);
            }
        }

        protected override void Die()
        {
            // Flagship death logic: Game Over
            Debug.Log("Flagship Destroyed! Game Over.");
            
            if (Core.GameManager.Instance != null)
            {
                // Trigger Game Over state in GameManager (need to implement SetGameOver)
                // For now, just pause
                Core.GameManager.Instance.TogglePause();
            }
            
            // Do NOT call base.Die() because we don't want to Despawn/Destroy the player object immediately
            // or we might want to play an explosion effect then disable the mesh.
            gameObject.SetActive(false);
        }
    }
}
