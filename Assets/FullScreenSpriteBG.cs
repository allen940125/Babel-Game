using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[ExecuteAlways]
public class FullScreenSpriteBG : MonoBehaviour
{
    public enum ScaleMode
    {
        Stretch,       // 強制拉伸填滿螢幕 (可能變形)
        FitAspect,     // 等比例縮放，完整顯示圖片 (黑邊模式)
        CoverAspect    // 等比例縮放，完全填滿螢幕 (裁切溢出模式)
    }

    [SerializeField] private Camera _targetCamera;
    [SerializeField] private ScaleMode _scaleMode = ScaleMode.CoverAspect;
    [SerializeField] private Vector2 _costomSize;

    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_targetCamera == null)
        {
            _targetCamera = Camera.main;
        }
    }

    private void Start()
    {
        ResizeToScreen();
    }

#if UNITY_EDITOR
    private void Update()
    {
        // 編輯器模式下調整視窗大小時即時更新
        if (!Application.isPlaying)
        {
            ResizeToScreen();
        }
    }
#endif

    [ContextMenu("手動更新尺寸")]
    public void ResizeToScreen()
    {
        if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_targetCamera == null) _targetCamera = Camera.main;
        if (_spriteRenderer.sprite == null || _targetCamera == null) return;

        // 1. 強制對齊相機中心 (維持自身 Z 軸距離)
        Vector3 camPos = _targetCamera.transform.position;
        transform.position = new Vector3(camPos.x, camPos.y, transform.position.z);

        // 2. 計算相機視錐體的世界空間寬高
        float worldScreenHeight;
        float worldScreenWidth;

        if (_targetCamera.orthographic)
        {
            worldScreenHeight = _targetCamera.orthographicSize * 2f;
            worldScreenWidth = worldScreenHeight * _targetCamera.aspect;
        }
        else
        {
            // 透視相機 (Perspective) 需依據物體與相機的 Z 軸距離計算
            float distance = Mathf.Abs(transform.position.z - _targetCamera.transform.position.z);
            worldScreenHeight = 2.0f * distance * Mathf.Tan(_targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            worldScreenWidth = worldScreenHeight * _targetCamera.aspect;
        }

        // 3. 取得 Sprite 原始世界大小 (Unit)
        Vector2 spriteSize = _spriteRenderer.sprite.bounds.size;

        // 4. 計算縮放比例
        float scaleX = worldScreenWidth / spriteSize.x;
        float scaleY = worldScreenHeight / spriteSize.y;

        switch (_scaleMode)
        {
            case ScaleMode.Stretch:
                transform.localScale = new Vector3(scaleX, scaleY, 1f);
                break;

            case ScaleMode.FitAspect:
                float fitScale = Mathf.Min(scaleX, scaleY);
                transform.localScale = new Vector3(fitScale, fitScale, 1f);
                break;

            case ScaleMode.CoverAspect:
                float coverScale = Mathf.Max(scaleX, scaleY);
                transform.localScale = new Vector3(coverScale, coverScale, 1f);
                break;
        }

        transform.localScale *= _costomSize;
    }
}