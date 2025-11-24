using System.Collections.Generic;
using UnityEngine;

namespace NavalCommand.Systems.VFX
{
    [System.Serializable]
    public class VFXRule
    {
        [Header("Criteria")]
        [Tooltip("If true, matches any Impact Category")]
        public bool AnyCategory;
        public ImpactCategory Category;

        [Tooltip("If true, matches any Impact Size")]
        public bool AnySize;
        public ImpactSize Size;

        [Tooltip("If true, matches any Surface Type")]
        public bool AnySurface;
        public SurfaceType Surface;

        [Header("Result")]
        public GameObject VFXPrefab;
        public AudioClip SFXClip;
        public float ScaleMultiplier = 1f;
        
        [Header("Priority")]
        [Tooltip("Higher priority rules are checked first")]
        public int Priority = 0;

        public bool Matches(ImpactPayload context)
        {
            if (!AnyCategory && Category != context.Impact.Category) return false;
            if (!AnySize && Size != context.Impact.Size) return false;
            if (!AnySurface && Surface != context.Surface) return false;
            return true;
        }
    }

    [CreateAssetMenu(fileName = "VFXLibrary", menuName = "NavalCommand/VFX/VFXLibrary")]
    public class VFXLibrarySO : ScriptableObject
    {
        public List<VFXRule> Rules = new List<VFXRule>();
        public GameObject FallbackVFX;
        public AudioClip FallbackSFX;

        public VFXRule GetBestRule(ImpactPayload context)
        {
            // Simple linear search with priority
            // In a production system with hundreds of rules, this could be optimized into a dictionary or lookup tree
            
            VFXRule bestRule = null;
            int bestPriority = -1;

            foreach (var rule in Rules)
            {
                if (rule.Matches(context))
                {
                    if (rule.Priority > bestPriority)
                    {
                        bestPriority = rule.Priority;
                        bestRule = rule;
                    }
                }
            }

            return bestRule;
        }
    }
}
