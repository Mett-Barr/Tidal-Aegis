using UnityEngine;
using UnityEngine.UI;
using NavalCommand.Entities.Units;

namespace NavalCommand.UI
{
    public class DashboardUI : MonoBehaviour
    {
        [Header("References")]
        public Text ThrottleText;
        public Text RudderText;
        public Text SpeedText;

        private FlagshipController flagship;

        private void Start()
        {
            // Find Flagship
            if (Core.GameManager.Instance != null && Core.GameManager.Instance.PlayerFlagship != null)
            {
                flagship = Core.GameManager.Instance.PlayerFlagship;
            }
        }

        private void Update()
        {
            if (flagship == null)
            {
                // Try to find it if missing (e.g. scene reload)
                if (Core.GameManager.Instance != null)
                {
                    flagship = Core.GameManager.Instance.PlayerFlagship;
                }
                
                if (flagship == null) return;
            }

            UpdateUI();
        }

        private void UpdateUI()
        {
            if (ThrottleText != null)
            {
                ThrottleText.text = $"THROTTLE: {flagship.CurrentThrottle.ToString().ToUpper()}";
                ThrottleText.color = GetThrottleColor(flagship.CurrentThrottle);
            }

            if (RudderText != null)
            {
                RudderText.text = $"RUDDER: {flagship.CurrentRudder.ToString().ToUpper()}";
                RudderText.color = GetRudderColor(flagship.CurrentRudder);
            }

            if (SpeedText != null)
            {
                // Convert to "Knots" (just raw unit for now)
                float speed = flagship.CurrentSpeedKnots;
                SpeedText.text = $"{speed:F1} kts";
            }
        }

        private Color GetThrottleColor(FlagshipController.ThrottleState state)
        {
            switch (state)
            {
                case FlagshipController.ThrottleState.Full: return Color.green;
                case FlagshipController.ThrottleState.Half: return Color.green;
                case FlagshipController.ThrottleState.Slow: return Color.yellow;
                case FlagshipController.ThrottleState.Stop: return Color.white;
                case FlagshipController.ThrottleState.FullReverse: return Color.red;
                default: return Color.white;
            }
        }

        private Color GetRudderColor(FlagshipController.RudderState state)
        {
            if (state == FlagshipController.RudderState.Center) return Color.white;
            return Color.yellow;
        }
    }
}
