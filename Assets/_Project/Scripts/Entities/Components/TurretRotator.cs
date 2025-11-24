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
        [SerializeField] private Transform _turretBase;
        [SerializeField] private Transform _firePoint;

        [Header("Debug")]
        [SerializeField] private bool _drawDebugGizmos = true;
        private Vector3? _lastTargetPos;

        [Header("Platform Capabilities")]
        public bool CanRotate = true;
        public bool IsVerticalLaunch = false;

        public void Initialize(Transform turretBase, Transform firePoint)
        {
            _turretBase = turretBase;
            _firePoint = firePoint;
        }

        /// <summary>
        /// Rotates the turret so that the FirePoint faces the target position.
        /// </summary>
        public void AimAt(Vector3 targetPosition, float rotationSpeed)
        {
            if (!CanRotate || _turretBase == null || _firePoint == null) return;
            
            _lastTargetPos = targetPosition;

            Vector3 directionToTarget = (targetPosition - _firePoint.position).normalized;
            if (directionToTarget == Vector3.zero) return;

            // Calculate target rotation for the FirePoint to face the target
            Quaternion targetFirePointRotation = Quaternion.LookRotation(directionToTarget);
            
            // Apply the inverse of the FirePoint's local rotation to find the needed Turret rotation
            // Turret * Local = Target => Turret = Target * Inverse(Local)
            // This compensates for any local rotation offset (e.g. -90 degrees) on the FirePoint
            Quaternion targetTurretRotation = targetFirePointRotation * Quaternion.Inverse(_firePoint.localRotation);

            // Smoothly rotate the turret base
            _turretBase.rotation = Quaternion.RotateTowards(_turretBase.rotation, targetTurretRotation, rotationSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Checks if the FirePoint is aligned with the target within the specified tolerance.
        /// </summary>
        public bool IsAligned(Vector3 targetPosition, float angleTolerance)
        {
            // VLS always aligned if target exists (vertical launch doesn't need aim)
            if (IsVerticalLaunch) return true;

            if (_firePoint == null) return false;

            Vector3 directionToTarget = (targetPosition - _firePoint.position).normalized;
            float angle = Vector3.Angle(_firePoint.forward, directionToTarget);

            return angle < angleTolerance;
        }
    }
}
