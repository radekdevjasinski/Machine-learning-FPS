using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

using MachineLearningFPS.Environment;

namespace MachineLearningFPS.UI
{
    public class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject _pausePanel;
        [SerializeField] private string _mainMenuSceneName = "main menu";

        private UIController _hud;
        private TimeScaleController _timeScaleController;
        private MatchController _matchController;
        private Button _resumeButton;
        private bool _isPaused;

        private void Awake()
        {
            _hud = GetComponent<UIController>();
            _timeScaleController = FindAnyObjectByType<TimeScaleController>();
            _matchController = FindAnyObjectByType<MatchController>();

            if (_pausePanel == null && transform.parent != null)
            {
                Transform found = transform.parent.Find("PauseMenu");
                if (found != null) _pausePanel = found.gameObject;
            }

            if (_pausePanel == null)
            {
                Debug.LogError("PauseMenuController: PauseMenu panel not assigned and not found as a sibling.");
                return;
            }

            _resumeButton = FindButton("Resume");
            _resumeButton.onClick.AddListener(Resume);
            FindButton("Main Menu").onClick.AddListener(ReturnToMainMenu);
            _pausePanel.SetActive(false);
        }

        private void Update()
        {
            if (_matchController != null && _matchController.CurrentStage != MatchStage.Gameplay) return;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (_isPaused) Resume();
                else Pause();
            }
        }

        private void Pause()
        {
            _isPaused = true;

            if (_timeScaleController != null) _timeScaleController.enabled = false;
            Time.timeScale = 0f;

            _pausePanel.SetActive(true);
            _hud?.SetPaused(true);
            EventSystem.current.SetSelectedGameObject(_resumeButton.gameObject);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Resume()
        {
            _isPaused = false;

            if (_timeScaleController != null) _timeScaleController.enabled = true;
            else Time.timeScale = 1f;

            _pausePanel.SetActive(false);
            _hud?.SetPaused(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(_mainMenuSceneName);
        }

        private Button FindButton(string label)
        {
            foreach (Button button in _pausePanel.GetComponentsInChildren<Button>(true))
            {
                if (button.GetComponentInChildren<TMP_Text>().text == label)
                {
                    return button;
                }
            }

            Debug.LogError($"PauseMenuController: no button labeled '{label}' found under {_pausePanel.name}.");
            return null;
        }
    }
}
