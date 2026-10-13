using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 3D 物件拖曳殘影(只負責殘影,不處理拖曳)。
/// 拖曳由 DraggableBehavior3D 控制,本腳本只「讀取」它的拖曳狀態,不會修改它。
/// 用法:掛在與 DraggableBehavior3D 同一個物件上,指定半透明材質(Ghost Material)。
/// </summary>
[RequireComponent(typeof(DraggableBehavior3D))]
public class DragAfterimage : MonoBehaviour
{
    [Header("殘影")]
    public Material ghostMaterial;           // 半透明材質(URP 請用 Unlit + Transparent)
    public Color ghostColor = new Color(0.3f, 0.8f, 1f, 0.6f);
    public float spawnInterval = 0.04f;      // 每隔幾秒產生一個殘影
    public float minMoveDistance = 0.05f;    // 移動超過這個距離才產生
    public float ghostLifetime = 0.35f;      // 殘影存活時間
    public bool shrink = false;              // 殘影是否逐漸縮小

    class Ghost
    {
        public GameObject go;
        public Material[] mats;
        public List<Mesh> bakedMeshes = new List<Mesh>(); // SkinnedMesh 烘焙出來的,需手動銷毀
        public float age;
    }

    readonly List<Ghost> ghosts = new List<Ghost>();

    DraggableBehavior3D draggable;
    FieldInfo isDraggingField;               // 讀取 DraggableBehavior3D 的私有欄位 _isDragging
    bool wasDragging;
    float spawnTimer;
    Vector3 lastSpawnPos;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    void Awake()
    {
        draggable = GetComponent<DraggableBehavior3D>();

        // 不修改 DraggableBehavior3D,用反射唯讀取得拖曳狀態
        isDraggingField = typeof(DraggableBehavior3D).GetField(
            "_isDragging", BindingFlags.Instance | BindingFlags.NonPublic);

        if (isDraggingField == null)
            Debug.LogWarning("[DragAfterimage] 找不到 DraggableBehavior3D._isDragging," +
                             "改為「只要物件移動就產生殘影」。");
    }

    void LateUpdate()
    {
        bool dragging = IsDragging();

        if (dragging && !wasDragging)
        {
            spawnTimer = 0f;
            lastSpawnPos = transform.position;
        }
        wasDragging = dragging;

        if (dragging)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval &&
                (transform.position - lastSpawnPos).sqrMagnitude >= minMoveDistance * minMoveDistance)
            {
                SpawnGhost();
                spawnTimer = 0f;
                lastSpawnPos = transform.position;
            }
        }

        UpdateGhosts();
    }

    bool IsDragging()
    {
        if (draggable == null || !draggable.enabled) return false;
        if (isDraggingField != null) return (bool)isDraggingField.GetValue(draggable);

        // 退而求其次:找不到欄位時,以「位置有變動」視為拖曳中
        return (transform.position - lastSpawnPos).sqrMagnitude >= minMoveDistance * minMoveDistance;
    }

    // ───────────────────────── 殘影 ─────────────────────────
    void SpawnGhost()
    {
        if (ghostMaterial == null) return;

        var root = new GameObject("Ghost");
        var ghost = new Ghost { go = root };
        var mats = new List<Material>();

        foreach (var mr in GetComponentsInChildren<MeshRenderer>())
        {
            var mf = mr.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;
            AddPart(root.transform, mr.transform, mf.sharedMesh, mats);
        }

        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var baked = new Mesh();
            smr.BakeMesh(baked);
            ghost.bakedMeshes.Add(baked);
            AddPart(root.transform, smr.transform, baked, mats);
        }

        ghost.mats = mats.ToArray();
        ghosts.Add(ghost);
    }

    void AddPart(Transform ghostRoot, Transform source, Mesh mesh, List<Material> matList)
    {
        var part = new GameObject("part");
        part.transform.SetParent(ghostRoot, false);
        part.transform.SetPositionAndRotation(source.position, source.rotation);
        part.transform.localScale = source.lossyScale;

        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = part.AddComponent<MeshRenderer>();
        var mat = new Material(ghostMaterial);
        SetAlpha(mat, ghostColor, ghostColor.a);
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        matList.Add(mat);
    }

    void UpdateGhosts()
    {
        for (int i = ghosts.Count - 1; i >= 0; i--)
        {
            var g = ghosts[i];
            g.age += Time.deltaTime;
            float k = Mathf.Clamp01(g.age / ghostLifetime);

            if (k >= 1f)
            {
                DestroyGhost(g);
                ghosts.RemoveAt(i);
                continue;
            }

            float a = ghostColor.a * (1f - k);
            foreach (var m in g.mats) SetAlpha(m, ghostColor, a);
            if (shrink) g.go.transform.localScale = Vector3.one * (1f - k * 0.5f);
        }
    }

    static void DestroyGhost(Ghost g)
    {
        foreach (var m in g.mats) if (m != null) Destroy(m);
        foreach (var mesh in g.bakedMeshes) if (mesh != null) Destroy(mesh);
        if (g.go != null) Destroy(g.go);
    }

    static void SetAlpha(Material m, Color baseColor, float alpha)
    {
        var c = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, c);   // URP / HDRP
        if (m.HasProperty(ColorId)) m.SetColor(ColorId, c);           // Built-in
    }

    void OnDisable()
    {
        foreach (var g in ghosts) DestroyGhost(g);
        ghosts.Clear();
        wasDragging = false;
    }
}