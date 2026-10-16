using System.Collections.Generic;
using UnityEngine;

public class InstancedGhostReporter : MonoBehaviour
{
    [Header("目標抓取設定 (靈活控制)")]
    [Tooltip("如果你只想抓特定的幾個部位，把他們拖進這個陣列。一旦這裡有東西，就會無視下方的自動抓取設定。")]
    public GameObject[] specificParts;

    [Tooltip("是否要自動往下搜尋所有子物件？ (specificParts 為空時才有效)")]
    public bool includeChildren = true;

    [Header("殘影設定")]
    public Material ghostMaterial;
    public Color ghostColor = new Color(0.3f, 0.8f, 1f, 0.6f);
    public float distanceGap = 0.5f;
    public float ghostLifetime = 0.35f;

    [Header("殘影位置微調")]
    public Vector3 posOffset = new Vector3(0, 0, 0.1f);
    public bool autoAntiZFighting = true;

    [Header("進階設定")]
    public Texture overrideTexture;

    // ★ 核心升級：定義一個內部結構，用來儲存這個物件身上的「所有」模型部件
    private struct GhostPart
    {
        public Mesh mesh;
        public Texture texture;
        public Transform transform;
    }
    
    // 將原本單一的 Mesh 變數，改為陣列
    private GhostPart[] _ghostParts;

    private float _spawnTimer;
    private Vector3 _lastSpawnPos;
    // 記錄父物件的座標，用來計算整體移動距離
    private Transform _rootTransform; 

    private void Start()
    {
        _rootTransform = transform;
        _lastSpawnPos = _rootTransform.position;

        List<GhostPart> partsList = new List<GhostPart>();

        // ★ 核心升級：決定我們要處理哪些 GameObject
        List<GameObject> targetsToScan = new List<GameObject>();

        if (specificParts != null && specificParts.Length > 0)
        {
            // 模式 A：設計師手動指定 (絕對控制權)
            targetsToScan.AddRange(specificParts);
        }
        else
        {
            // 模式 B：腳本自動抓取
            if (includeChildren)
            {
                // 把自己與所有子物件都加入清單
                Transform[] allTransforms = GetComponentsInChildren<Transform>();
                foreach (Transform t in allTransforms) targetsToScan.Add(t.gameObject);
            }
            else
            {
                // 只抓自己 (不找子物件)
                targetsToScan.Add(gameObject);
            }
        }

        // 開始從這些目標中提煉出 Mesh 與 Sprite
        foreach (GameObject obj in targetsToScan)
        {
            // 嘗試抓取 3D 網格
            MeshFilter mf = obj.GetComponent<MeshFilter>();
            if (mf != null)
            {
                MeshRenderer mr = obj.GetComponent<MeshRenderer>();
                if (mr != null && mr.sharedMaterial != null)
                {
                    partsList.Add(new GhostPart
                    {
                        mesh = mf.sharedMesh,
                        transform = mf.transform,
                        texture = GetTextureFromMaterial(mr.sharedMaterial)
                    });
                }
            }

            // 嘗試抓取 2D 網格
            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                partsList.Add(new GhostPart
                {
                    mesh = ConvertSpriteToMesh(sr.sprite),
                    transform = sr.transform,
                    texture = overrideTexture != null ? overrideTexture : sr.sprite.texture
                });
            }
        }

        _ghostParts = partsList.ToArray();

        if (_ghostParts.Length == 0)
        {
            Debug.LogWarning($"[設定警告] {gameObject.name} 的抓取規則沒有找到任何有效的 Mesh 或 Sprite！殘影將不會產生。");
        }
    }

    private Texture GetTextureFromMaterial(Material mat)
    {
        if (overrideTexture != null) return overrideTexture;
        if (mat.HasProperty("_BaseMap")) return mat.GetTexture("_BaseMap");
        if (mat.HasProperty("_MainTex")) return mat.GetTexture("_MainTex");
        return null;
    }

    private Mesh ConvertSpriteToMesh(Sprite sprite)
    {
        Mesh mesh = new Mesh { name = sprite.name + "_GhostMesh" };
        mesh.vertices = System.Array.ConvertAll(sprite.vertices, v => (Vector3)v);
        mesh.uv = sprite.uv;
        mesh.triangles = System.Array.ConvertAll(sprite.triangles, t => (int)t);
        return mesh;
    }

    private void Update()
    {
        if (_ghostParts == null || _ghostParts.Length == 0 || ghostMaterial == null || GhostInstancedManager.Instance == null) return;

        Vector3 currentPos = _rootTransform.position;
        float distanceMoved = Vector3.Distance(_lastSpawnPos, currentPos);

        if (distanceMoved >= distanceGap)
        {
            int spawnCount = Mathf.FloorToInt(distanceMoved / distanceGap);

            for (int i = 1; i <= spawnCount; i++)
            {
                float t = (float)i / spawnCount;
                // 父物件預期的插值中心點
                Vector3 rootLerpPos = Vector3.Lerp(_lastSpawnPos, currentPos, t);
                
                // ★ 迴圈通報：把身上的所有部件一起丟給總管
                foreach (var part in _ghostParts)
                {
                    // 計算子物件相對於父物件的正確偏移座標
                    Vector3 localOffset = part.transform.position - currentPos;
                    Vector3 finalPos = rootLerpPos + localOffset + posOffset;
                    
                    if (autoAntiZFighting) finalPos.z += (Time.time % 1.0f) * 0.01f + (i * 0.001f);

                    GhostInstancedManager.Instance.AddGhost(
                        part.mesh, 
                        ghostMaterial, 
                        part.texture, 
                        finalPos, 
                        part.transform.rotation, 
                        part.transform.localScale, 
                        ghostColor, 
                        ghostLifetime
                    );
                }
            }

            _lastSpawnPos = currentPos;
        }
    }
}