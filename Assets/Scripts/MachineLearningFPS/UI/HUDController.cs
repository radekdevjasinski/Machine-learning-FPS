using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using MachineLearningFPS.Character;
using MachineLearningFPS.Camera;
using MachineLearningFPS.WeaponSystem;
using MachineLearningFPS.Environment;
using TMPro;
using UnityEngine.XR;

namespace MachineLearningFPS.UI
{
    public class UIController : MonoBehaviour
    {
        [SerializeField] private Transform HUDParent;
        [SerializeField] private Color HUDColor;
        [SerializeField] private float HUDColorAlpha;

        [Header("Shooting UI")]
        [SerializeField] private Slider shootingCooldownSlider;
        [SerializeField] private TMP_Text shootingCooldownText;

        [Header("Movement UI")]
        [SerializeField] private TMP_Text movementStateText;

        [Header("Score UI")]
        [SerializeField] private TMP_Text _blueTeamScoreText;
        [SerializeField] private TMP_Text _orangeTeamScoreText;
        [SerializeField] private TMP_Text _episodeCountText;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private TMP_Text _messageText;

        [Header("Cooldown UI")]
        [SerializeField] private GameObject _jumpCooldownGroup;
        [SerializeField] private TMP_Text _jumpCooldownText;
        [SerializeField] private GameObject _crouchCooldownGroup;
        [SerializeField] private TMP_Text _crouchCooldownText;
        [SerializeField] private GameObject _switchCooldownGroup;
        [SerializeField] private TMP_Text _switchCooldownText;

        [Header("Health UI")]
        [SerializeField] private Transform healthBarParent;
        [SerializeField] private GameObject healthBarPrefab;
        [SerializeField] private int maxHealthBars = 10;

        [Header("Images References")]
        [SerializeField] private Image[] _hudImages;
        [SerializeField] private TMP_Text[] _hudTexts;


        private int _episodeCount = 0;


        private SpectatorManager _spectatorManager;
        private EpisodeController _episodeController;
        private Health _activeHealth;
        private FPSMovement _activeMovement;
        private WeaponController _activeWeaponController;
        private Coroutine _messageCoroutine;

        private void OnEnable()
        {
            MovementToUI.OnMovementStateChanged += UpdateMovementState;
            SpectatorManager.OnActiveTargetChanged += HandleActiveTargetChanged;
            EpisodeController.OnEpisodeReset += EpisodeChange;
            Health.OnHealthChanged += UpdateHealthBar;
            MatchController.OnStageMessage += ShowMessage;
            MatchController.OnScoreChanged += UpdateScore;
        }

        private void OnDisable()
        {
            MovementToUI.OnMovementStateChanged -= UpdateMovementState;
            SpectatorManager.OnActiveTargetChanged -= HandleActiveTargetChanged;
            EpisodeController.OnEpisodeReset -= EpisodeChange;
            Health.OnHealthChanged -= UpdateHealthBar;
            MatchController.OnStageMessage -= ShowMessage;
            MatchController.OnScoreChanged -= UpdateScore;
        }

        private void ShowMessage(string message, float displaySeconds)
        {
            if (_messageText == null) return;

            _messageText.text = message;
            _messageText.gameObject.SetActive(true);

            if (_messageCoroutine != null) StopCoroutine(_messageCoroutine);
            _messageCoroutine = StartCoroutine(HideMessageAfterDelay(displaySeconds));
        }

        private IEnumerator HideMessageAfterDelay(float displaySeconds)
        {
            yield return new WaitForSeconds(displaySeconds);
            _messageText.gameObject.SetActive(false);
            _messageCoroutine = null;
        }
        void Start()
        {
            _episodeController = FindAnyObjectByType<EpisodeController>();

            GameObject spectatorObj = GameObject.FindWithTag("MainCamera");
            if (spectatorObj != null)
            {
                _spectatorManager = spectatorObj.GetComponent<SpectatorManager>();
                if (_spectatorManager != null)
                {
                    HandleActiveTargetChanged(_spectatorManager.GetCurrentTarget());
                }
                else
                {
                    Debug.LogError("SpectatorManager component not found on MainCamera.");
                }
            }
            else
            {
                HandleActiveTargetChanged(null);
                Debug.LogError("MainCamera with SpectatorManager not found in the scene.");
            }

            _episodeCountText.text = $"round {_episodeCount + 1}";

        }
        public void UpdateShootingCooldown(float cooldownPercentage)
        {
            if (shootingCooldownSlider != null)
            {
                shootingCooldownSlider.value = cooldownPercentage;
            }

            if (shootingCooldownText != null)
            {
                shootingCooldownText.text = $"{cooldownPercentage * 100:F0}%";
            }
        }

