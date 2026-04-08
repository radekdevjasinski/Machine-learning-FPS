using UnityEngine;

public class PerspectiveController : MonoBehaviour
{
    [SerializeField] GameObject _model;

    [Header("Layer Names")]
    [SerializeField] string _defaultLayerName = "Default";
    [SerializeField] string _hideLayerName = "Hide";

    public void SetDefaultView()
    {
        int layerIndex = LayerMask.NameToLayer(_defaultLayerName);
        SetLayerRecursively(_model, layerIndex);
    }

    public void SetHideView()
    {
        int layerIndex = LayerMask.NameToLayer(_hideLayerName);
        SetLayerRecursively(_model, layerIndex);
    }

    void SetLayerRecursively(GameObject obj, int newLayerIndex)
    {
        obj.layer = newLayerIndex;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayerIndex);
        }
    }
}
