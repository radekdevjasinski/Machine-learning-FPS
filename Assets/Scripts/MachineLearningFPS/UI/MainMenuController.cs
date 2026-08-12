using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MachineLearningFPS.UI
{
    public class MainMenuController : MonoBehaviour
    {
        private MainMenuNavigator navigator;

        private void Awake()
        {
            navigator = GetComponentInParent<MainMenuNavigator>();

            FindButton("Play").onClick.AddListener(navigator.ShowLevelList);
            FindButton("Exit").onClick.AddListener(Exit);

            // Not implemented yet.
            FindButton("Options").interactable = false;
        }

        private Button FindButton(string label)
        {
            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                if (button.GetComponentInChildren<TMP_Text>().text == label)
                {
                    return button;
                }
            }

            Debug.LogError($"MainMenuController: no button labeled '{label}' found under {name}.");
            return null;
        }

        private static void Exit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
