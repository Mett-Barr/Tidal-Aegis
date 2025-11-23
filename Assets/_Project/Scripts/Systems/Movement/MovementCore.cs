using UnityEngine;
using System;

namespace NavalCommand.Systems.Movement
{
    /// <summary>
    /// Represents the snapshot of an entity's movement at a specific time.
    /// Must contain all data required to compute the next state.
    /// </summary>
    [Serializable]
    public struct MovementState
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 Acceleration; // NEW: For quadratic prediction
        public Quaternion Rotation;
        
        // Behavioral State
        public float TimeAlive;
        public int PhaseIndex; // 0: Launch, 1: Cruise, 2: Terminal
        public Vector3 CustomData; // e.g., Cruise Altitude, Cached Target Pos, or Last Turn Vector
        
        public static MovementState Create(Vector3 pos, Vector3 vel, Quaternion rot)
        {
            return new MovementState
            {
                Position = pos,
                Velocity = vel,
                Acceleration = Vector3.zero,
                Rotation = rot,
                TimeAlive = 0f,
                PhaseIndex = 0,
                CustomData = Vector3.zero
            };
        }
    }

    /// <summary>
    /// External information visible to the logic.
    /// Crucially, it does NOT contain other entities' logic to prevent recursion.
    /// </summary>
    public struct MovementContext
    {
        public Vector3 Gravity;
        public MovementState? TargetState; // Current observed state of the target (if any)
        
        // Pre-calculated prediction function (Time -> Predicted Position)
        // Supplied by PredictionEngine, consumed by Logic.
        public Func<float, Vector3> TargetPrediction; 
    }

    /// <summary>
    /// Pure function delegate: f(State, Context, dt) -> NextState
    /// </summary>
    public delegate MovementState MovementLogic(MovementState current, MovementContext ctx, float dt);
}
