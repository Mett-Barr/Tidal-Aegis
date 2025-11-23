using UnityEngine;
using System;
using System.Collections.Generic;

namespace NavalCommand.Systems.Movement
{
    public static class PredictionEngine
    {
        /// <summary>
        /// Simulates the future trajectory of an entity.
        /// Returns a function that maps Time (from now) to Predicted Position.
        /// </summary>
        public static Func<float, Vector3> Predict(MovementState startState, MovementLogic logic, MovementContext ctx, float timeHorizon, float stepSize)
        {
            if (logic == null) return t => startState.Position + startState.Velocity * t; // Linear fallback

            // 1. Run Simulation
            List<Vector3> waypoints = new List<Vector3>();
            List<float> times = new List<float>();

            MovementState currentState = startState;
            float t = 0f;

            waypoints.Add(currentState.Position);
            times.Add(0f);

            // Prevent infinite recursion: The context passed to simulation MUST NOT have a TargetPrediction
            // or at least not one that depends on US.
            // For safety, we strip the prediction from the context during simulation unless we are sure.
            // In this simple implementation, we assume the target moves linearly during OUR prediction of it, 
            // or we use the pre-calculated prediction already in ctx.
            
            while (t < timeHorizon)
            {
                currentState = logic(currentState, ctx, stepSize);
                t += stepSize;
                
                waypoints.Add(currentState.Position);
                times.Add(t);
            }

            // 2. Create Lookup Function (Linear Interpolation)
            return (queryTime) =>
            {
                if (queryTime <= 0) return waypoints[0];
                if (queryTime >= t) return waypoints[waypoints.Count - 1] + currentState.Velocity * (queryTime - t); // Extrapolate

                // Binary Search or simple index mapping if fixed step
                // Since step is fixed:
                float indexFloat = queryTime / stepSize;
                int i = Mathf.FloorToInt(indexFloat);
                float alpha = indexFloat - i;

                if (i >= waypoints.Count - 1) return waypoints[waypoints.Count - 1];

                return Vector3.Lerp(waypoints[i], waypoints[i + 1], alpha);
            };
        }
    }
}
