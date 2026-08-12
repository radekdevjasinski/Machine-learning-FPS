using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace MachineLearningFPS.UI
{
    public class LevelListController : MonoBehaviour
    {
        [SerializeField]
        private List<SceneMenuEntry> scenes = new()
        {
            new SceneMenuEntry { displayName = "CL1", sceneName = "cl1" },
            new SceneMenuEntry { displayName = "CL2", sceneName = "cl2" },
        };

        private MainMenuNavigator navigator;
        private Transform buttonContainer;
        private GameObject buttonTemplate;

        private void Awake()
        {
            navigator = GetComponentInParent<MainMenuNavigator>();
            buttonContainer = transform.Find("LevelList");
            buttonTemplate = transform.Find("SceneButtonTemplate").gameObject;

            FindButton("Back").onClick.AddListener(navigator.ShowMain);

            // Not implemented yet.
            FindButton("human vs computer").interactable = false;

            BuildSceneButtons();
        }

        private void BuildSceneButtons()
        {
            for (int i = buttonContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(buttonContainer.GetChild(i).gameObject);
            }

            foreach (SceneMenuEntry entry in scenes)
            {
                GameObject buttonInstance = Instantiate(buttonTemplate, buttonContainer);
                buttonInstance.SetActive(true);
                buttonInstance.GetComponentInChildren<TMP_Text>().text = $"• {entry.displayName}";

                string sceneName = entry.sceneName;
                buttonInstance.GetComponent<Button>().onClick.AddListener(() => SceneManager.LoadScene(sceneName));
            }
        }

        private Button FindButton(string label)
        {
            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                if (button.transform.IsChildOf(buttonContainer))
                {
                    continue;
                }

                if (button.GetComponentInChildren<TMP_Text>().text == label)
                {
                    return button;
                }
            }

            Debug.LogError($"LevelListController: no button labeled '{label}' found under {name}.");
            return null;
        }
    }
}
