using UnityEngine;
using System;
using System.Collections;

namespace NavalCommand.VFX
{
    /// <summary>
    /// Auto-following VFX that can detach and fade independently.
    /// Uses event-based recycling for object pooling (no Instantiate/Destroy).
    /// </summary>
    public class AutoFollowVFX : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Time to wait before recycling this VFX after detachment")]
        public float AutoDestructDelay = 5.0f;

        /// <summary>
        /// Event fired when VFX is ready to be recycled to pool.
        /// VFXManager subscribes to this to handle recycling.
        /// </summary>
        public Action OnDetached;

        private Transform _target;
        private bool _isDetached = false;

        /// <summary>
        /// Start following a target transform.
        /// </summary>
        public void StartFollowing(Transform target)
        {
            _target = target;
            _isDetached = false;

            // Reset smoke particles (if any)
            foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.gameObject.SetActive(true);
                ps.Clear();
                ps.Play();
            }
            
            // Reset TrailRenderer
            TrailRenderer trail = GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.Clear();
                trail.emitting = true;
            }
        }

        /// <summary>
        /// Detach from target and fade out particles.
        /// After particles fade, fires OnDetached event for pool recycling.
        /// </summary>
        public void DetachAndFade()
        {
            if (_isDetached) return;
            
            _isDetached = true;
            _target = null;
            
            // Detach from parent
            transform.SetParent(null);

            // Stop smoke particle emission (if any smoke particles exist)
            foreach (var ps in GetComponentsInChildren<ParticleSystem>())
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            
            // TrailRenderer: Stop emitting, let existing trail fade
            TrailRenderer trail = GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.emitting = false;
            }

            // Start recycling
            StartCoroutine(RecycleAfterDelay());
        }

        private IEnumerator RecycleAfterDelay()
        {
            // Calculate actual max particle lifetime
            float maxLifetime = 0f;
            foreach (var ps in GetComponentsInChildren<ParticleSystem>())
            {
                if (ps != null)
                {
                    maxLifetime = Mathf.Max(maxLifetime, ps.main.startLifetime.constantMax);
                }
            }
            
            // SOLUTION B: Consider TrailRenderer fade time
            TrailRenderer trail = GetComponent<TrailRenderer>();
            float trailTime = (trail != null) ? trail.time : 0f;
            
            // Wait for longest fade time
            float waitTime = Mathf.Max(AutoDestructDelay, maxLifetime, trailTime) + 0.5f;
            yield return new WaitForSeconds(waitTime);
            
            // Force clear all remaining VFX
            foreach (var ps in GetComponentsInChildren<ParticleSystem>())
            {
                if (ps != null)
                {
                    ps.Clear(true);
                }
            }
            
            // Trail will auto-fade (no need to clear)
            
            // Fire event for VFXManager to recycle to pool
            OnDetached?.Invoke();
            
            // Reset state for reuse
            ResetState();
        }

        private void ResetState()
        {
            _target = null;
            _isDetached = false;
            
            // Clear event to prevent stale callbacks
            OnDetached = null;
        }

        private void LateUpdate()
        {
            // Follow target if not detached
            if (!_isDetached && _target != null)
            {
                transform.position = _target.position;
                transform.rotation = _target.rotation;
            }
        }

        private void OnDisable()
        {
            // Stop any running coroutines when pooled
            StopAllCoroutines();
        }
    }
}
