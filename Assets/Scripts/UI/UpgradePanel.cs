using UnityEngine;

namespace NavalCommand.UI
{
    public class UpgradePanel : MonoBehaviour
    {
        public void Show()
        {
            gameObject.SetActive(true);
            Time.timeScale = 0f; // Pause game
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            Time.timeScale = 1f; // Resume game
        }

        public void SelectUpgrade(int upgradeIndex)
        {
            // Apply upgrade logic
            Hide();
        }
    }
}
