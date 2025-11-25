using UnityEngine;
using NavalCommand.Core;

namespace NavalCommand.Systems
{
    public class CameraSystem : MonoBehaviour
    {
        [Header("Follow Settings")]
        public Transform Target;
        public float Distance = 200f;
        public float Angle = 50f;
        public float SmoothSpeed = 0.05f; // Faster tracking to stay behind

        private Vector3 currentVelocity; // For SmoothDamp
        private Transform camTransform; // The transform we are actually moving

        private void Start()
        {
            // Default to moving this object
            camTransform = transform;

            // Validate settings to prevent division by zero or invalid states
            if (Distance <= 0.1f) Distance = 50f;
            if (Angle <= 0.1f) Angle = 60f;

            // Ensure this script is controlling a Camera
            if (GetComponent<Camera>() == null)
            {
                // Fallback: Control Camera.main if this script is attached to a Manager/Empty object
                if (Camera.main != null)
                {
                    camTransform = Camera.main.transform;
                    Debug.LogWarning($"CameraSystem: Attached to {name} (non-Camera). Auto-switching to control Main Camera.");
                }
                else
                {
                    Debug.LogError("CameraSystem: No Camera component found and no Main Camera in scene!");
                }
            }
        }

        [Header("Zoom Settings")]
        public float MinDistance = 20f;
        public float MaxDistance = 2000f;
        public float ZoomSensitivity = 5f;
        public float PanSpeed = 50f;

        private Vector3 manualOffset = Vector3.zero; // For arrow key panning

        private void LateUpdate()
        {
            // ... (Binding logic) ...
            // 1. Try to get from GameManager
            if (Target == null)
            {
                if (GameManager.Instance != null && GameManager.Instance.PlayerFlagship != null)
                {
                    Target = GameManager.Instance.PlayerFlagship.transform;
                    SnapToTarget();
                }
                // 2. Fallback: Try to find by Tag "Player"
                else
                {
                    GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                    if (playerObj != null)
                    {
                        Target = playerObj.transform;
                        SnapToTarget();
                    }
                }
            }

            if (Target == null) return;

            HandleInput();
            HandleMovement();
        }

        private void HandleInput()
        {
            // Zoom (Mouse Scroll + Keyboard +/-)
            float zoomDelta = 0f;
            if (Infrastructure.InputReader.Instance != null)
            {
                zoomDelta = Infrastructure.InputReader.Instance.ZoomDelta;
            }

            // Keyboard Zoom Support
            if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus)) zoomDelta += 0.1f;
            if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus)) zoomDelta -= 0.1f;

            if (Mathf.Abs(zoomDelta) > 0.001f)
            {
                Distance -= zoomDelta * ZoomSensitivity;
                Distance = Mathf.Clamp(Distance, MinDistance, MaxDistance);
            }

            // Pan (Arrow Keys)
            float h = Input.GetAxis("Horizontal"); // A/D or Left/Right
            float v = Input.GetAxis("Vertical");   // W/S or Up/Down
            
            // Only pan if Flagship movement is disabled (to avoid conflict), or just allow it always for debug
            // Since user asked to replace flagship control, we assume arrows now control camera pan
            if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f)
            {
                // Pan relative to camera look direction
                Vector3 forward = camTransform.forward;
                Vector3 right = camTransform.right;
                
                // Flatten to horizontal plane
                forward.y = 0; forward.Normalize();
                right.y = 0; right.Normalize();

                Vector3 moveDir = (right * h + forward * v).normalized;
                manualOffset += moveDir * PanSpeed * Time.deltaTime;
            }
        }

        public void SnapToTarget()
        {
            if (Target == null || camTransform == null) return;
            
            Vector3 desiredPos = CalculateDesiredPosition();
            camTransform.position = desiredPos;
            camTransform.LookAt(Target);
        }

        private void HandleMovement()
        {
            if (camTransform == null) return;

            Vector3 desiredPosition = CalculateDesiredPosition();
            
            // Use SmoothDamp for smoother camera movement
            camTransform.position = Vector3.SmoothDamp(camTransform.position, desiredPosition, ref currentVelocity, SmoothSpeed);
            
            // Explicitly calculate rotation
            Vector3 direction = (Target.position - camTransform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(direction);
                camTransform.rotation = lookRot;
            }
        }

        private Vector3 CalculateDesiredPosition()
        {
            float radianAngle = Angle * Mathf.Deg2Rad;
            float height = Mathf.Sin(radianAngle) * Distance;
            float backDist = Mathf.Cos(radianAngle) * Distance;

            // We want the camera BEHIND the target.
            // If Target.forward is (0,0,1), we want offset (0, height, -backDist).
            Vector3 offset = new Vector3(0, height, -backDist); 
            
            // Rotate this offset by the target's Y rotation to stay relative to the ship's facing
            Quaternion currentRotation = Quaternion.Euler(0, Target.eulerAngles.y, 0);
            Vector3 rotatedOffset = currentRotation * offset;
            
            return Target.position + rotatedOffset + manualOffset;
        }

        private void OnValidate()
        {
            if (Target != null)
            {
                HandleMovement();
            }
        }
    }
}
