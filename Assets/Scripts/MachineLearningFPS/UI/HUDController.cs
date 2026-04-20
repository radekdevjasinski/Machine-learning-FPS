using UnityEngine;
using UnityEngine.UI;

using MachineLearningFPS.Character;
using MachineLearningFPS.Camera;
using MachineLearningFPS.WeaponSystem;
using TMPro;

namespace MachineLearningFPS.UI
{
    public class UIController : MonoBehaviour
    {
        [SerializeField] private Slider shootingCooldownSlider;
        [SerializeField] private TMP_Text shootingCooldownText;
        [SerializeField] private TMP_Text movementStateText;
        [SerializeField] private Transform HUDParent;


        private SpectatorManager _spectatorManager;
        private WeaponController _activeWeaponController;

        private void OnEnable()
        {
            MovementToUI.OnMovementStateChanged += UpdateMovementState;
            SpectatorManager.OnActiveTargetChanged += HandleActiveTargetChanged;
        }

        private void OnDisable()
        {
            MovementToUI.OnMovementStateChanged -= UpdateMovementState;
            SpectatorManager.OnActiveTargetChanged -= HandleActiveTargetChanged;
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
                UpdateShootingCooldown(_activeWeaponController != null ? _activeWeaponController.ShootReadinessPercentage : 0f);
                UpdateMovementState(activeTarget, "Idle");
            }
            else
            {
                _activeWeaponController = null;
            }
        }
        private void SwitchHUDElements(bool state)
        {
            if (HUDParent == null) return;
            HUDParent.gameObject.SetActive(state);
        }

    }
}
