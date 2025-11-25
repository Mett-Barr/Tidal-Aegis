using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NavalCommand.Entities.Projectiles
{
    /// <summary>
    /// Manages the lifecycle of visual effects attached to a projectile.
    /// Handles the "Detach and Fade" logic for trails and smoke when the projectile is destroyed.
    /// </summary>
    public class ProjectileVFXController : MonoBehaviour
    {
        [Header("Particle Systems")]
        [Tooltip("Short-lived particles (e.g. Engine Flame, Tracer Glow). Stops immediately on impact.")]
        public List<ParticleSystem> FlameParticles = new List<ParticleSystem>();

        [Tooltip("Long-lived particles (e.g. Smoke Trail). Detaches and fades on impact.")]
        public List<ParticleSystem> SmokeParticles = new List<ParticleSystem>();

        [Header("Settings")]
        public float AutoDestructDelay = 5.0f; // Time to wait for smoke to fade before destroying detached object

        private bool _isDetached = false;

        /// <summary>
        /// Called when the projectile is fired.
        /// </summary>
        public void OnLaunch()
        {
            _isDetached = false;
            
            // Reset and Play all particles
            foreach (var ps in FlameParticles)
            {
                if (ps != null)
                {
                    ps.Clear();
                    ps.Play();
                }
            }
            foreach (var ps in SmokeParticles)
            {
                if (ps != null)
                {
                    ps.Clear();
                    ps.Play();
                }
            }
        }

        /// <summary>
        /// Called when the projectile hits something or expires.
        /// Detaches smoke trails to let them fade naturally.
        /// </summary>
        public void OnImpact()
        {
            if (_isDetached) return;
            _isDetached = true;

            // 1. Stop Flame immediately (Engine cut)
            foreach (var ps in FlameParticles)
            {
                if (ps != null)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }

            // 2. Handle Smoke (Detach and Fade)
            if (SmokeParticles.Count > 0)
            {
                // Create a holder for the detached particles
                GameObject vfxHolder = new GameObject($"VFX_Remnant_{gameObject.name}_{Time.time}");
                vfxHolder.transform.position = transform.position;
                vfxHolder.transform.rotation = transform.rotation;

                bool hasActiveSmoke = false;

                foreach (var ps in SmokeParticles)
                {
                    if (ps != null)
                    {
                        // Parent to the new holder
                        // Note: We assume the PS is on a child object of the projectile.
                        // If it's on the root, we can't detach it easily without breaking the projectile.
                        // The Generator should ensure PS are on child objects.
                        
                        ps.transform.SetParent(vfxHolder.transform, true); // Keep world position
                        
                        // Stop emitting, but let existing particles fade
                        var main = ps.main;
                        main.stopAction = ParticleSystemStopAction.None; // We handle destruction
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                        
                        hasActiveSmoke = true;
                    }
                }

                if (hasActiveSmoke)
                {
                    // Auto-destroy the holder after delay
                    Destroy(vfxHolder, AutoDestructDelay);
                }
                else
                {
                    Destroy(vfxHolder);
                }
            }
        }
    }
}
