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

    [Header("爆炸設定 (時間到未解除)")] // ★ 新增
    [Tooltip("時間到時生成的爆炸特效 Prefab")]
    public GameObject explosionPrefab; 

    // 內部變數
    private LineRenderer _lineRenderer;
    private Vector3 _startPos;
    private float _timer; 
    private bool _hasFinished = false;

    protected override void Awake()
    {
        base.Awake();
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
        CheckProjectileCollision();
        UpdateLineVisual();
    }

    private void HandleMovement()
    {
        if (endPoint == null || movingObject == null || travelDuration <= 0) return;

        _timer += Time.deltaTime;
        float t = 0f;

        if (isOneShot)
        {
            t = Mathf.Clamp01(_timer / travelDuration);
            
            // 如果跑到終點了 (時間到)
            if (t >= 1.0f) 
            {
                _hasFinished = true;
                
                // ★ 1. 生成爆炸特效
                if (explosionPrefab != null)
                {
                    Instantiate(explosionPrefab, movingObject.position, Quaternion.identity);
                }

                // ★ 2. 關閉線條與移動物體
                if (_lineRenderer != null) _lineRenderer.enabled = false;
                if (movingObject != null) movingObject.gameObject.SetActive(false);

                // ★ 3. 關閉整個機關 (Visual Object)
                // 這會讓 IsCleared 變成 true，達成「消失」的效果
                // 就像是被玩家解除了一樣，只是這次是因為時間到自爆
                if (visualObject != null) visualObject.SetActive(false);
            }
        }
        else
        {
            // 如果不是 OneShot 模式，就維持循環
            t = Mathf.Repeat(_timer / travelDuration, 1.0f);
        }

        movingObject.position = Vector3.Lerp(_startPos, endPoint.position, t);
    }
    
    private void CheckProjectileCollision()
    {
        if (movingObject == null) return;

        Collider2D hit = Physics2D.OverlapCircle(movingObject.position, projectileRadius);

        // 如果撞到了目標 (例如 Player)
        if (hit != null && hit.CompareTag(targetTag))
        {
            Debug.Log("移動物體撞到了目標！");
            
            _hasFinished = true; // 標記結束
            
            // 1. 隱藏子彈
            movingObject.gameObject.SetActive(false);

            // 2. 關閉線條
            if (_lineRenderer != null) _lineRenderer.enabled = false;

            // ★★★ 關鍵修正：這裡漏了！讓整個機關也一起消失 ★★★
            // 這樣 IsCleared 才會變 true，Boss 才會知道這個機關被解除了
            if (visualObject != null) visualObject.SetActive(false);
        }
    }
    
    // ... (ApplySafeRandomRotation, InitLineRendererSettings 等保持不變) ...
    // 為了節省篇幅，以下省略未修改的函式，請保留原有的內容
    private void ApplySafeRandomRotation() {
        if (objectToRandomRotate == null) return;
        int maxAttempts = 30; bool foundSafeSpot = false;
        for (int i = 0; i < maxAttempts; i++) {
            float randomAngle = Random.Range(0f, 360f);
            Quaternion tryRotation = Quaternion.Euler(0, 0, randomAngle);
            Vector3 dir = tryRotation * Vector3.up; 
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, fixedDistance, wallLayer);
            if (hit.collider == null) {
                objectToRandomRotate.localRotation = tryRotation;
                if (endPoint != null) {
                    endPoint.position = transform.position + dir * fixedDistance;
                    endPoint.rotation = Quaternion.identity; 
                }
                foundSafeSpot = true; break; 
            }
        }
        if (!foundSafeSpot) {
            objectToRandomRotate.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
            if (endPoint != null) endPoint.rotation = Quaternion.identity;
        }
    }
    private void InitLineRendererSettings() {
        if (_lineRenderer != null) {
            _lineRenderer.startWidth = lineWidth; _lineRenderer.endWidth = lineWidth;
            _lineRenderer.startColor = lineColor; _lineRenderer.endColor = lineColor;
            _lineRenderer.sortingOrder = 10; 
        }
    }
    private void UpdateLineVisual()
    {
        // ★ 強制檢查：如果機關已經壞了(隱藏了) 或是 任務結束了
        // 就直接強制關閉線條，並離開，不准再畫線！
        if (IsCleared || (isOneShot && _hasFinished))
        {
            if (_lineRenderer != null) _lineRenderer.enabled = false;
            return;
        }

        // 正常畫線邏輯
        if (_lineRenderer != null && endPoint != null)
        {
            // 確保線是開著的 (因為有可能被誤關，所以在正常運作時要開著)
            _lineRenderer.enabled = true; 
            
            _lineRenderer.startWidth = lineWidth;
            _lineRenderer.endWidth = lineWidth;
            _lineRenderer.startColor = lineColor;
            _lineRenderer.endColor = lineColor;

            _lineRenderer.SetPosition(0, _startPos);
            _lineRenderer.SetPosition(1, endPoint.position);
        }
    }
    
    private void OnTriggerStay2D(Collider2D other) 
    { 
        if (other.CompareTag(targetTag)) 
        { 
            if(visualObject) visualObject.SetActive(false); 
            if(_lineRenderer != null) _lineRenderer.enabled = false;
            if(movingObject != null) movingObject.gameObject.SetActive(false);
        } 
    }
    public override void ResetMechanism() { base.ResetMechanism(); ApplySafeRandomRotation(); _timer = 0f; _hasFinished = false; }
}