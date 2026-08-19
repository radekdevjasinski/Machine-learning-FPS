using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

            ShowMain();
        }

        public void ShowMain()
        {
            mainPanel.SetActive(true);
            levelListPanel.SetActive(false);
            SelectFirstButton(mainPanel);
        }

        public void ShowLevelList()
        {
            mainPanel.SetActive(false);
            levelListPanel.SetActive(true);
            SelectFirstButton(levelListPanel);
        }

        private static void SelectFirstButton(GameObject panel)
        {
            Button firstButton = panel.GetComponentInChildren<Button>(true);
            if (firstButton != null) EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
        }
    }
}
