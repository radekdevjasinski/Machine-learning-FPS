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
        [SerializeField] Material _shadowMaterial;

        private readonly Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();

        public static event Action<Transform> OnCharacterAppear;
        public static event Action<Transform> OnCharacterDisappear;

        private void Awake()
        {
            CacheOriginalMaterials();
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
            RestoreOriginalMaterials();
        }

        public void SetHideView()
        {
            SetShadowMaterialsRecursively(_model, _shadowMaterial, _weaponHand);
        }

        private void CacheOriginalMaterials()
        {
            if (_model == null) return;

            foreach (Renderer renderer in _model.GetComponentsInChildren<Renderer>(true))
            {
                _originalMaterials[renderer] = renderer.materials;
            }
        }

        private void RestoreOriginalMaterials()
        {
            foreach (var kvp in _originalMaterials)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.materials = kvp.Value;
                }
            }
        }

        void SetShadowMaterialsRecursively(GameObject obj, Material mat, GameObject exclude = null)
        {
            if (obj == exclude) return;

            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material[] materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = mat;
                }
                renderer.materials = materials;
            }

            foreach (Transform child in obj.transform)
            {
                SetShadowMaterialsRecursively(child.gameObject, mat, exclude);
            }
        }

    }

}
