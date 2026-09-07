using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MachineLearningFPS.UI
{
    public class TimeScaleController : MonoBehaviour
    {
        private const float MinTimeScale = 0.5f;
        private const float MaxTimeScale = 5f;
        private const float TimeScaleStep = 0.25f;
        private const float DisplayDuration = 2f;

        [Range(MinTimeScale, MaxTimeScale)]
        [SerializeField] private float _timeScale = 1f;

        [Header("Input Actions")]
        [SerializeField] private InputActionReference _speedUpAction;
        [SerializeField] private InputActionReference _slowDownAction;

        [Header("Speed Display")]
        [SerializeField] private TextMeshProUGUI _speedText;

        private Action<InputAction.CallbackContext> _onSpeedUp;
        private Action<InputAction.CallbackContext> _onSlowDown;

        private float _displayTimeRemaining;

        private void OnEnable()
        {
            Time.timeScale = _timeScale;

            _onSpeedUp ??= _ => ChangeTimeScale(TimeScaleStep);
            _onSlowDown ??= _ => ChangeTimeScale(-TimeScaleStep);

            if (_speedUpAction != null)
            {
                _speedUpAction.action.Enable();
                _speedUpAction.action.performed += _onSpeedUp;
            }

            if (_slowDownAction != null)
            {
                _slowDownAction.action.Enable();
                _slowDownAction.action.performed += _onSlowDown;
            }
        }

        private void OnDisable()
        {
            if (_speedUpAction != null)
            {
                _speedUpAction.action.performed -= _onSpeedUp;
                _speedUpAction.action.Disable();
            }

            if (_slowDownAction != null)
            {
                _slowDownAction.action.performed -= _onSlowDown;
                _slowDownAction.action.Disable();
            }
        }

        private void OnValidate()
        {
            Time.timeScale = _timeScale;
        }

        private void Update()
        {
            if (!Mathf.Approximately(Time.timeScale, _timeScale))
            {
                Time.timeScale = _timeScale;
            }

            if (_displayTimeRemaining > 0f)
            {
                _displayTimeRemaining -= Time.unscaledDeltaTime;
                if (_displayTimeRemaining <= 0f && _speedText != null)
                {
                    _speedText.gameObject.SetActive(false);
                }
            }
        }

        private void ChangeTimeScale(float delta)
        {
            _timeScale = Mathf.Clamp(_timeScale + delta, MinTimeScale, MaxTimeScale);
            Time.timeScale = _timeScale;
            ShowSpeedDisplay();
        }

        private void ShowSpeedDisplay()
        {
            if (_speedText == null) return;

            _speedText.text = _timeScale.ToString("0.##", CultureInfo.InvariantCulture) + "x";
            _speedText.gameObject.SetActive(true);
            _displayTimeRemaining = DisplayDuration;
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
