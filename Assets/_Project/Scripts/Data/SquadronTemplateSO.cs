using UnityEngine;

namespace NavalCommand.Data
{
    public enum WeightClass
    {
        SuperHeavy,
        Heavy,
        Medium,
        Light
    }

    [CreateAssetMenu(fileName = "NewSquadronTemplate", menuName = "NavalCommand/SquadronTemplate")]
    public class SquadronTemplateSO : ScriptableObject
    {
        [Header("Display Info")]
        [Tooltip("Squadron display name [Traditional Chinese]")]
        public string SquadronName;
        [TextArea]
        [Tooltip("Flavor text description [Traditional Chinese]")]
        public string Description;

        [Header("Configuration")]
        public WeaponType WeaponConfig;
        public WeightClass Weight;
        public int UnitCount;
        public GameObject UnitPrefab;
    }
}
