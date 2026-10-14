using System.Collections.Generic;
using UnityEngine;

public class GhostInstancedManager : MonoBehaviour
{
    public static GhostInstancedManager Instance;

    private struct GhostData
    {
        public Matrix4x4 matrix;
        public float age;
        public float lifetime;
        public Color baseColor;
    }

    private class GhostBatch
    {
        public Mesh mesh;
        public Material material;
        public Texture texture; // ★ 新增：紀錄這批殘影共用的貼圖
        public List<GhostData> activeGhosts = new List<GhostData>(1024);
        public Matrix4x4[] matrices = new Matrix4x4[1023];
        public Vector4[] colors = new Vector4[1023];
    }
    
    [Header("全域殘影衰減控制")]
    [Tooltip("X軸為時間進度(0~1)，Y軸為透明度倍率(0~1)")]
    public AnimationCurve alphaFadeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    private Dictionary<string, GhostBatch> _batches = new Dictionary<string, GhostBatch>();
    private MaterialPropertyBlock _block;
    private int _colorID;
    // 1. 新增變數
    private int _mainTexID; 
    private int _baseMapID; 

    private void Awake()
    {
        Instance = this;
        _block = new MaterialPropertyBlock();
        _colorID = Shader.PropertyToID("_BaseColor");
    
        // 2. 獲取兩種命名的 ID
        _mainTexID = Shader.PropertyToID("_MainTex"); 
        _baseMapID = Shader.PropertyToID("_BaseMap"); // URP 專用
    }

    // ★ API 新增 Texture 參數
    public void AddGhost(Mesh mesh, Material mat, Texture tex, Vector3 pos, Quaternion rot, Vector3 scale, Color color, float lifetime)
    {
        if (mesh == null || mat == null) return;

        // ★ 鑰匙加入 Texture ID。這樣不同貼圖的子彈就會自動分開 Draw Call，不會混在一起
        string key = mesh.GetInstanceID() + "_" + mat.GetInstanceID() + "_" + (tex != null ? tex.GetInstanceID() : 0);

        if (!_batches.TryGetValue(key, out GhostBatch batch))
        {
            batch = new GhostBatch { mesh = mesh, material = mat, texture = tex };
            _batches.Add(key, batch);
        }

        if (batch.activeGhosts.Count >= 1023) return;

        batch.activeGhosts.Add(new GhostData
        {
            matrix = Matrix4x4.TRS(pos, rot, scale),
            age = 0f,
            lifetime = lifetime,
            baseColor = color
        });
    }

    private void Update()
    {
        foreach (var batch in _batches.Values)
        {
            if (batch.activeGhosts.Count == 0) continue;

            int count = 0;
            for (int i = batch.activeGhosts.Count - 1; i >= 0; i--)
            {
                var ghost = batch.activeGhosts[i];
                ghost.age += Time.deltaTime;

                if (ghost.age >= ghost.lifetime)
                {
                    batch.activeGhosts.RemoveAt(i);
                    continue;
                }

                // 計算正規化的時間進度 (0 到 1)
                float progress = ghost.age / ghost.lifetime;
                
                // ★ 核心修改：利用曲線計算當下的透明度倍率
                float curveMultiplier = alphaFadeCurve.Evaluate(progress);
                float currentAlpha = ghost.baseColor.a * curveMultiplier;

                batch.matrices[count] = ghost.matrix;
                batch.colors[count] = new Vector4(ghost.baseColor.r, ghost.baseColor.g, ghost.baseColor.b, currentAlpha);
                
                batch.activeGhosts[i] = ghost;
                count++;
            }

            if (count > 0)
            {
                _block.SetVectorArray(_colorID, batch.colors);
                
                // ★ 關鍵：在呼叫顯卡繪製前，將這批次專屬的貼圖動態塞入 Block
                if (batch.texture != null)
                {
                    // 3. 暴力雙傳：不管 Shader 叫什麼名字，全部塞進去
                    _block.SetTexture(_mainTexID, batch.texture);
                    _block.SetTexture(_baseMapID, batch.texture);
                }

                Graphics.DrawMeshInstanced(
                    batch.mesh, 0, batch.material, batch.matrices, count, _block, 
                    UnityEngine.Rendering.ShadowCastingMode.Off, false, gameObject.layer
                );
            }
        }
    }
}