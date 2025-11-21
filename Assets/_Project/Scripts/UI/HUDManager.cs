using UnityEngine;
using UnityEngine.UI;
using NavalCommand.Core;

namespace NavalCommand.UI
{
    public class HUDManager : MonoBehaviour
    {
        [Header("UI References")]
        public Text ScoreText;
        public Text HPText;

        private void Start()
        {
            EventManager.OnScoreChanged += UpdateScore;
            EventManager.OnFlagshipHPUpdate += UpdateHP;
        }

        private void OnDestroy()
        {
            EventManager.OnScoreChanged -= UpdateScore;
            EventManager.OnFlagshipHPUpdate -= UpdateHP;
        }

        public void UpdateScore(int score)
        {
            if (ScoreText != null)
            {
                ScoreText.text = $"分數: {score}";
            }
        }

        public void UpdateHP(float current, float max)
        {
            if (HPText != null)
            {
                HPText.text = $"旗艦狀態: {current}/{max}";
            }
        }
    }
}
