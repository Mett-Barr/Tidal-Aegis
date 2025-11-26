using UnityEngine;
using NavalCommand.Infrastructure;

namespace NavalCommand.Entities.Units
{
    public class FlagshipController : BaseUnit
    {
        public enum ThrottleState { FullReverse = -1, Stop = 0, Slow = 1, Half = 2, Full = 3 }
        public enum RudderState { HardLeft = -2, Left = -1, Center = 0, Right = 1, HardRight = 2 }

        [Header("Movement Settings")]
        public float MaxSpeed = 15f; // Knots approx
        public float Acceleration = 2f; // How fast we reach target speed
        public float MaxTurnRate = 30f; // Degrees per second
        public float RudderSensitivity = 20f; // How fast rudder changes effect

        [Header("Weapon Control - Dynamic Discovery")]
        [Tooltip("Automatically discovers all weapon types on this ship")]
        public bool EnableFlagshipGun = true;
        public bool EnableAutocannon = true;
        public bool EnableMissile = true;
        public bool EnableCIWS = true;
        public bool EnableTorpedo = true;
        public bool EnableLaserCIWS = true;  // NEW: Laser weapons

        [Header("Current Status")]
        public ThrottleState CurrentThrottle = ThrottleState.Stop;
        public RudderState CurrentRudder = RudderState.Center;
        public float CurrentSpeedKnots => currentSpeed;

        private float currentSpeed = 0f;
        private float currentTurnRate = 0f;
        private float targetSpeed = 0f;
        private float targetTurnRate = 0f;

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
            
            // Subscribe to Input Events
            if (InputReader.Instance != null)
            {
                InputReader.Instance.OnThrottleUp += IncreaseThrottle;
                InputReader.Instance.OnThrottleDown += DecreaseThrottle;
                InputReader.Instance.OnRudderLeft += TurnLeft;
                InputReader.Instance.OnRudderRight += TurnRight;
            }
        }

        private void OnDestroy()
        {
            if (InputReader.Instance != null)
            {
                InputReader.Instance.OnThrottleUp -= IncreaseThrottle;
                InputReader.Instance.OnThrottleDown -= DecreaseThrottle;
                InputReader.Instance.OnRudderLeft -= TurnLeft;
                InputReader.Instance.OnRudderRight -= TurnRight;
            }
        }

        [Header("Debug Control")]
        public bool EnableMovement = false; // Default disabled as requested

        protected override void Update()
        {
            base.Update();
            ApplyWeaponSettings();

            // Disable movement input processing if flag is false
            if (!EnableMovement)
            {
                // Reset inputs to stop moving
                targetSpeed = 0;
                targetTurnRate = 0;
                CurrentThrottle = ThrottleState.Stop;
                CurrentRudder = RudderState.Center;
            }
        }

        /// <summary>
        /// NEW: Dynamic weapon control based on WeaponType enum instead of string matching
        /// Automatically discovers all weapon types on the ship without needing manual updates
        /// </summary>
        private void ApplyWeaponSettings()
        {
            var weapons = GetComponentsInChildren<Components.WeaponController>();
            foreach (var weapon in weapons)
            {
                if (weapon.WeaponStats == null) continue;

                // NEW: Use WeaponType enum for reliable type detection
                Data.WeaponType weaponType = weapon.WeaponStats.Type;
                bool shouldEnable = true;

                // Map WeaponType to control toggle
                switch (weaponType)
                {
                    case Data.WeaponType.FlagshipGun:
                        shouldEnable = EnableFlagshipGun;
                        break;
                    case Data.WeaponType.Autocannon:
                        shouldEnable = EnableAutocannon;
                        break;
                    case Data.WeaponType.Missile:
                        shouldEnable = EnableMissile;
                        break;
                    case Data.WeaponType.CIWS:
                        shouldEnable = EnableCIWS;
                        break;
                    case Data.WeaponType.Torpedo:
                        shouldEnable = EnableTorpedo;
                        break;
                    case Data.WeaponType.LaserCIWS:  // NEW: Laser support
                        shouldEnable = EnableLaserCIWS;
                        break;
                    default:
                        // Unknown weapon type, enable by default and log warning
                        shouldEnable = true;
                        Debug.LogWarning($"[FlagshipController] Unknown weapon type: {weaponType} on {weapon.WeaponStats.name}");
                        break;
                }

                weapon.IsWeaponEnabled = shouldEnable;
            }
        }

        private void IncreaseThrottle()
        {
            if (CurrentThrottle < ThrottleState.Full) CurrentThrottle++;
            UpdateTargetSpeed();
        }

        private void DecreaseThrottle()
        {
            if (CurrentThrottle > ThrottleState.FullReverse) CurrentThrottle--;
            UpdateTargetSpeed();
        }

        private void TurnLeft()
        {
            if (CurrentRudder > RudderState.HardLeft) CurrentRudder--;
            UpdateTargetTurnRate();
        }

        private void TurnRight()
        {
            if (CurrentRudder < RudderState.HardRight) CurrentRudder++;
            UpdateTargetTurnRate();
        }

        private void UpdateTargetSpeed()
        {
            switch (CurrentThrottle)
            {
                case ThrottleState.FullReverse: targetSpeed = -MaxSpeed * 0.5f; break;
                case ThrottleState.Stop: targetSpeed = 0f; break;
                case ThrottleState.Slow: targetSpeed = MaxSpeed * 0.33f; break;
                case ThrottleState.Half: targetSpeed = MaxSpeed * 0.66f; break;
                case ThrottleState.Full: targetSpeed = MaxSpeed; break;
            }
        }

        private void UpdateTargetTurnRate()
        {
            switch (CurrentRudder)
            {
                case RudderState.HardLeft: targetTurnRate = -MaxTurnRate; break;
                case RudderState.Left: targetTurnRate = -MaxTurnRate * 0.5f; break;
                case RudderState.Center: targetTurnRate = 0f; break;
                case RudderState.Right: targetTurnRate = MaxTurnRate * 0.5f; break;
                case RudderState.HardRight: targetTurnRate = MaxTurnRate; break;
            }
        }

        private void FixedUpdate()
        {
            Move();
        }

        public override void Move()
        {
            // Smoothly interpolate towards target values
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, Acceleration * Time.fixedDeltaTime);
            currentTurnRate = Mathf.MoveTowards(currentTurnRate, targetTurnRate, RudderSensitivity * Time.fixedDeltaTime);

            // Apply Physics
            // Rotation
            float turnAmount = currentTurnRate * Time.fixedDeltaTime;
            Quaternion turnRotation = Quaternion.Euler(0, turnAmount, 0);
            Rb.MoveRotation(Rb.rotation * turnRotation);

            // Position
            Vector3 forwardVelocity = transform.forward * currentSpeed;
            Rb.MovePosition(Rb.position + forwardVelocity * Time.fixedDeltaTime);
        }

        protected override void Die()
        {
            // Flagship death logic: Game Over
            // Only trigger Game Over if this is actually the PLAYER'S flagship
            if (Core.GameManager.Instance != null && Core.GameManager.Instance.PlayerFlagship == this)
            {
                Debug.LogError($"[FlagshipController] PLAYER Flagship Destroyed (HP <= 0)! Initiating Game Over/Pause.");
                Core.GameManager.Instance.TogglePause();
            }
            else
            {
                Debug.Log($"[FlagshipController] Enemy/Other Flagship Destroyed. No Game Over.");
            }
            
            // Do NOT call base.Die() because we don't want to Despawn/Destroy the player object immediately
            // or we might want to play an explosion effect then disable the mesh.
            gameObject.SetActive(false);
        }
    }
}
