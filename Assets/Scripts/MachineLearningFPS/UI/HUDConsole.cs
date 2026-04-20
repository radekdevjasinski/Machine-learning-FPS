using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MachineLearningFPS.UI
{
    public class HUDConsole : MonoBehaviour
    {
        [SerializeField] private TMP_Text consoleText;
        [SerializeField] private bool enableObservationDisplay = true;

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
        }

        /// <summary>
        /// Updates or adds an observation value. If the name exists, updates it; otherwise adds new.
        /// </summary>
        public void UpdateValue(string name, object value)
        {
            if (!enableObservationDisplay) return;

            _observations[name] = value;
            RefreshDisplay();
        }

        /// <summary>
        /// Clears all observations from the display.
        /// </summary>
        public void Clear()
        {
            _observations.Clear();
            RefreshDisplay();
        }

        /// <summary>
        /// Refreshes the UI text with all current observations.
        /// </summary>
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

        /// <summary>
        /// Formats values for readable display.
        /// </summary>
        private string FormatValue(object value)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "v" : "x";
            }

            if (value is float floatValue)
            {
                return floatValue.ToString("F1");
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

        /// <summary>
        /// Gets the singleton instance (optional, for static access).
        /// </summary>
        public static HUDConsole Instance => _instance;

        /// <summary>
        /// Static method for convenience.
        /// </summary>
        public static void Log(string name, object value)
        {
            if (_instance != null)
            {
                _instance.UpdateValue(name, value);
            }
        }
    }
}
