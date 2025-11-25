using UnityEngine;
using NavalCommand.Core;
using NavalCommand.Data;
using NavalCommand.Systems;

namespace NavalCommand.Systems.Aiming
{
    public static class AimingFunctions
    {
        // Updated Delegate: Now accepts 'forward' (current aim direction)
        public delegate Vector3? AimingLogic(Vector3 origin, Vector3 forward, WeaponStatsSO stats, Transform target, ITargetPredictor predictor);

        public static AimingLogic Resolve(string name)
        {
            switch (name)
            {
                case "Ballistic": return Ballistic;
                case "Direct": return Direct;
                case "Predictive": return Predictive;
                case "AdvancedPredictive": return AdvancedPredictive; // New!
                default:
                    Debug.LogWarning($"[AimingFunctions] Unknown logic '{name}', defaulting to Direct.");
                    return Direct;
            }
        }

        // -------------------------------------------------------------------------
        // 1. Ballistic (Standard Gravity Arc)
        // -------------------------------------------------------------------------
        public static Vector3? Ballistic(Vector3 origin, Vector3 forward, WeaponStatsSO stats, Transform target, ITargetPredictor predictor)
        {
            if (WorldPhysicsSystem.Instance == null) return null;

            float scaledSpeed = WorldPhysicsSystem.Instance.GetScaledSpeed(stats.ProjectileSpeed);
            float scaledRange = WorldPhysicsSystem.Instance.GetScaledRange(stats.Range);
            
            float gravityY = -WorldPhysicsSystem.Instance.GetBallisticGravity(scaledSpeed, scaledRange);
            if (stats.GravityMultiplier < 0.01f) gravityY = 0f;
            else gravityY *= stats.GravityMultiplier;

            if (BallisticsComputer.SolveInterception(origin, scaledSpeed, Mathf.Abs(gravityY), predictor, out Vector3 solution, out float t))
            {
                return solution;
            }
            return null;
        }

        // -------------------------------------------------------------------------
        // 2. Direct (Missiles/Torpedoes)
        // -------------------------------------------------------------------------
        public static Vector3? Direct(Vector3 origin, Vector3 forward, WeaponStatsSO stats, Transform target, ITargetPredictor predictor)
        {
            if (WorldPhysicsSystem.Instance == null) return null;
            
            float scaledSpeed = WorldPhysicsSystem.Instance.GetScaledSpeed(stats.ProjectileSpeed);
            return (target.position - origin).normalized * scaledSpeed;
        }

        // -------------------------------------------------------------------------
        // 3. Predictive (Simple Linear Lead)
        // -------------------------------------------------------------------------
        public static Vector3? Predictive(Vector3 origin, Vector3 forward, WeaponStatsSO stats, Transform target, ITargetPredictor predictor)
        {
            if (WorldPhysicsSystem.Instance == null) return null;

            float scaledSpeed = WorldPhysicsSystem.Instance.GetScaledSpeed(stats.ProjectileSpeed);
            float dist = Vector3.Distance(origin, target.position);
            float timeToImpact = dist / scaledSpeed;

            Vector3 predictedPos = predictor.PredictPosition(timeToImpact);
            return (predictedPos - origin).normalized * scaledSpeed;
        }

        // -------------------------------------------------------------------------
        // 4. Advanced Predictive (Lead + Turret Lag Compensation)
        // -------------------------------------------------------------------------
        public static Vector3? AdvancedPredictive(Vector3 origin, Vector3 forward, WeaponStatsSO stats, Transform target, ITargetPredictor predictor)
        {
            if (WorldPhysicsSystem.Instance == null) return null;

            float scaledSpeed = WorldPhysicsSystem.Instance.GetScaledSpeed(stats.ProjectileSpeed);
            float maxRotSpeed = stats.RotationSpeed;
            float rotAccel = stats.RotationAcceleration;

            // 1. Initial Guess: Flight time to current target pos
            float dist = Vector3.Distance(origin, target.position);
            float t_flight = dist / scaledSpeed;
            float t_total = t_flight; // Start assuming 0 turn time

            // Iterative Solver
            int iterations = 4;
            Vector3 finalAimDir = forward;

            for (int i = 0; i < iterations; i++)
            {
                // A. Predict Target at T_total
                Vector3 predictedPos = predictor.PredictPosition(t_total);
                Vector3 aimVec = predictedPos - origin;
                float newDist = aimVec.magnitude;
                finalAimDir = aimVec.normalized;

                // B. Calculate Turn Time
                float angleDiff = Vector3.Angle(forward, finalAimDir);
                float t_turn = CalculateTurnTime(angleDiff, maxRotSpeed, rotAccel);

                // C. Calculate Flight Time
                float t_newFlight = newDist / scaledSpeed;

                // D. Update Total Time (Turn + Flight)
                // Note: We don't just add them linearly because we turn AND the shell flies? 
                // No, we must turn FIRST, then fire. So T_total = T_turn + T_flight.
                float t_newTotal = t_turn + t_newFlight;

                // E. Converge
                if (Mathf.Abs(t_newTotal - t_total) < 0.05f)
                {
                    t_total = t_newTotal;
                    break;
                }
                t_total = Mathf.Lerp(t_total, t_newTotal, 0.6f);
            }

            // Final Prediction
            Vector3 finalPos = predictor.PredictPosition(t_total);
            
            // GRAVITY COMPENSATION:
            // Now that we know WHERE the target will be, we must calculate the Ballistic Arc to hit it.
            // If we just return (finalPos - origin), we are firing linearly, which fails for gravity-affected projectiles (Autocannon).
            
            float gravityY = -WorldPhysicsSystem.Instance.GetBallisticGravity(scaledSpeed, WorldPhysicsSystem.Instance.GetScaledRange(stats.Range));
            if (stats.GravityMultiplier < 0.01f) gravityY = 0f;
            else gravityY *= stats.GravityMultiplier;

            // Use Double Precision for Ballistic Calculation to avoid float errors with high velocity/gravity
            Vector3? ballisticVel = CalculateBallisticVelocityHighPrecision(origin, finalPos, scaledSpeed, Mathf.Abs(gravityY));
            
            if (ballisticVel.HasValue)
            {
                return ballisticVel.Value;
            }
            
            // Fallback to linear if no solution (e.g. out of range)
            return (finalPos - origin).normalized * scaledSpeed;
        }

