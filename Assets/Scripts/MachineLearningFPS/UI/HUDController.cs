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
        [Header("Shooting UI")]
        [SerializeField] private Slider shootingCooldownSlider;
        [SerializeField] private TMP_Text shootingCooldownText;

        [Header("Movement UI")]
        [SerializeField] private TMP_Text movementStateText;

        [Header("Score UI")]
        [SerializeField] private TMP_Text _blueTeamScoreText;
        [SerializeField] private TMP_Text _redTeamScoreText;
        [SerializeField] private TMP_Text _episodeCountText;

        [Header("Health UI")]
        [SerializeField] private Transform healthBarParent;
        [SerializeField] private GameObject healthBarPrefab;
        [SerializeField] private int maxHealthBars = 10;

        private int _blueTeamScore = 0;
        private int _redTeamScore = 0;
        private int _episodeCount = 0;


        private SpectatorManager _spectatorManager;
        private Health _activeHealth;
        private WeaponController _activeWeaponController;

        private void OnEnable()
        {
            MovementToUI.OnMovementStateChanged += UpdateMovementState;
            SpectatorManager.OnActiveTargetChanged += HandleActiveTargetChanged;
            EpisodeController.OnPlayerKilled += AddTeamScore;
            EpisodeController.OnEpisodeReset += EpisodeChange;
            Health.OnHealthChanged += UpdateHealthBar;
        }

        private void OnDisable()
        {
            MovementToUI.OnMovementStateChanged -= UpdateMovementState;
            SpectatorManager.OnActiveTargetChanged -= HandleActiveTargetChanged;
            EpisodeController.OnPlayerKilled -= AddTeamScore;
            EpisodeController.OnEpisodeReset -= EpisodeChange;
            Health.OnHealthChanged -= UpdateHealthBar;
        }
        void Start()
        {
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
        }
        public void UpdateShootingCooldown(float cooldownPercentage)
        {
            if (shootingCooldownSlider != null)
            {
                shootingCooldownSlider.value = cooldownPercentage;
            }

            if (shootingCooldownText != null)
            {
                shootingCooldownText.text = $"{cooldownPercentage * 100:F1}%";
            }
        }

        public void UpdateMovementState(Transform head, string state)
        {
            if (movementStateText != null && _spectatorManager.GetCurrentTarget() == head)
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
                    Instantiate(healthBarPrefab, healthBarParent);
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
        }

        private void HandleActiveTargetChanged(Transform activeTarget)
        {
            bool hasTarget = activeTarget != null;
            SwitchHUDElements(hasTarget);

            if (hasTarget)
            {
                FPSMovement movement = activeTarget.GetComponentInParent<FPSMovement>();
                _activeWeaponController = movement.gameObject.GetComponentInChildren<WeaponController>();
                _activeHealth = activeTarget.GetComponentInParent<Health>();
                UpdateShootingCooldown(_activeWeaponController != null ? _activeWeaponController.ShootReadinessPercentage : 0f);
                UpdateMovementState(activeTarget, "Idle");
                if (_activeHealth != null) UpdateHealthBar(_activeHealth.transform);
            }
            else
            {
                _activeWeaponController = null;
                _activeHealth = null;
            }
        }
        private void SwitchHUDElements(bool state)
        {
            if (HUDParent == null) return;
            HUDParent.gameObject.SetActive(state);
        }
        public void AddTeamScore(int teamID)
        {
            if (teamID == 0)
            {
                _blueTeamScore += 1;
                if (_blueTeamScoreText != null) _blueTeamScoreText.text = _blueTeamScore.ToString();
            }
            else if (teamID == 1)
            {
                _redTeamScore += 1;
                if (_redTeamScoreText != null) _redTeamScoreText.text = _redTeamScore.ToString();
            }

        }
        public void EpisodeChange()
        {
            if (_episodeCountText != null)
            {
                _episodeCount += 1;
                _episodeCountText.text = $"Episode: {_episodeCount + 1}";
            }
            if (_spectatorManager.GetCurrentTarget() != null)
            {
                HandleActiveTargetChanged(_spectatorManager.GetCurrentTarget());
            }
        }

    }
}