        public void UpdateMovementState(Transform head, string state)
        {
            if (movementStateText != null && _spectatorManager != null && _spectatorManager.GetCurrentTarget() == head)
            {
                movementStateText.text = state;
            }
        }
        public void UpdateHealthBar(Transform healthTransform)
        {
            if (healthBarParent != null && _activeHealth != null && _activeHealth.transform == healthTransform)
            {
                float healthPercentage = _activeHealth.CurrentHealth / _activeHealth.MaxHealth;
                int healthBarsToShow = (int)(healthPercentage * maxHealthBars);

                while (healthBarParent.childCount < maxHealthBars)
                {
                    GameObject healthBar = Instantiate(healthBarPrefab, healthBarParent);
                    healthBar.GetComponent<Image>().color = new Color(HUDColor.r, HUDColor.g, HUDColor.b, HUDColorAlpha);
                }

                for (int i = 0; i < healthBarParent.childCount; i++)
                {
                    healthBarParent.GetChild(i).gameObject.SetActive(i < healthBarsToShow && i < maxHealthBars);
                }
            }
        }
        void Update()
        {
            if (_activeWeaponController != null)
            {
                UpdateShootingCooldown(_activeWeaponController.ShootReadinessPercentage);
            }

            UpdateTimer();

            UpdateCooldownDisplay(_jumpCooldownGroup, _jumpCooldownText, _activeMovement != null ? _activeMovement.JumpCooldownRemaining : 0f);
            UpdateCooldownDisplay(_crouchCooldownGroup, _crouchCooldownText, _activeMovement != null ? _activeMovement.CrouchCooldownRemaining : 0f);
            UpdateCooldownDisplay(_switchCooldownGroup, _switchCooldownText, _activeWeaponController != null ? _activeWeaponController.WeaponSwitchCooldownRemaining : 0f);
        }

        private void UpdateTimer()
        {
            if (_timerText == null || _episodeController == null) return;

            float remaining = _episodeController.TimeRemainingSeconds;
            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);
            _timerText.text = $"{minutes}:{seconds:D2}";
        }

        private void UpdateCooldownDisplay(GameObject group, TMP_Text text, float remainingSeconds)
        {
            if (group == null || text == null) return;

            bool onCooldown = remainingSeconds > 0f;
            group.SetActive(onCooldown);
            if (onCooldown) text.text = Mathf.Min(Mathf.CeilToInt(remainingSeconds), 9).ToString();
        }

        private void HandleActiveTargetChanged(Transform activeTarget)
        {
            bool hasTarget = activeTarget != null;
            SwitchHUDElements(hasTarget);

            if (hasTarget)
            {
                _activeMovement = activeTarget.GetComponentInParent<FPSMovement>();
                _activeWeaponController = _activeMovement.gameObject.GetComponentInChildren<WeaponController>();
                _activeHealth = activeTarget.GetComponentInParent<Health>();
                UpdateShootingCooldown(_activeWeaponController != null ? _activeWeaponController.ShootReadinessPercentage : 0f);
                UpdateMovementState(activeTarget, "idle");
                if (_activeHealth != null) UpdateHealthBar(_activeHealth.transform);
            }
            else
            {
                _activeMovement = null;
                _activeWeaponController = null;
                _activeHealth = null;
            }

            UpdateColors();
        }
        private void SwitchHUDElements(bool state)
        {
            if (HUDParent == null) return;
            HUDParent.gameObject.SetActive(state);
        }

        public void SetPaused(bool isPaused)
        {
            if (isPaused)
            {
                SwitchHUDElements(false);
            }
            else if (_spectatorManager != null)
            {
                HandleActiveTargetChanged(_spectatorManager.GetCurrentTarget());
            }
        }
        public void UpdateScore(int team0Score, int team1Score)
        {
            if (_blueTeamScoreText != null) _blueTeamScoreText.text = team0Score.ToString();
            if (_orangeTeamScoreText != null) _orangeTeamScoreText.text = team1Score.ToString();
        }
        public void EpisodeChange()
        {
            if (_episodeCountText != null)
            {
                _episodeCount += 1;
                _episodeCountText.text = $"round {_episodeCount + 1}";
            }
            if (_spectatorManager.GetCurrentTarget() != null)
            {
                HandleActiveTargetChanged(_spectatorManager.GetCurrentTarget());
            }
            UpdateColors();
        }

        private void UpdateColors()
        {
            if (HUDColor == null)
                return;

            foreach (Image image in _hudImages)
            {
                image.color = new Color(HUDColor.r, HUDColor.g, HUDColor.b, HUDColorAlpha);
            }

            foreach (TMP_Text text in _hudTexts)
            {
                text.color = new Color(HUDColor.r, HUDColor.g, HUDColor.b, HUDColorAlpha);

            }
            foreach (Transform child in healthBarParent)
            {
                child.gameObject.GetComponent<Image>().color = new Color(HUDColor.r, HUDColor.g, HUDColor.b, HUDColorAlpha);
            }
        }

    }
}
