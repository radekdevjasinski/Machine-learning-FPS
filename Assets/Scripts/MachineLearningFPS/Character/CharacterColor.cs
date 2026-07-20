using UnityEngine;
using ThirdParty.FreeLowPolyRobot;
namespace MachineLearningFPS.Character
{
    public class CharacterColor : MonoBehaviour
    {
        [SerializeField] private ModularRobotRandomizer _robotColorScript;
        [SerializeField] private Vector2 _adjustment;

        void Start()
        {
            if (_robotColorScript == null)
                _robotColorScript = GetComponentInChildren<ModularRobotRandomizer>();
        }

        public Color GetCurrentColor()
        {
            return AdjustColorForUI(_robotColorScript.GetCurrentColor(_adjustment));
        }
        public void RandomizeColor()
        {
            _robotColorScript.RandomizeMaterialOffsets();
        }
        private Color AdjustColorForUI(Color originalColor)
        {
            float h, s, v;
            Color.RGBToHSV(originalColor, out h, out s, out v);

            v = Mathf.Max(v, 0.8f);

            s = Mathf.Max(s, 0.7f);

            return Color.HSVToRGB(h, s, v);
        }
    }
}