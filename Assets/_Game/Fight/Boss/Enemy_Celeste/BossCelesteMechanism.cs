using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BossCelesteMechanism : BossSpecialMechanism
{
    [Header("Celeste 機關設定")]
    public Transform endPoint;
    public Transform movingObject;

    [Header("位置與牆壁偵測")]
    public Transform objectToRandomRotate;
    public float fixedDistance = 5.0f;
    public LayerMask wallLayer; 

    [Header("外觀與時間控制")]
    public float lineWidth = 0.1f;
    [ColorUsage(true, true)]
    public Color lineColor = Color.red;
    public float travelDuration = 2.0f;
    public bool isOneShot = true;

    [Header("碰撞設定")]
    public float projectileRadius = 0.3f;

    [Header("爆炸設定")] 
    public GameObject explosionPrefab; 

    private LineRenderer _lineRenderer;
    private Vector3 _startPos;
    private float _timer; 
    private bool _hasFinished = false;

    protected override void Awake()
    {
        base.Awake(); 
        
        // 1. 強制關掉本體碰撞體，避免與移動物體衝突
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
            Debug.Log($"[{gameObject.name}] Celeste 機關本體 Collider 已安全閹割(禁用)，將完全依賴子彈偵測。");
        }

        _lineRenderer = GetComponent<LineRenderer>();
        _startPos = transform.position; 
        
        if (_lineRenderer.material == null || _lineRenderer.material.name.Contains("Default-Line"))
        {
             _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }
    }

    private void Start()
    {
        InitLineRendererSettings();
    }

    private void OnEnable()
    {
        ApplySafeRandomRotation();
        
        _timer = 0f;
        _hasFinished = false;
        
        if (_lineRenderer != null) _lineRenderer.enabled = true;

        if (movingObject != null)
        {
            movingObject.position = _startPos;
            movingObject.gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        if (IsCleared) return; 
        
        if (isOneShot && _hasFinished) 
        {
            if (_lineRenderer != null) _lineRenderer.enabled = false;
            return;
        }

        HandleMovement();
        //CheckProjectileCollision();
        UpdateLineVisual();
    }

    protected override void OnTriggerEnter(Collider other)
    {
        //base.OnTriggerEnter(other);
    }

    // 開放給 SmashableTarget 呼叫的強制處決通道
    public void ForceTrigger()
    {
        Debug.Log($"<color=magenta>[強制處決] 機關 {gameObject.name} 被主動砸毀！</color>");
        TriggerThisMechanism(); 
    }
    
    // 當成功扣除 Boss 秒數後，父類別會自動呼叫這個擴充點
    protected override void OnMechanismTriggered() 
    { 
        // 確保被砸毀時，Celeste 專屬的子彈和雷射線會乖乖消失
        _hasFinished = true;
        if (_lineRenderer != null) _lineRenderer.enabled = false;
        if (movingObject != null) movingObject.gameObject.SetActive(false);
    }
    
    private void HandleMovement()
    {
        if (endPoint == null || movingObject == null || travelDuration <= 0) return;

        _timer += Time.deltaTime;
        float t = 0f;

        if (isOneShot)
        {
            t = Mathf.Clamp01(_timer / travelDuration);
            if (t >= 1.0f) 
            {
                _hasFinished = true;
                Debug.Log($"<color=gray>[Celeste機關] {gameObject.name} 到達終點，時間耗盡自爆。</color>");
                
                if (explosionPrefab != null)
                {
                    Instantiate(explosionPrefab, movingObject.position, Quaternion.identity);
                }

                if (_lineRenderer != null) _lineRenderer.enabled = false;
                if (movingObject != null) movingObject.gameObject.SetActive(false);
                if (visualObject != null) visualObject.SetActive(false);
            }
        }
        else
        {
            t = Mathf.Repeat(_timer / travelDuration, 1.0f);
        }

        movingObject.position = Vector3.Lerp(_startPos, endPoint.position, t);
    }
    
    // ★ 大量增加 Debug 的物理碰撞診斷
    private void CheckProjectileCollision()
    {
        if (movingObject == null) return;

        // 使用 3D 球體偵測，取得周遭所有的碰撞體
        Collider[] hits = Physics.OverlapSphere(movingObject.position, projectileRadius);

        if (hits.Length > 0)
        {
            // Debug: 顯示掃描到了什麼，以及對方的 Z 軸。
            // 這可以解決「其實位置有重疊，但 Z 軸不同導致穿過去」的 Unity 常見 3D 坑
            foreach (var hit in hits)
            {
                float zDistance = Mathf.Abs(movingObject.position.z - hit.transform.position.z);
                Debug.Log($"<color=white>[碰撞探測] 正在碰觸: {hit.gameObject.name} | Tag: {hit.gameObject.tag} | Z軸距離差: {zDistance:F2}</color>");

                // ★ 檢查 Tag 是否精確匹配
                if (hit.CompareTag(targetTag))
                {
                    Debug.Log($"<color=green>[攔截成功] 玩家 {hit.gameObject.name} (Tag: {hit.gameObject.tag}) 碰觸到移動物體，執行扣秒！</color>");
                    
                    _hasFinished = true; 
                    
                    TriggerThisMechanism(); 
                    
                    if (_lineRenderer != null) _lineRenderer.enabled = false;
                    movingObject.gameObject.SetActive(false);
                    break; 
                }
                else if (hit.gameObject.name != "BasePlane" && hit.gameObject.name != "Main Camera") // 排除雜音
                {
                    // 警報：撞到了東西，但因為 Tag 不對被無視了
                    Debug.LogWarning($"[Tag不符警告] 移動物體撞到了 {hit.gameObject.name}，但其 Tag 為 '{hit.gameObject.tag}'，不等於設定的目標 '{targetTag}'！");
                }
            }
        }
    }
    
    private void ApplySafeRandomRotation() 
    {
        if (objectToRandomRotate == null) return;
        int maxAttempts = 30; 
        bool foundSafeSpot = false;
        
        for (int i = 0; i < maxAttempts; i++) 
        {
            float randomAngle = Random.Range(0f, 360f);
            Quaternion tryRotation = Quaternion.Euler(0, 0, randomAngle);
            Vector3 dir = tryRotation * Vector3.up; 
            
            // 將 Raycast 視覺化，在編輯器內可以看見它的雷達掃描！ (持續 1 秒的藍線)
            Debug.DrawRay(transform.position, dir * fixedDistance, Color.blue, 1.0f);

            if (!Physics.Raycast(transform.position, dir, fixedDistance, wallLayer)) 
            {
                // 安全，畫綠線表示
                Debug.DrawRay(transform.position, dir * fixedDistance, Color.green, 2.0f);
                objectToRandomRotate.localRotation = tryRotation;
                if (endPoint != null) 
                {
                    endPoint.position = transform.position + dir * fixedDistance;
                    endPoint.rotation = Quaternion.identity; 
                }
                foundSafeSpot = true; 
                break; 
            }
        }

        if (!foundSafeSpot) 
        {
            Debug.LogWarning($"[{gameObject.name}] 隨機發射角度 30 次都遇到牆壁，強制使用隨機角度！");
            objectToRandomRotate.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
            if (endPoint != null) endPoint.rotation = Quaternion.identity;
        }
    }

    private void InitLineRendererSettings() 
    {
        if (_lineRenderer != null) 
        {
            _lineRenderer.startWidth = lineWidth; _lineRenderer.endWidth = lineWidth;
            _lineRenderer.startColor = lineColor; _lineRenderer.endColor = lineColor;
            _lineRenderer.sortingOrder = 10; 
        }
    }

    private void UpdateLineVisual()
    {
        if (IsCleared || (isOneShot && _hasFinished))
        {
            if (_lineRenderer != null) _lineRenderer.enabled = false;
            return;
        }

        if (_lineRenderer != null && endPoint != null)
        {
            _lineRenderer.enabled = true; 
            _lineRenderer.startWidth = lineWidth;
            _lineRenderer.endWidth = lineWidth;
            _lineRenderer.startColor = lineColor;
            _lineRenderer.endColor = lineColor;
            _lineRenderer.SetPosition(0, _startPos);
            _lineRenderer.SetPosition(1, endPoint.position);
        }
    }

    // ★ 開放 Gizmos 繪製，在 Scene 畫出紅色半透明球體，與前方射線。 
    // 你可以在不點擊的情況下隨時觀察這顆子彈的物理碰撞判定大小！
    private void OnDrawGizmos()
    {
        if (movingObject != null)
        {
            // 畫判定球
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f); // 半透明橘色
            Gizmos.DrawWireSphere(movingObject.position, projectileRadius);
            
            // 畫跟隨移動軌跡線
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, movingObject.position);
        }
    }

    public override void ResetMechanism() 
    { 
        base.ResetMechanism(); 
        ApplySafeRandomRotation(); 
        _timer = 0f; 
        _hasFinished = false; 
    }
}