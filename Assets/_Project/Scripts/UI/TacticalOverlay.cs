using UnityEngine;

namespace NavalCommand.UI
{
    public class TacticalOverlay : MonoBehaviour
    {
        [Header("Settings")]
        public bool ShowRangeRings = true;
        public Color RingColor = Color.green;

        private void OnDrawGizmos()
        {
            if (ShowRangeRings)
            {
                Gizmos.color = RingColor;
                // Draw rings around flagship (Placeholder visualization)
                // In game, this would use LineRenderer
            }
        }

        // TODO: Implement runtime LineRenderer logic
    }
}
