using UnityEngine;

public class ExcludeFromVolume : MonoBehaviour
{
    [SerializeField] private string targetLayerName = "NoPostFX";
    [SerializeField] private bool applyToChildren = true;

    private void Awake()
    {
        int layer = LayerMask.NameToLayer(targetLayerName);
        SetLayerRecursively(gameObject, layer);
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;

        if (!applyToChildren) return;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        int layer = LayerMask.NameToLayer(targetLayerName);
        if (layer != -1)
        {
            SetLayerRecursively(gameObject, layer);
        }
    }
#endif
}