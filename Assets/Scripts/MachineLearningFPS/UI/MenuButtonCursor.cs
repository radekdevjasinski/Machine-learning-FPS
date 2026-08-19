using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace MachineLearningFPS.UI
{
    public class MenuButtonCursor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private float blinkInterval = 0.5f;
        private float pulseMinAlpha = 0f;

        private static readonly Color CursorColor = new Color(0.15f, 1f, 0.15f);

        private TMP_Text label;
        private string baseText;
        private bool pointerOver;
        private bool showingCursor;
        private float phase;

        private void Awake()
        {
            label = GetComponentInChildren<TMP_Text>();
        }

        private void Update()
        {
            if (label == null) return;

            bool isSelected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
            bool showCursor = pointerOver || isSelected;

            if (!showCursor)
            {
                if (showingCursor)
                {
                    label.text = baseText;
                    showingCursor = false;
                }
                return;
            }

            if (!showingCursor)
            {
                baseText = label.text;
                showingCursor = true;
                phase = 0f;
            }

            phase += Time.deltaTime;
            if (phase >= blinkInterval * 2f) phase -= blinkInterval * 2f;
            float alpha = phase < blinkInterval ? 1f : pulseMinAlpha;
            Color32 cursorColor = CursorColor;
            cursorColor.a = (byte)(alpha * 255f);
            string hex = ColorUtility.ToHtmlStringRGBA(cursorColor);
            label.text = $"{baseText}<color=#{hex}>|</color>";
        }

        public void OnPointerEnter(PointerEventData eventData) => pointerOver = true;
        public void OnPointerExit(PointerEventData eventData) => pointerOver = false;
        public void OnSelect(BaseEventData eventData) { }
        public void OnDeselect(BaseEventData eventData) { }

        public void ResetCache() => showingCursor = false;
    }
}