        /// <summary>
        /// Calculates the initial velocity vector needed to hit a static target point with a given speed and gravity.
        /// Uses DOUBLE precision to prevent errors with high velocity (v^4) and large gravity.
        /// </summary>
        private static Vector3? CalculateBallisticVelocityHighPrecision(Vector3 start, Vector3 target, float speed, float gravity)
        {
            Vector3 dir = target - start;
            Vector3 dirXZ = new Vector3(dir.x, 0, dir.z);
            double x = dirXZ.magnitude;
            double y = dir.y;
            double v = speed;
            double g = gravity;

            if (g < 0.001)
            {
                return dir.normalized * speed;
            }

            double v2 = v * v;
            double v4 = v2 * v2;
            
            // Ballistic Equation: angle = atan( (v^2 +/- sqrt(v^4 - g(g*x^2 + 2*y*v^2))) / (g*x) )
            double term = g * (g * x * x + 2 * y * v2);
            double root = v4 - term;
            
            if (root < 0)
            {
                // Debug.Log($"[Aiming] Out of Range. Root: {root}, V4: {v4}, Term: {term}");
                return null; 
            }

            // Low Arc (Minus sign)
            double angle = System.Math.Atan((v2 - System.Math.Sqrt(root)) / (g * x));

            if (double.IsNaN(angle)) return null;

            Vector3 jumpDir = dirXZ.normalized;
            float angleF = (float)angle;
            
            Vector3 v0 = jumpDir * Mathf.Cos(angleF) * speed + Vector3.up * Mathf.Sin(angleF) * speed;
            return v0;
        }

        /// <summary>
        /// Calculates time to rotate 'angle' degrees given max speed and acceleration.
        /// Uses Kinematic equations for Trapezoidal or Triangular velocity profile.
        /// </summary>
        private static float CalculateTurnTime(float angle, float maxSpeed, float accel)
        {
            if (angle < 0.01f) return 0f;
            if (accel <= 0.1f) return angle / Mathf.Max(maxSpeed, 0.1f); // No accel, linear

            // Time to reach max speed: t_accel = v_max / a
            float t_accel = maxSpeed / accel;
            
            // Angle covered during acceleration (and deceleration): 
            // d_accel = 0.5 * a * t^2 = 0.5 * a * (v/a)^2 = 0.5 * v^2 / a
            // Total angle for accel + decel = v^2 / a
            float angle_threshold = (maxSpeed * maxSpeed) / accel;

            if (angle < angle_threshold)
            {
                // Triangular Profile (Never reach max speed)
                // d = a * t_half^2  -> t_half = sqrt(d/a) -> total_t = 2 * sqrt(d/a)
                // Here d is half the angle? No.
                // Total angle = 2 * (0.5 * a * t_half^2) = a * t_half^2
                // t_half = Sqrt(angle / accel)
                // Total Time = 2 * Sqrt(angle / accel)
                return 2f * Mathf.Sqrt(angle / accel);
            }
            else
            {
                // Trapezoidal Profile (Reach max speed, cruise, decel)
                // Time = T_accel + T_decel + T_cruise
                // T_accel + T_decel = 2 * (maxSpeed / accel)
                // Angle remaining for cruise = angle - angle_threshold
                // T_cruise = (angle - angle_threshold) / maxSpeed
                
                float t_ramp = 2f * (maxSpeed / accel);
                float angle_cruise = angle - angle_threshold;
                float t_cruise = angle_cruise / maxSpeed;
                
                return t_ramp + t_cruise;
            }
        }
    }
}
