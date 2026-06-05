using UnityEngine;
using Gamemanager; // 引用你的事件系統
using System.Collections;

public class CameraShaker : MonoBehaviour
{
    [Header("預設受傷震動參數")]
    public float defaultHitIntensity = 0.3f;
    public float defaultHitDuration = 0.2f;

    [Header("瀕死持續震動參數")]
    public float lowHealthIntensity = 0.05f; // 持續晃動的幅度(要小一點)
    public float lowHealthSpeed = 10f;       // 晃動的速度

    // 內部狀態
    private Vector3 _initialPosition;
    private bool _isHitShaking = false;      // 是否正在播放受傷震動
    private bool _isLowHealthShaking = false;// 是否處於瀕死狀態
    
    // 用來儲存受傷震動當下的偏移量
    private Vector3 _currentHitOffset = Vector3.zero;

    void OnEnable()
    {
        _initialPosition = transform.localPosition;

        if (GameManager.Instance != null && GameManager.Instance.MainGameEvent != null)
        {
            // 訂閱事件 (使用 Lambda 接收參數)
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossTakeDamageEvent, OnBossDamaged);
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossLowHealthStateEvent, OnLowHealthState);
        }
    }

    void OnDisable()
    {
        if (GameManager.Instance != null && GameManager.Instance.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.Unsubscribe<BossTakeDamageEvent>(OnBossDamaged);
            GameManager.Instance.MainGameEvent.Unsubscribe<BossLowHealthStateEvent>(OnLowHealthState);
        }
    }

    // --- 事件處理 ---

    private void OnBossDamaged(BossTakeDamageEvent cmd)
    {
        // 接收參數，如果傳入 0 則使用預設值
        float intensity = cmd.Intensity > 0 ? cmd.Intensity : defaultHitIntensity;
        float duration = cmd.Duration > 0 ? cmd.Duration : defaultHitDuration;

        StopCoroutine("HitShakeRoutine");
        StartCoroutine(HitShakeRoutine(intensity, duration));
    }

    private void OnLowHealthState(BossLowHealthStateEvent cmd)
    {
        _isLowHealthShaking = cmd.IsActive;
        
        // 如果關閉瀕死震動，且目前沒有受傷震動，就強制歸位
        if (!cmd.IsActive && !_isHitShaking)
        {
            transform.localPosition = _initialPosition;
        }
    }

    // --- 震動邏輯 ---

    private IEnumerator HitShakeRoutine(float intensity, float duration)
    {
        _isHitShaking = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // 產生隨機震動偏移
            _currentHitOffset = (Vector3)Random.insideUnitCircle * intensity;
            elapsed += Time.deltaTime;
            yield return null;
        }

        _currentHitOffset = Vector3.zero;
        _isHitShaking = false;
        
        // 震動結束後，如果沒有瀕死狀態，就歸位
        if (!_isLowHealthShaking)
        {
            transform.localPosition = _initialPosition;
        }
    }

    void LateUpdate()
    {
        Vector3 finalPos = _initialPosition;

        if (_isHitShaking)
        {
            // 情況 A: 正在受傷震動 -> 使用受傷偏移
            finalPos += _currentHitOffset;
        }
        else if (_isLowHealthShaking)
        {
            // 情況 B: 沒受傷但瀕死 -> 使用平滑的持續晃動 (Perlin Noise 或 Lerp)
            // 這裡用簡單的隨機 Lerp 模擬不穩定的鏡頭
            Vector3 shakeTarget = (Vector3)Random.insideUnitCircle * lowHealthIntensity;
            
            // 使用 Lerp 讓它看起來像是在"飄動"而不是"劇烈抖動"
            // 我們把這個飄動效果疊加在初始位置上
            // 為了簡單，這裡直接用 noise 模擬
            float x = (Mathf.PingPong(Time.time * lowHealthSpeed, 1f) - 0.5f) * lowHealthIntensity;
            float y = (Mathf.PingPong(Time.time * lowHealthSpeed * 0.8f, 1f) - 0.5f) * lowHealthIntensity;
            
            finalPos += new Vector3(x, y, 0);
        }

        transform.localPosition = finalPos;
    }
}