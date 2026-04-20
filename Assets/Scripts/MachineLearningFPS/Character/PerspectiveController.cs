using System;
using System.Collections.Generic;
using UnityEngine;

namespace MachineLearningFPS.Character
{
    public class PerspectiveController : MonoBehaviour
    {
        [SerializeField] GameObject _model;
        [SerializeField] Transform _head;
        public Transform Head => _head;
        [SerializeField] GameObject _weaponHand;
        [SerializeField] private int _hiddenLayer = 8;

        private readonly Dictionary<Transform, int> _originalLayers = new Dictionary<Transform, int>();

        public static event Action<Transform> OnCharacterAppear;
        public static event Action<Transform> OnCharacterDisappear;

        private void Awake()
        {
            CacheOriginalLayers();
        }

        private void OnEnable()
        {
            OnCharacterAppear?.Invoke(_head);
        }

        private void OnDisable()
        {
            OnCharacterDisappear?.Invoke(_head);
        }

        public void SetDefaultView()
        {
            RestoreOriginalLayers();
        }

        public void SetHideView()
        {
            SetLayerRecursively(_model, _hiddenLayer, _weaponHand);
        }

        private void CacheOriginalLayers()
        {
            if (_model == null) return;

            foreach (Transform child in _model.GetComponentsInChildren<Transform>(true))
            {
                _originalLayers[child] = child.gameObject.layer;
            }
        }

        private void RestoreOriginalLayers()
        {
            foreach (var kvp in _originalLayers)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.gameObject.layer = kvp.Value;
                }
            }
        }

        private void SetLayerRecursively(GameObject obj, int layer, GameObject exclude = null)
        {
            if (obj == null || obj == exclude) return;

            obj.layer = layer;

            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer, exclude);
            }
        }

    }

}
