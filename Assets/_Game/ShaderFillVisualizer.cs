using UnityEngine;
// using Sirenix.OdinInspector; // 若有使用 Odin，可解除此註解

[RequireComponent(typeof(Renderer))]
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

    // ==========================================
    // 編輯器自動計算功能
    // ==========================================
    
    // [Button("自動計算並寫入邊界 (依據材質方向)")]
    [ContextMenu("Calculate Bounds")]
    public void SetupBounds()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        if (targetRenderer == null) return;

        // 區分編輯器環境與執行期，避免在編輯器下生成 Instance 材質導致記憶體洩漏
        Material mat = Application.isPlaying ? targetRenderer.material : targetRenderer.sharedMaterial;
        if (mat == null) return;

        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogError("[錯誤] 找不到 MeshFilter 或 Mesh，無法計算邊界。");
            return;
        }

        // 1. 獲取 Shader 中設定的填充方向 (Local Space)
        Vector3 fillDir = mat.GetVector("_FillDirection").normalized;
        if (fillDir == Vector3.zero) 
        {
            Debug.LogWarning("[警告] 填充方向為 (0,0,0)，強制預設為 (0,1,0)");
            fillDir = Vector3.up; 
        }

        // 2. 獲取模型的本地邊界盒 (AABB)
        Bounds bounds = mf.sharedMesh.bounds;

        // 3. 展開 Bounding Box 的 8 個頂點
        Vector3[] corners = new Vector3[8];
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        corners[0] = new Vector3(min.x, min.y, min.z);
        corners[1] = new Vector3(min.x, min.y, max.z);
        corners[2] = new Vector3(min.x, max.y, min.z);
        corners[3] = new Vector3(min.x, max.y, max.z);
        corners[4] = new Vector3(max.x, min.y, min.z);
        corners[5] = new Vector3(max.x, min.y, max.z);
        corners[6] = new Vector3(max.x, max.y, min.z);
        corners[7] = new Vector3(max.x, max.y, max.z);

        // 4. 將 8 個頂點投影到填充方向上，找出極值
        float minBound = float.MaxValue;
        float maxBound = float.MinValue;

        foreach (Vector3 corner in corners)
        {
            float projection = Vector3.Dot(corner, fillDir);
            if (projection < minBound) minBound = projection;
            if (projection > maxBound) maxBound = projection;
        }

        // 5. 將結果寫回材質球
        mat.SetFloat("_MinBound", minBound);
        mat.SetFloat("_MaxBound", maxBound);

        Debug.Log($"[邊界計算成功] 物件: {gameObject.name} | 方向: {fillDir} | Min: {minBound} | Max: {maxBound}");
    }
}