using System.Collections.Generic;
using UnityEngine;

public class SkinnedGhostManager : MonoBehaviour
{
    public static SkinnedGhostManager Instance;

    private struct SkinnedGhostNode
    {
        public Mesh bakedMesh;
        public Material material;
        public Texture texture;
        public Matrix4x4 matrix;
        public float age;
        public float lifetime;
        public Color baseColor;
    }

    private List<SkinnedGhostNode> _activeGhosts = new List<SkinnedGhostNode>(256);
    private MaterialPropertyBlock _block;
    private int _colorID;
    private int _baseMapID;
    private int _mainTexID;

    private void Awake()
    {
        Instance = this;
        _block = new MaterialPropertyBlock();
        _colorID = Shader.PropertyToID("_BaseColor");
        _baseMapID = Shader.PropertyToID("_BaseMap");
        _mainTexID = Shader.PropertyToID("_MainTex");
    }

    // 接收從 Reporter 傳來的烘焙網格
    public void AddGhost(Mesh bakedMesh, Material mat, Texture tex, Matrix4x4 matrix, Color color, float lifetime)
    {
        if (bakedMesh == null || mat == null) return;

        _activeGhosts.Add(new SkinnedGhostNode
        {
            bakedMesh = bakedMesh,
            material = mat,
            texture = tex,
            matrix = matrix,
            age = 0f,
            lifetime = lifetime,
            baseColor = color
        });
    }

    private void Update()
    {
        for (int i = _activeGhosts.Count - 1; i >= 0; i--)
        {
            var ghost = _activeGhosts[i];
            ghost.age += Time.deltaTime;

            if (ghost.age >= ghost.lifetime)
            {
                // ★ 絕對不可省略：徹底銷毀烘焙出來的網格，釋放記憶體
                Destroy(ghost.bakedMesh);
                _activeGhosts.RemoveAt(i);
                continue;
            }

            float progress = ghost.age / ghost.lifetime;
            float currentAlpha = Mathf.Lerp(ghost.baseColor.a, 0f, progress);

            _block.SetColor(_colorID, new Color(ghost.baseColor.r, ghost.baseColor.g, ghost.baseColor.b, currentAlpha));
            
            if (ghost.texture != null)
            {
                _block.SetTexture(_baseMapID, ghost.texture);
                _block.SetTexture(_mainTexID, ghost.texture);
            }

            // 呼叫單次渲染 API
            Graphics.DrawMesh(
                ghost.bakedMesh, 
                ghost.matrix, 
                ghost.material, 
                gameObject.layer, 
                null, 
                0, 
                _block, 
                UnityEngine.Rendering.ShadowCastingMode.Off, 
                false
            );

            _activeGhosts[i] = ghost;
        }
    }
}