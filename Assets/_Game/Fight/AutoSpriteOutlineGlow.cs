using UnityEngine;

// 這行讓腳本在編輯模式下也會執行 Update
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class SimpleSpriteGlow : MonoBehaviour
{
    [Header("核心開關")]
    [Tooltip("是否開啟發光效果")]
    public bool isGlowing = true;

    [Header("發光設定")]
    public Color glowColor = Color.red;
    public float scaleMultiplier = 1.2f;
    [Range(0, 1)] public float alpha = 0.6f;

    [Header("呼吸燈效果")]
    public bool useBreathing = true;
    public float breatheSpeed = 2f;
    public float breatheRange = 0.1f;

    // 內部變數
    private SpriteRenderer _ownerSprite;
    private SpriteRenderer _glowSprite;
    private GameObject _glowObject;
    private Material _pureColorMaterial;

    private const string GLOW_CHILD_NAME = "GlowEffect_Auto";
    
    void OnEnable()
    {
        // 初始化
        _ownerSprite = GetComponent<SpriteRenderer>();
        CheckAndCreateGlowObject();
    }

    void LateUpdate()
    {
        // 防呆：如果父物件沒有 SpriteRenderer，就沒戲唱
        if (_ownerSprite == null) return;

        // 1. 檢查開關 & 確保子物件存在
        if (!isGlowing)
        {
            if (_glowObject != null && _glowObject.activeSelf)
                _glowObject.SetActive(false);
            return;
        }

        // 如果開關是開的，但子物件不見了(被誤刪)，嘗試重建
        if (_glowObject == null)
        {
            CheckAndCreateGlowObject();
            if (_glowObject == null) return; // 真的建不出來就放棄
        }

        // 確保它是開啟的
        if (!_glowObject.activeSelf) _glowObject.SetActive(true);

        // 2. 同步 Sprite (這是關鍵：Boss 換圖片，影子也要換)
        if (_glowSprite.sprite != _ownerSprite.sprite)
        {
            _glowSprite.sprite = _ownerSprite.sprite;
        }

        // 3. 同步翻轉
        _glowSprite.flipX = _ownerSprite.flipX;
        _glowSprite.flipY = _ownerSprite.flipY;

        // 4. 同步層級 (永遠在 Boss 後面)
        _glowSprite.sortingLayerID = _ownerSprite.sortingLayerID;
        _glowSprite.sortingOrder = _ownerSprite.sortingOrder - 1;

        // 5. 設定顏色與透明度
        Color finalColor = glowColor;
        finalColor.a = alpha;
        _glowSprite.color = finalColor;

        // 6. 計算呼吸與縮放
        float currentScale = scaleMultiplier;
        
        // 注意：在編輯器模式下 Application.isPlaying 為 false，我們用 Editor 的時間或系統時間來模擬動畫
        float timeVar = Application.isPlaying ? Time.time : (float)UnityEditor.EditorApplication.timeSinceStartup;

        if (useBreathing)
        {
            float breathe = Mathf.PingPong(timeVar * breatheSpeed, breatheRange);
            currentScale += breathe;
        }

        _glowObject.transform.localScale = Vector3.one * currentScale;
        _glowObject.transform.localPosition = Vector3.zero;
        _glowObject.transform.localRotation = Quaternion.identity;
    }

    // 檢查並建立發光子物件
    private void CheckAndCreateGlowObject()
    {
        // 先找找看是不是已經有了 (避免編輯器重複一直生)
        Transform existingChild = transform.Find(GLOW_CHILD_NAME);
        
        if (existingChild != null)
        {
            _glowObject = existingChild.gameObject;
            _glowSprite = _glowObject.GetComponent<SpriteRenderer>();
        }
        else
        {
            // 沒找到才建立新的
            _glowObject = new GameObject(GLOW_CHILD_NAME);
            _glowObject.transform.SetParent(transform);
            _glowSprite = _glowObject.AddComponent<SpriteRenderer>();
            
            // 為了不讓這個生成的物件弄亂你的 Hierarchy，可以把它設為 HideAndDontSave (選用)
            // _glowObject.hideFlags = HideFlags.DontSave; 
        }

        // 設定純色材質 (關鍵：解決黑色圖片無法染色的問題)
        if (_glowSprite != null)
        {
            // 使用 GUI/Text Shader 可以讓圖片變成無視光影的純色塊
            if (_glowSprite.sharedMaterial == null || _glowSprite.sharedMaterial.name != "GlowPureMat")
            {
                // 這裡我們直接動態給它一個內建的 Shader
                Material mat = new Material(Shader.Find("GUI/Text Shader"));
                mat.name = "GlowPureMat";
                mat.hideFlags = HideFlags.DontSave; // 不要在專案裡存成實體檔案
                _glowSprite.material = mat;
            }
        }
    }

    // 當腳本被移除時，順手把子物件清掉 (保持整潔)
    void OnDestroy()
    {
        if (_glowObject != null)
        {
            if (Application.isPlaying) Destroy(_glowObject);
            else DestroyImmediate(_glowObject); // 編輯模式下要用 DestroyImmediate
        }
    }
}