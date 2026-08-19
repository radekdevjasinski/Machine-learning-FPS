using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

using MachineLearningFPS.Environment;

namespace MachineLearningFPS.UI
{
    public class LevelListController : MonoBehaviour
    {
        private static readonly Color CpuVsCpuColor = new Color(0.15f, 1f, 0.15f, 0.6862745f);
        private static readonly Color HumanVsCpuColor = new Color(1f, 0.15f, 0.15f, 0.6862745f);

        [SerializeField]
        private List<SceneMenuEntry> scenes = new()
        {
            new SceneMenuEntry { displayName = "CL1", sceneName = "cl1" },
            new SceneMenuEntry { displayName = "CL2", sceneName = "cl2" },
            new SceneMenuEntry { displayName = "CL3", sceneName = "cl3" },
        };

        private MainMenuNavigator navigator;
        private Transform buttonContainer;
        private GameObject buttonTemplate;
        private Button modeButton;
        private TMP_Text modeButtonText;
        private MenuButtonCursor modeButtonCursor;
        private bool humanVsCpu;

        private void Awake()
        {
            navigator = GetComponentInParent<MainMenuNavigator>();
            buttonContainer = transform.Find("LevelList");
            buttonTemplate = transform.Find("SceneButtonTemplate").gameObject;

            FindButton("Back").onClick.AddListener(navigator.ShowMain);

            modeButton = FindButton("human vs computer");
            modeButtonText = modeButton.GetComponentInChildren<TMP_Text>();
            modeButtonCursor = modeButton.GetComponent<MenuButtonCursor>();
            modeButton.onClick.AddListener(ToggleMode);
            RefreshModeButtonLabel();

            BuildSceneButtons();
        }

        private void ToggleMode()
        {
            humanVsCpu = !humanVsCpu;
            RefreshModeButtonLabel();

            EventSystem.current.SetSelectedGameObject(null);
            modeButtonCursor.ResetCache();
            EventSystem.current.SetSelectedGameObject(modeButton.gameObject);
        }

        private void RefreshModeButtonLabel()
        {
            modeButtonText.text = humanVsCpu ? "human vs computer" : "computer vs computer";
            modeButtonText.color = humanVsCpu ? HumanVsCpuColor : CpuVsCpuColor;
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

                SceneMenuEntry capturedEntry = entry;
                buttonInstance.GetComponent<Button>().onClick.AddListener(() =>
                {
                    MatchSettings.Select(humanVsCpu ? GameMode.HumanVsBot : GameMode.BotVsBot);
                    SceneManager.LoadScene(capturedEntry.sceneName);
                });
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
