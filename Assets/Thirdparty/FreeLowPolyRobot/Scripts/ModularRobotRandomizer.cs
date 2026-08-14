using System.Collections.Generic;
using UnityEngine;

namespace ThirdParty.FreeLowPolyRobot
{
    public class ModularRobotRandomizer : MonoBehaviour
    {
        private List<GameObject> heads = new List<GameObject>();
        private List<GameObject> bodies = new List<GameObject>();
        private List<GameObject> armsL = new List<GameObject>();
        private List<GameObject> armsR = new List<GameObject>();
        private List<GameObject> legsL = new List<GameObject>();
        private List<GameObject> legsR = new List<GameObject>();

        private List<GameObject> activeParts = new List<GameObject>();

        [SerializeField] private string materialNameToModify = "M_AtlasOffset"; // // Material name
        [SerializeField] private Material materialToModify; // Optional material reference
        [SerializeField] private Texture2D colorAtlasTexture;
        private Vector2 _currentOffset;

        // Cached at Awake, while the original M_AtlasOffset material is still in each slot,
        // so team-material swaps keep working after the slot no longer matches materialNameToModify by name.
        private readonly List<(SkinnedMeshRenderer renderer, int slot)> _colorSlots = new List<(SkinnedMeshRenderer, int)>();

        private void Awake()
        {
            OrganizeRobotParts();
            CacheColorSlots();
        }

        private void CacheColorSlots()
        {
            foreach (GameObject part in activeParts)
            {
                if (part == null) continue;

                SkinnedMeshRenderer renderer = part.GetComponent<SkinnedMeshRenderer>();
                if (renderer == null) continue;

                int materialIndex = GetMaterialIndex(renderer);
                if (materialIndex != -1) _colorSlots.Add((renderer, materialIndex));
            }
        }
        public Color GetCurrentColor(Vector2 adjustment)
        {
            if (colorAtlasTexture == null)
            {
                Debug.LogError("Brak przypisanej tekstury colorAtlasTexture w ModularRobotRandomizer!");
                return Color.white;
            }

            Color pixelColor = colorAtlasTexture.GetPixelBilinear(_currentOffset.x + adjustment.x, _currentOffset.y + adjustment.y);

            return pixelColor;
        }


        private void OrganizeRobotParts()
        {
            Transform parent = this.gameObject.transform;

            foreach (Transform part in parent)
            {
                string partName = part.name;

                if (partName.Contains("Head")) heads.Add(part.gameObject);
                else if (partName.Contains("Body")) bodies.Add(part.gameObject);
                else if (partName.Contains("Arm.L")) armsL.Add(part.gameObject);
                else if (partName.Contains("Arm.R")) armsR.Add(part.gameObject);
                else if (partName.Contains("Leg.L")) legsL.Add(part.gameObject);
                else if (partName.Contains("Leg.R")) legsR.Add(part.gameObject);

                activeParts.Add(part.gameObject);
            }
        }

        public void RandomizeMaterialOffsets()
        {
            float[] possibleValues = { 0f, 0.205078125f, 0.41015625f };
            float randomX = possibleValues[Random.Range(0, possibleValues.Length)];

            float randomY = Random.Range(0, 32) * 0.03125f; // Generate values between 0 and 1 on steps of 0.03125

            _currentOffset = new Vector2(randomX, randomY);


            foreach (GameObject part in activeParts)
            {
                if (part != null)
                {
                    SkinnedMeshRenderer renderer = part.GetComponent<SkinnedMeshRenderer>();
                    if (renderer != null)
                    {
                        int materialIndex = GetMaterialIndex(renderer);
                        if (materialIndex != -1) // Material found on material list
                        {
                            Material mat = renderer.materials[materialIndex];

                            mat.SetVector("_UV_Offset", new Vector2(randomX, randomY));
                        }
                    }
                }
            }
        }

        // M_AtlasOffset's shader has no exposed color/tint property (colors come purely from
        // sampling colorAtlasTexture at _UV_Offset), so team coloring swaps in a plain solid-color
        // material instead of trying to tint a shader that can't be tinted.
        public void SetTeamMaterial(Material material)
        {
            foreach (var (renderer, slot) in _colorSlots)
            {
                if (renderer == null) continue;

                Material[] materials = renderer.materials;
                materials[slot] = material;
                renderer.materials = materials;
            }
        }

        private int GetMaterialIndex(SkinnedMeshRenderer renderer)
        {
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                Material mat = renderer.materials[i];

                if ((materialToModify != null && mat == materialToModify) || mat.name.Contains(materialNameToModify))
                {
                    return i; // Return material index
                }
            }
            return -1; // Material not found
        }

    }

}

