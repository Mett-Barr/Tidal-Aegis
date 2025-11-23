using UnityEngine;

namespace NavalCommand.Systems.Movement
{
    public static class MovementFunctions
    {
        // -------------------------------------------------------------------------
        // 1. Ballistic (Gravity + Drag)
        // -------------------------------------------------------------------------
        public static MovementState Ballistic(MovementState state, MovementContext ctx, float dt)
        {
            Vector3 acceleration = ctx.Gravity;
            Vector3 newVelocity = state.Velocity + acceleration * dt;
            Vector3 newPosition = state.Position + state.Velocity * dt; 
            
            Quaternion newRotation = state.Rotation;
            if (newVelocity.sqrMagnitude > 0.01f)
            {
                newRotation = Quaternion.LookRotation(newVelocity);
            }

            return new MovementState
            {
                Position = newPosition,
                Velocity = newVelocity,
                Acceleration = acceleration,
                Rotation = newRotation,
                TimeAlive = state.TimeAlive + dt,
                PhaseIndex = 0,
                CustomData = state.CustomData
            };
        }

        // -------------------------------------------------------------------------
        // 2. Linear (Straight Line, No Gravity)
        // -------------------------------------------------------------------------
        public static MovementState Linear(MovementState state, MovementContext ctx, float dt)
        {
            Vector3 newPosition = state.Position + state.Velocity * dt;
            
            return new MovementState
            {
                Position = newPosition,
                Velocity = state.Velocity,
                Acceleration = Vector3.zero,
                Rotation = state.Rotation,
                TimeAlive = state.TimeAlive + dt,
                PhaseIndex = 0,
                CustomData = state.CustomData
            };
        }

        // -------------------------------------------------------------------------
        // 3. Guided Missile (Vertical Launch -> Cruise -> Terminal ProNav)
        // -------------------------------------------------------------------------
        public static MovementState GuidedMissile(MovementState state, MovementContext ctx, float dt)
        {
            float cruiseHeight = state.CustomData.x;
            float terminalDist = state.CustomData.y;
            float vlsHeight = state.CustomData.z;
            
            // Scaling Factor for Turn Rate
            float scaleFactor = 1.0f;
            if (WorldPhysicsSystem.Instance != null)
            {
                scaleFactor = WorldPhysicsSystem.Instance.GlobalSpeedScale / WorldPhysicsSystem.Instance.GlobalRangeScale;
            }

            // Split Turn Rates (Scaled)
            float cruiseTurnRate = 180f * scaleFactor; // Agile
            float terminalTurnRate = 60f * scaleFactor; // Smooth
            float currentTurnRate = cruiseTurnRate; // Default to cruise
            
            float navConstant = 3f; // Unused in Predictive Pursuit

            Vector3 currentPos = state.Position;
            Vector3 currentVel = state.Velocity;
            float speed = currentVel.magnitude;
            Vector3 forward = currentVel.normalized;
            Vector3 desiredDir = Vector3.zero;

            int phase = state.PhaseIndex;
            
            if (speed < 0.001f)
            {
                // Safety: If speed is zero, just return current state to prevent NaN/Errors
                return state;
            }

            // --- Phase Logic ---
            if (phase == 0) // Vertical Launch
            {
                if (currentPos.y < vlsHeight)
                {
                    desiredDir = Vector3.up;
                }
                else
                {
                    phase = 1;
                }
            }
            
            if (phase == 1) // Cruise
            {
                currentTurnRate = cruiseTurnRate; // Use Agile Turn Rate

                // Maintain Cruise Height
                float heightError = cruiseHeight - currentPos.y;
                Vector3 cruiseDir = forward;
                cruiseDir.y = Mathf.Clamp(heightError * 0.5f, -0.5f, 0.5f); 
                
                if (ctx.TargetState.HasValue)
                {
                    Vector3 targetPos = ctx.TargetState.Value.Position;
                    
                    // Use Prediction
                    if (ctx.TargetPrediction != null)
                    {
                        float dist = Vector3.Distance(currentPos, targetPos);
                        float timeToImpact = dist / speed;
                        if (!float.IsNaN(timeToImpact) && !float.IsInfinity(timeToImpact))
                        {
                            targetPos = ctx.TargetPrediction(timeToImpact);
                        }
                    }

                    Vector3 dirToTarget = (targetPos - currentPos).normalized;
                    
                    if (Vector3.Distance(currentPos, targetPos) < terminalDist)
                    {
                        phase = 2; // Switch to Terminal
                    }
                    else
                    {
                        cruiseDir.x = dirToTarget.x;
                        cruiseDir.z = dirToTarget.z;
                        cruiseDir = cruiseDir.normalized;
                        desiredDir = cruiseDir;
                    }
                }
                else
                {
                    desiredDir.y = 0;
                    desiredDir.Normalize();
                }
            }

            if (phase == 2) // Terminal Homing (Robust Predictive Pursuit)
            {
                currentTurnRate = terminalTurnRate; // Use Smooth Turn Rate

                if (ctx.TargetState.HasValue)
                {
                    Vector3 targetPos = ctx.TargetState.Value.Position;
                    Vector3 targetVel = ctx.TargetState.Value.Velocity;
                    
                    float dist = Vector3.Distance(currentPos, targetPos);
                    float timeToImpact = dist / speed;
                    
                    if (!float.IsNaN(timeToImpact) && !float.IsInfinity(timeToImpact))
                    {
                         // Predict future position
                        Vector3 predictedPos = targetPos + targetVel * timeToImpact;
                        
                        // Calculate desired direction
                        desiredDir = (predictedPos - currentPos).normalized;
                    }
                    else
                    {
                        desiredDir = (targetPos - currentPos).normalized;
                    }

                    // Apply Turn Rate (Smooth Rotation)
                    if (desiredDir != Vector3.zero && forward != Vector3.zero)
                    {
                        Quaternion currentRot = Quaternion.LookRotation(forward);
                        Quaternion targetRot = Quaternion.LookRotation(desiredDir);
                        Quaternion newRot = Quaternion.RotateTowards(currentRot, targetRot, currentTurnRate * dt);
                        forward = newRot * Vector3.forward;
                    }
                }
            }
            else
            {
                // --- Kinematics (Launch / Cruise) ---
                // Rotate velocity towards desired direction
                if (desiredDir != Vector3.zero && forward != Vector3.zero)
                {
                    Quaternion currentRot = Quaternion.LookRotation(forward);
                    Quaternion targetRot = Quaternion.LookRotation(desiredDir);
                    Quaternion newRot = Quaternion.RotateTowards(currentRot, targetRot, currentTurnRate * dt);
                    forward = newRot * Vector3.forward;
                }
            }

            Vector3 newVelocity = forward * speed; 
            Vector3 newPosition = currentPos + newVelocity * dt;
            
            // Calculate actual acceleration applied
            Vector3 actualAccel = (newVelocity - currentVel) / dt;

            return new MovementState
            {
                Position = newPosition,
                Velocity = newVelocity,
                Acceleration = actualAccel,
                Rotation = Quaternion.LookRotation(newVelocity),
                TimeAlive = state.TimeAlive + dt,
                PhaseIndex = phase,
                CustomData = state.CustomData
            };
        }

