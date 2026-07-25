using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class GlowBehavior3D : MonoBehaviour
{
    [SerializeField] private Color glowColor = Color.red;
    [SerializeField] private float maxIntensity = 2f;
    [SerializeField] private float breatheSpeed = 2f;
    
    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock;
    private bool _isGlowing = false;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (!_isGlowing) return;

        float intensity = (Mathf.Sin(Time.time * breatheSpeed) * 0.5f + 0.5f) * maxIntensity;
        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(EmissionColor, glowColor * intensity);
        _renderer.SetPropertyBlock(_propBlock);
    }

    public void SetGlow(bool state)
    {
        _isGlowing = state;
        if (!state)
        {
            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(EmissionColor, Color.black);
            _renderer.SetPropertyBlock(_propBlock);
        }
    }
}