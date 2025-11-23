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
            float turnRate = 60f; // Increased for better agility
            float navConstant = 3f; // ProNav Constant (typically 3-5)

            Vector3 currentPos = state.Position;
            Vector3 currentVel = state.Velocity;
            float speed = currentVel.magnitude;
            Vector3 forward = currentVel.normalized;

            int phase = state.PhaseIndex;
            Vector3 desiredDir = forward;
            Vector3 accelerationCmd = Vector3.zero;

            // --- Phase Logic ---
            if (phase == 0) // Vertical Launch
            {
                if (state.TimeAlive < 0.5f || currentPos.y < vlsHeight)
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
                        targetPos = ctx.TargetPrediction(timeToImpact);
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

            if (phase == 2) // Terminal Homing (Proportional Navigation)
            {
                if (ctx.TargetState.HasValue)
                {
                    Vector3 targetPos = ctx.TargetState.Value.Position;
                    Vector3 targetVel = ctx.TargetState.Value.Velocity;
                    
                    // ProNav Algorithm
                    // 1. Relative Position & Velocity
                    Vector3 r = targetPos - currentPos;
                    Vector3 v = targetVel - currentVel;
                    
                    // 2. Rotation Vector of LOS (Omega)
                    // Omega = (R x V) / (R . R)
                    float rSqr = Vector3.Dot(r, r);
                    if (rSqr > 0.001f)
                    {
                        Vector3 omega = Vector3.Cross(r, v) / rSqr;
                        
                        // 3. Acceleration Command
                        // A_cmd = N * V_closing * (Omega x LOS_Unit)? 
                        // Simplified Vector ProNav: A = N * V_rel x Omega
                        // This produces acceleration perpendicular to relative velocity
                        
                        Vector3 aCmd = navConstant * Vector3.Cross(v, omega); // This is technically APN (Augmented ProNav) if we include target accel, but simple PN uses V_rel.
                        
                        // Limit Acceleration based on Turn Rate
                        // Max Accel = Speed * TurnRate (approx circular motion a = v^2/r = v * omega)
                        float maxAccel = speed * (turnRate * Mathf.Deg2Rad);
                        accelerationCmd = Vector3.ClampMagnitude(aCmd, maxAccel);
                        
                        // We don't set desiredDir directly here, we apply acceleration to velocity
                        // But to keep consistent with the "Turn Rate" model used in other phases:
                        // We can convert A_cmd to a desired direction.
                        // Desired Dir is roughly Current Dir + (A_cmd * dt) / Speed
                        desiredDir = (currentVel + accelerationCmd * dt).normalized;
                    }
                    else
                    {
                        desiredDir = (targetPos - currentPos).normalized; // Fallback
                    }
                }
            }

            // --- Kinematics ---
            // Rotate velocity towards desired direction
            if (desiredDir != Vector3.zero)
            {
                Quaternion currentRot = Quaternion.LookRotation(forward);
                Quaternion targetRot = Quaternion.LookRotation(desiredDir);
                Quaternion newRot = Quaternion.RotateTowards(currentRot, targetRot, turnRate * dt);
                forward = newRot * Vector3.forward;
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
            float turnRate = 30f;

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
