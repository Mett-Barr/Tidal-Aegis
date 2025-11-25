using UnityEngine;

namespace NavalCommand.Entities.Components
{
    /// <summary>
    /// Handles the rotation of a turret to face a target, compensating for FirePoint offsets.
    /// Decoupled from WeaponController to simplify logic and improve testability.
    /// </summary>
    public class TurretRotator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _azimuthTransform;   // Base (Yaw)
        [SerializeField] private Transform _elevationTransform; // Gun (Pitch)
        [SerializeField] private Transform _firePoint;

        [Header("Settings")]
        public float MinPitch = -10f;
        public float MaxPitch = 85f;
        public bool CanRotate = true;
        public bool IsVerticalLaunch = false;

        [Header("Runtime State")]
        private float _currentYawVelocity;
        private float _currentPitchVelocity;
        public float RotationAcceleration = 1000f; // Default if not set

        public void Initialize(Transform azimuth, Transform elevation, Transform firePoint)
        {
            _azimuthTransform = azimuth;
            _elevationTransform = elevation;
            _firePoint = firePoint;
        }

        public void AimAt(Vector3 targetPosition, float rotationSpeed)
        {
            if (!CanRotate || _azimuthTransform == null || _elevationTransform == null) return;

            // ---------------------------------------------------------
            // 1. Azimuth (Yaw) - Rotate Base around Y axis
            // ---------------------------------------------------------
            Vector3 targetDir = targetPosition - _azimuthTransform.position;
            Vector3 targetDirFlattened = new Vector3(targetDir.x, 0, targetDir.z);
            
            if (targetDirFlattened.sqrMagnitude > 0.001f)
            {
                Quaternion targetYaw = Quaternion.LookRotation(targetDirFlattened);
                // REVERT: Back to Linear Rotation for precision
                _azimuthTransform.rotation = Quaternion.RotateTowards(_azimuthTransform.rotation, targetYaw, rotationSpeed * Time.deltaTime);
            }

            // ---------------------------------------------------------
            // 2. Elevation (Pitch) - Rotate Gun around X axis (Local)
            // ---------------------------------------------------------
            Vector3 targetVector = targetPosition - _elevationTransform.position;
            Vector3 localTargetVector = _azimuthTransform.InverseTransformDirection(targetVector);

            float flatDistance = Mathf.Sqrt(localTargetVector.x * localTargetVector.x + localTargetVector.z * localTargetVector.z);
            float targetPitchAngle = Mathf.Atan2(localTargetVector.y, flatDistance) * Mathf.Rad2Deg;
            
            // Clamp pitch (Note: Negative because X-axis is inverted)
            targetPitchAngle = Mathf.Clamp(-targetPitchAngle, -MaxPitch, -MinPitch);
            
            Quaternion targetPitchRot = Quaternion.Euler(targetPitchAngle, 0, 0);
            // REVERT: Back to Linear Rotation for precision
            _elevationTransform.localRotation = Quaternion.RotateTowards(_elevationTransform.localRotation, targetPitchRot, rotationSpeed * Time.deltaTime);
        }

        public bool IsAligned(Vector3 targetPosition, float angleTolerance)
        {
            if (IsVerticalLaunch) return true;
            
            // Option A+: Pivot-Centric Alignment Check
            // We check if the GUN PIVOT is facing the target, ignoring the physical offset of the muzzle.
            // This prevents "Parallax Error" where the gun is aimed correctly but the muzzle offset makes the angle > tolerance.
            
            Transform checkTransform = _elevationTransform; 
            if (checkTransform == null) checkTransform = _firePoint; // Fallback
            if (checkTransform == null) return true; // Should not happen if initialized

            Vector3 directionToTarget = (targetPosition - checkTransform.position).normalized;
            float angle = Vector3.Angle(checkTransform.forward, directionToTarget);

            // Debug firing alignment (Optional, can be removed later)
            // if (angle > angleTolerance && Time.frameCount % 60 == 0)
            // {
            //    Debug.Log($"[{gameObject.name}] Not Aligned: Angle {angle:F1}° > Tol {angleTolerance:F1}°");
            // }

            return angle < angleTolerance;
        }
    }
}
