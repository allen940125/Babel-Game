using UnityEngine;

public class InstancedGhostReporter : MonoBehaviour
{
    [Header("殘影設定")]
    public Material ghostMaterial;
    public Color ghostColor = new Color(0.3f, 0.8f, 1f, 0.6f);
    [Tooltip("每移動多遠產生一個殘影")]
    public float distanceGap = 0.5f;
    public float ghostLifetime = 0.35f;

    [Header("殘影位置微調 (2.5D必備)")]
    public Vector3 posOffset = new Vector3(0, 0, 0.1f);
    public bool autoAntiZFighting = true;

    [Header("進階設定")]
    public Texture overrideTexture;

    private Mesh _cachedMesh;
    private Texture _cachedTexture;
    private Transform _meshTransform; // ★ 必須記錄真正掛載網格的 Transform，防止 3D 子物件縮放跑位

    private float _spawnTimer;
    private Vector3 _lastSpawnPos;

    private void Start()
    {
        _lastSpawnPos = transform.position;

        // 1. 抓取 3D 靜態網格 (往子物件找)
        MeshFilter mf = GetComponentInChildren<MeshFilter>();
        if (mf != null)
        {
            _cachedMesh = mf.sharedMesh;
            _meshTransform = mf.transform;
            
            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mr != null && mr.sharedMaterial != null)
            {
                GetTextureFromMaterial(mr.sharedMaterial);
            }
            return;
        }

        // 2. 抓取 2D 網格 (往子物件找)
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            _cachedMesh = ConvertSpriteToMesh(sr.sprite);
            _meshTransform = sr.transform;
            
            if (overrideTexture != null)
                _cachedTexture = overrideTexture;
            else
                _cachedTexture = sr.sprite.texture;
            return;
        }

        Debug.LogError($"[架構錯誤] {gameObject.name} 身上找不到 MeshFilter 或 SpriteRenderer！");
    }

    private void GetTextureFromMaterial(Material mat)
    {
        if (overrideTexture != null)
        {
            _cachedTexture = overrideTexture;
            return;
        }

        // 兼容 URP 與舊版管線
        if (mat.HasProperty("_BaseMap"))
            _cachedTexture = mat.GetTexture("_BaseMap");
        else if (mat.HasProperty("_MainTex"))
            _cachedTexture = mat.GetTexture("_MainTex");
    }

    private Mesh ConvertSpriteToMesh(Sprite sprite)
    {
        Mesh mesh = new Mesh { name = sprite.name + "_GhostMesh" };
        
        Vector2[] spriteVertices = sprite.vertices;
        Vector3[] vertices = new Vector3[spriteVertices.Length];
        for (int i = 0; i < spriteVertices.Length; i++) vertices[i] = spriteVertices[i]; 
        
        ushort[] spriteTriangles = sprite.triangles;
        int[] triangles = new int[spriteTriangles.Length];
        for (int i = 0; i < spriteTriangles.Length; i++) triangles[i] = spriteTriangles[i];
        
        mesh.vertices = vertices;
        mesh.uv = sprite.uv;
        mesh.triangles = triangles;
        
        return mesh;
    }

    private void Update()
    {
        if (_cachedMesh == null || ghostMaterial == null || GhostInstancedManager.Instance == null) return;

        Vector3 currentPos = _meshTransform.position;
        float distanceMoved = Vector3.Distance(_lastSpawnPos, currentPos);

        // 當移動距離大於我們設定的間距時
        if (distanceMoved >= distanceGap)
        {
            // 計算這一幀內到底「跨越」了幾個殘影間距
            int spawnCount = Mathf.FloorToInt(distanceMoved / distanceGap);

            for (int i = 1; i <= spawnCount; i++)
            {
                // 計算補間比例 (例如 0.25, 0.5, 0.75, 1.0)
                float t = (float)i / spawnCount;
            
                // 精準計算出在這一幀內，殘影應該出現的歷史軌跡座標
                Vector3 lerpPos = Vector3.Lerp(_lastSpawnPos, currentPos, t);
            
                // 套用 Z 軸防閃爍偏移
                Vector3 finalPos = lerpPos + posOffset;
                if (autoAntiZFighting) finalPos.z += (Time.time % 1.0f) * 0.01f + (i * 0.001f);

                GhostInstancedManager.Instance.AddGhost(
                    _cachedMesh, 
                    ghostMaterial, 
                    _cachedTexture, 
                    finalPos, 
                    _meshTransform.rotation, 
                    _meshTransform.localScale, 
                    ghostColor, 
                    ghostLifetime
                );
            }

            // 更新最後生成位置
            _lastSpawnPos = currentPos;
        }
    }
}