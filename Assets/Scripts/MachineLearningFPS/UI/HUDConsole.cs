using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MachineLearningFPS.Camera;

namespace MachineLearningFPS.UI
{
    public class HUDConsole : MonoBehaviour
    {
        [SerializeField] private TMP_Text consoleText;
        [SerializeField] private bool enableObservationDisplay = true;

        private Transform _activeTarget;
        private Dictionary<string, object> _observations = new Dictionary<string, object>();
        private static HUDConsole _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            if (consoleText == null)
            {
                consoleText = GetComponentInChildren<TMP_Text>();
            }
            SpectatorManager.OnActiveTargetChanged += HandleTargetChanged;
        }

        private void OnDisable()
        {
            SpectatorManager.OnActiveTargetChanged -= HandleTargetChanged;
        }

        private void HandleTargetChanged(Transform newTarget)
        {
            _activeTarget = newTarget;
            Clear();
        }

        public void UpdateValue(Transform source, string name, object value)
        {
            if (!enableObservationDisplay) return;
            if (source != _activeTarget) return;

            _observations[name] = value;
            RefreshDisplay();
        }

        public void Clear()
        {
            _observations.Clear();
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (consoleText == null) return;

            string displayText = "";
            foreach (var kvp in _observations)
            {
                displayText += $"{kvp.Key}: {FormatValue(kvp.Value)}\n";
            }

            consoleText.text = displayText;
        }

        private string FormatValue(object value)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "v" : "x";
            }

            if (value is float floatValue)
            {
                return floatValue.ToString("F3");
            }

            if (value is Vector3 vec3)
            {
                return $"{vec3.x:F1}, {vec3.y:F1}, {vec3.z:F1}";
            }

            if (value is int intValue)
            {
                return intValue.ToString();
            }

            return value?.ToString() ?? "null";
        }

        public static HUDConsole Instance => _instance;

        public static void Log(Transform source, string name, object value)
        {
            if (_instance != null)
            {
                _instance.UpdateValue(source, name, value);
            }
        }
    }
}