        // -------------------------------------------------------------------------
        // 4. Torpedo (Underwater Cruise)
        // -------------------------------------------------------------------------
        public static MovementState Torpedo(MovementState state, MovementContext ctx, float dt)
        {
            float depth = state.CustomData.x;
            
            // Scaling Factor
            float scaleFactor = 1.0f;
            if (WorldPhysicsSystem.Instance != null)
            {
                scaleFactor = WorldPhysicsSystem.Instance.GlobalSpeedScale / WorldPhysicsSystem.Instance.GlobalRangeScale;
            }
            
            float turnRate = 30f * scaleFactor;

            Vector3 currentPos = state.Position;
            Vector3 currentVel = state.Velocity;
            float speed = currentVel.magnitude;
            Vector3 forward = currentVel.normalized;

            Vector3 desiredDir = forward;
            
            float heightError = depth - currentPos.y;
            float verticalSpeed = Mathf.Clamp(heightError, -1f, 1f); 
            
            if (ctx.TargetState.HasValue)
            {
                Vector3 targetPos = ctx.TargetState.Value.Position;
                targetPos.y = depth; 

                if (ctx.TargetPrediction != null)
                {
                    float dist = Vector3.Distance(currentPos, targetPos);
                    float timeToImpact = dist / speed;
                    Vector3 pred = ctx.TargetPrediction(timeToImpact);
                    pred.y = depth; 
                    targetPos = pred;
                }

                Vector3 dirToTarget = (targetPos - currentPos).normalized;
                desiredDir = dirToTarget;
            }

            if (desiredDir != Vector3.zero)
            {
                Quaternion currentRot = Quaternion.LookRotation(forward);
                Quaternion targetRot = Quaternion.LookRotation(desiredDir);
                Quaternion newRot = Quaternion.RotateTowards(currentRot, targetRot, turnRate * dt);
                forward = newRot * Vector3.forward;
            }

            Vector3 newVelocity = forward * speed;
            newVelocity.y = verticalSpeed; 
            
            Vector3 newPosition = currentPos + newVelocity * dt;
            Vector3 actualAccel = (newVelocity - currentVel) / dt;

            return new MovementState
            {
                Position = newPosition,
                Velocity = newVelocity,
                Acceleration = actualAccel,
                Rotation = Quaternion.LookRotation(newVelocity),
                TimeAlive = state.TimeAlive + dt,
                PhaseIndex = 0,
                CustomData = state.CustomData
            };
        }
    }
}
