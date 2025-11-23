using UnityEngine;

namespace NavalCommand.Systems
{
    /// <summary>
    /// Interface for predicting target position over time.
    /// Allows for different prediction models (Linear, Circular, Waypoint-based, etc.)
    /// </summary>
    public interface ITargetPredictor
    {
        Vector3 PredictPosition(float time);
        Vector3 GetVelocity();
    }

    /// <summary>
    /// Interface for entities that can provide a prediction strategy for themselves.
    /// Allows for decoupled prediction logic (e.g. missiles providing their own guidance model).
    /// </summary>
    public interface IPredictionProvider
    {
        /// <summary>
        /// Gets the prediction strategy for this entity, relative to the observer.
        /// </summary>
        /// <param name="observerPos">Position of the observer (shooter)</param>
        /// <param name="observerVel">Velocity of the observer (shooter)</param>
        /// <returns>A predictor instance</returns>
        ITargetPredictor GetPredictor(Vector3 observerPos, Vector3 observerVel);
    }

    /// <summary>
    /// Standard linear prediction: Pos = Start + Vel * t
    /// </summary>
    public class LinearTargetPredictor : ITargetPredictor
    {
        private Vector3 _startPos;
        private Vector3 _velocity;

        public LinearTargetPredictor(Vector3 pos, Vector3 vel)
        {
            _startPos = pos;
            _velocity = vel;
        }

        public Vector3 PredictPosition(float time)
        {
            return _startPos + _velocity * time;
        }

        public Vector3 GetVelocity()
        {
            return _velocity;
        }
    }

    /// <summary>
    /// Quadratic prediction: Pos = Start + Vel * t + 0.5 * Accel * t^2
    /// Useful for accelerating or turning targets (if Accel is centripetal).
    /// </summary>
    public class QuadraticTargetPredictor : ITargetPredictor
    {
        private Vector3 _startPos;
        private Vector3 _velocity;
        private Vector3 _acceleration;

        public QuadraticTargetPredictor(Vector3 pos, Vector3 vel, Vector3 accel)
        {
            _startPos = pos;
            _velocity = vel;
            _acceleration = accel;
        }

        public Vector3 PredictPosition(float time)
        {
            return _startPos + _velocity * time + 0.5f * _acceleration * time * time;
        }

        public Vector3 GetVelocity()
        {
            return _velocity + _acceleration * 1f; 
        }
    }

    /// <summary>
    /// Simulates a Guided Missile using Augmented Pursuit (Predictive Pursuit).
    /// Matches the logic in MovementFunctions.GuidedMissile.
    /// </summary>
    public class AugmentedPursuitPredictor : ITargetPredictor
    {
        private Vector3 _missilePos;
        private Vector3 _missileVel;
        private ITargetPredictor _targetPredictor; // The thing the missile is chasing (Me)
        private float _turnRate;

        public AugmentedPursuitPredictor(Vector3 missilePos, Vector3 missileVel, ITargetPredictor myPredictor, float turnRate)
        {
            _missilePos = missilePos;
            _missileVel = missileVel;
            _targetPredictor = myPredictor;
            _turnRate = turnRate;
        }

        public Vector3 PredictPosition(float time)
        {
            if (time <= 0.001f) return _missilePos;

            // Run Simulation
            // Step size: 0.05s for better precision
            float dt = 0.05f;
            int steps = Mathf.CeilToInt(time / dt);
            
            Vector3 currentPos = _missilePos;
            Vector3 currentVel = _missileVel;
            float speed = currentVel.magnitude;
            Vector3 forward = currentVel.normalized;
            
            for (int i = 0; i < steps; i++)
            {
                float t = i * dt;
                Vector3 myPos = _targetPredictor.PredictPosition(t);
                Vector3 myVel = _targetPredictor.GetVelocity();

                // Augmented Pursuit Logic (Predictive)
                // 1. Predict where target will be at impact
                float dist = Vector3.Distance(currentPos, myPos);
                float timeToImpact = speed > 0 ? dist / speed : 0;
                Vector3 predictedTargetPos = myPos + myVel * timeToImpact;

                // 2. Determine desired direction
                Vector3 desiredDir = (predictedTargetPos - currentPos).normalized;

                // 3. Rotate towards desired direction
                if (desiredDir != Vector3.zero)
                {
                    Quaternion currentRot = Quaternion.LookRotation(forward);
                    Quaternion targetRot = Quaternion.LookRotation(desiredDir);
                    Quaternion newRot = Quaternion.RotateTowards(currentRot, targetRot, _turnRate * dt);
                    forward = newRot * Vector3.forward;
                }

                // 4. Move
                currentVel = forward * speed;
                currentPos += currentVel * dt;
            }

            return currentPos;
        }

        public Vector3 GetVelocity()
        {
            return _missileVel;
        }
    }

