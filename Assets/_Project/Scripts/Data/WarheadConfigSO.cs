using UnityEngine;
using NavalCommand.Systems.VFX;

namespace NavalCommand.Data
{
    [CreateAssetMenu(fileName = "WarheadConfig", menuName = "Naval Command/Warhead Config")]
    public class WarheadConfigSO : ScriptableObject
    {
        [Header("Damage")]
        public float Damage = 10f;
        public float ExplosionRadius = 0f; // 0 = Single Target
        
        [Header("Impact")]
        public ImpactProfile ImpactProfile;
    }
}
