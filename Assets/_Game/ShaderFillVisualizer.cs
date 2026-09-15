using UnityEngine;

public class ShaderFillVisualizer : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    private Material _runtimeMaterial;
    private int _fillAmountPropertyID;

    private void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            _runtimeMaterial = targetRenderer.material;
            _fillAmountPropertyID = Shader.PropertyToID("_FillAmount");
        }
    }

    // 提供給 UnityEvent 呼叫的方法
    public void SetFillAmount(float value)
    {
        if (_runtimeMaterial != null)
        {
            _runtimeMaterial.SetFloat(_fillAmountPropertyID, value);
        }
    }

    private void OnDestroy()
    {
        if (_runtimeMaterial != null) Destroy(_runtimeMaterial);
    }
}