    public static class BallisticsComputer
    {
        /// <summary>
        /// Solves for the firing vector required to hit a moving target.
        /// Uses an iterative approach to converge on the intercept time.
        /// </summary>
        /// <param name="origin">Shooter position</param>
        /// <param name="projectileSpeed">Projectile speed (m/s)</param>
        /// <param name="gravity">Gravity magnitude (positive, m/s^2)</param>
        /// <param name="target">Target prediction model</param>
        /// <param name="fireVelocity">Output: Required firing velocity vector</param>
        /// <param name="impactTime">Output: Estimated time to impact</param>
        /// <returns>True if a valid solution exists, False if out of range</returns>
        public static bool SolveInterception(
            Vector3 origin,
            float projectileSpeed,
            float gravity,
            ITargetPredictor target,
            out Vector3 fireVelocity,
            out float impactTime)
        {
            fireVelocity = Vector3.zero;
            impactTime = 0f;

            // 1. Initial Guess for Time (t)
            // Assume target is stationary at current pos for first guess
            float dist = Vector3.Distance(origin, target.PredictPosition(0));
            float t = dist / projectileSpeed;

            // Iteration Parameters
            int maxIterations = 5;
            float timeThreshold = 0.01f;

            for (int i = 0; i < maxIterations; i++)
            {
                // 2. Predict Target Position at time t
                Vector3 predictedPos = target.PredictPosition(t);

                // 3. Solve Ballistic Arc to hit this static point
                Vector3? solution = CalculateBallisticVelocity(origin, predictedPos, projectileSpeed, gravity);

                if (!solution.HasValue)
                {
                    return false; // Target out of range or impossible angle
                }

                // 4. Calculate actual time of flight for this arc
                float newT = CalculateTimeOfFlight(origin, predictedPos, solution.Value, gravity);

                // 5. Check Convergence
                if (Mathf.Abs(newT - t) < timeThreshold)
                {
                    // Converged!
                    fireVelocity = solution.Value;
                    impactTime = newT;
                    return true;
                }

                // 6. Update t (Lerp for stability)
                t = Mathf.Lerp(t, newT, 0.5f);
            }

            // If we ran out of iterations, use the last best guess
            Vector3 finalPos = target.PredictPosition(t);
            Vector3? finalSol = CalculateBallisticVelocity(origin, finalPos, projectileSpeed, gravity);
            
            if (finalSol.HasValue)
            {
                fireVelocity = finalSol.Value;
                impactTime = t;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Calculates the initial velocity vector needed to hit a static target point with a given speed and gravity.
        /// Chooses the "Low Arc" (Direct Fire) solution.
        /// </summary>
        private static Vector3? CalculateBallisticVelocity(Vector3 start, Vector3 target, float speed, float gravity)
        {
            Vector3 dir = target - start;
            Vector3 dirXZ = new Vector3(dir.x, 0, dir.z);
            float x = dirXZ.magnitude; // Horizontal distance
            float y = dir.y;           // Vertical difference

            // Handle Zero Gravity (Linear Shot)
            if (gravity < 0.001f)
            {
                return dir.normalized * speed;
            }

            float v2 = speed * speed;
            float v4 = speed * speed * speed * speed;
            float g = gravity;

            // Ballistic Trajectory Equation:
            // angle = atan( (v^2 +/- sqrt(v^4 - g(g*x^2 + 2*y*v^2))) / (g*x) )
            
            float root = v4 - g * (g * x * x + 2 * y * v2);

            if (root < 0)
            {
                return null; // Target out of range
            }

            // We use the minus sign for the Low Arc (Direct Fire)
            // Plus sign would be High Arc (Mortar/Artillery)
            float angle = Mathf.Atan((v2 - Mathf.Sqrt(root)) / (g * x));

            if (float.IsNaN(angle)) return null;

            Vector3 jumpDir = dirXZ.normalized;
            Vector3 v0 = jumpDir * Mathf.Cos(angle) * speed + Vector3.up * Mathf.Sin(angle) * speed;
            return v0;
        }

        private static float CalculateTimeOfFlight(Vector3 start, Vector3 target, Vector3 velocity, float gravity)
        {
            // If Zero Gravity, T = Dist / Speed
            if (gravity < 0.001f)
            {
                return Vector3.Distance(start, target) / velocity.magnitude;
            }

            // For Ballistic: T = HorizontalDist / HorizontalSpeed
            Vector3 horizontalVel = new Vector3(velocity.x, 0, velocity.z);
            Vector3 horizontalDist = new Vector3(target.x - start.x, 0, target.z - start.z);
            
            if (horizontalVel.magnitude < 0.001f)
            {
                // Vertical shot? Use vertical formula
                // Not handling purely vertical shots for now as they are rare in naval combat
                return 0f;
            }

            return horizontalDist.magnitude / horizontalVel.magnitude;
        }
    }
}
