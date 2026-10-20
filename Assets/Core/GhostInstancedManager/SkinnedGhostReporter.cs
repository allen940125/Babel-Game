using UnityEngine;

public class SkinnedGhostReporter : MonoBehaviour
{
    [Header("殘影設定")]
    public Material ghostMaterial;
    public Color ghostColor = new Color(0.5f, 0.5f, 1f, 0.6f);
    public float spawnInterval = 0.05f;
    public float ghostLifetime = 0.4f;

    [Header("進階設定")]
    public Texture overrideTexture;

    private SkinnedMeshRenderer _skinnedRenderer;
    private Texture _cachedTexture;
    private float _spawnTimer;

    private void Start()
    {
        _skinnedRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (_skinnedRenderer == null)
        {
            Debug.LogError($"[架構錯誤] {gameObject.name} 身上沒有 SkinnedMeshRenderer！");
            return;
        }

        if (overrideTexture != null)
        {
            _cachedTexture = overrideTexture;
        }
        else if (_skinnedRenderer.sharedMaterial != null)
        {
            Material mat = _skinnedRenderer.sharedMaterial;
            if (mat.HasProperty("_BaseMap")) _cachedTexture = mat.GetTexture("_BaseMap");
            else if (mat.HasProperty("_MainTex")) _cachedTexture = mat.GetTexture("_MainTex");
        }
    }

    private void Update()
    {
        if (_skinnedRenderer == null || ghostMaterial == null || SkinnedGhostManager.Instance == null) return;

        // 判定邏輯可依據角色速度自行加入，此處示範純計時
        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= spawnInterval)
        {
            // ★ 核心步驟：動態配置記憶體並烘焙當下幀的網格
            Mesh bakedMesh = new Mesh();
            _skinnedRenderer.BakeMesh(bakedMesh);

            // 計算當下 Transform 的矩陣
            Matrix4x4 matrix = Matrix4x4.TRS(_skinnedRenderer.transform.position, _skinnedRenderer.transform.rotation, _skinnedRenderer.transform.localScale);

            SkinnedGhostManager.Instance.AddGhost(
                bakedMesh, 
                ghostMaterial, 
                _cachedTexture, 
                matrix, 
                ghostColor, 
                ghostLifetime
            );

            _spawnTimer = 0f;
        }
    }
}