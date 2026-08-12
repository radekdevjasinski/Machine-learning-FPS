using UnityEngine;

namespace MachineLearningFPS.UI
{
    public class MainMenuNavigator : MonoBehaviour
    {
        private GameObject mainPanel;
        private GameObject levelListPanel;

        private void Awake()
        {
            mainPanel = transform.Find("Main").gameObject;
            levelListPanel = transform.Find("LevelList").gameObject;
        }

        public void ShowMain()
        {
            mainPanel.SetActive(true);
            levelListPanel.SetActive(false);
        }

        public void ShowLevelList()
        {
            mainPanel.SetActive(false);
            levelListPanel.SetActive(true);
        }
    }
}
