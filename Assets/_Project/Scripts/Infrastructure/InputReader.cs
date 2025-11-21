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

        public event Action OnThrottleUp;
        public event Action OnThrottleDown;
        public event Action OnRudderLeft;
        public event Action OnRudderRight;

        private void HandleMovement()
        {
            // Continuous (Legacy)
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");
            MoveDirection = new Vector2(x, y).normalized;

            // Discrete Events (WoWS Style)
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) OnThrottleUp?.Invoke();
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) OnThrottleDown?.Invoke();
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) OnRudderLeft?.Invoke();
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) OnRudderRight?.Invoke();
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
