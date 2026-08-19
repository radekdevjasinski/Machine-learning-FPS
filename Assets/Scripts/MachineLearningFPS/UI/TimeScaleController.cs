using UnityEngine;

namespace MachineLearningFPS.UI
{
    public class TimeScaleController : MonoBehaviour
    {
        [Range(0.1f, 5f)]
        [SerializeField] private float _timeScale = 1f;

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
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
