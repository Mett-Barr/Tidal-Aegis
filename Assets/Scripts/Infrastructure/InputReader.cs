using System;
using UnityEngine;

namespace NavalCommand.Infrastructure
{
    public class InputReader : MonoBehaviour
    {
        public static InputReader Instance { get; private set; }

        public Vector2 MoveDirection { get; private set; }
        public bool IsFiring { get; private set; }
        public float ZoomDelta { get; private set; }

        public event Action OnPausePressed;

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
        }

        private void Update()
        {
            HandleMovement();
            HandleCombat();
            HandleCamera();
            HandleSystem();
        }

        private void HandleMovement()
        {
            // Legacy Input for now, can be swapped for New Input System here
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");
            MoveDirection = new Vector2(x, y).normalized;
        }

        private void HandleCombat()
        {
            IsFiring = Input.GetButton("Fire1") || Input.GetKey(KeyCode.Space);
        }

        private void HandleCamera()
        {
            ZoomDelta = Input.GetAxis("Mouse ScrollWheel");
        }

        private void HandleSystem()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnPausePressed?.Invoke();
            }
        }
    }
}
