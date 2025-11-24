using UnityEngine;
using NavalCommand.Core;
using NavalCommand.Data;
using NavalCommand.Systems;

namespace NavalCommand.Systems.Aiming
{
    public static class AimingFunctions
    {
        public delegate Vector3? AimingLogic(Vector3 origin, WeaponStatsSO stats, Transform target, ITargetPredictor predictor);

        public static AimingLogic Resolve(string name)
        {
            switch (name)
            {
                case "Ballistic": return Ballistic;
                case "Direct": return Direct;
                case "Predictive": return Predictive;
                default:
                    Debug.LogWarning($"[AimingFunctions] Unknown logic '{name}', defaulting to Direct.");
                    return Direct;
            }
        }

        // -------------------------------------------------------------------------
        // 1. Ballistic (Guns, Cannons, CIWS)
        // -------------------------------------------------------------------------
        public static Vector3? Ballistic(Vector3 origin, WeaponStatsSO stats, Transform target, ITargetPredictor predictor)
        {
            if (WorldPhysicsSystem.Instance == null) return null;

            float scaledSpeed = WorldPhysicsSystem.Instance.GetScaledSpeed(stats.ProjectileSpeed);
            float scaledRange = WorldPhysicsSystem.Instance.GetScaledRange(stats.Range);
            
            // Calculate Gravity
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
        // 2. Direct (Missiles, Torpedoes - Let the guidance handle it)
        // -------------------------------------------------------------------------
        public static Vector3? Direct(Vector3 origin, WeaponStatsSO stats, Transform target, ITargetPredictor predictor)
        {
            if (WorldPhysicsSystem.Instance == null) return null;
            
            float scaledSpeed = WorldPhysicsSystem.Instance.GetScaledSpeed(stats.ProjectileSpeed);
            return (target.position - origin).normalized * scaledSpeed;
        }

        // -------------------------------------------------------------------------
        // 3. Predictive (Unguided Rockets, Lasers - Linear Lead)
        // -------------------------------------------------------------------------
        public static Vector3? Predictive(Vector3 origin, WeaponStatsSO stats, Transform target, ITargetPredictor predictor)
        {
            if (WorldPhysicsSystem.Instance == null) return null;

            float scaledSpeed = WorldPhysicsSystem.Instance.GetScaledSpeed(stats.ProjectileSpeed);
            float dist = Vector3.Distance(origin, target.position);
            float timeToImpact = dist / scaledSpeed;

            Vector3 predictedPos = predictor.PredictPosition(timeToImpact);
            return (predictedPos - origin).normalized * scaledSpeed;
        }
    }
}
