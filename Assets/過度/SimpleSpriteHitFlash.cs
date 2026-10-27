using System.Collections;
using UnityEngine;

public class SimpleSpriteHitFlash : MonoBehaviour
{
    [Tooltip("要變色的目標 Sprite (沒填的話會自動抓自己身上的)")]
    [SerializeField] private SpriteRenderer targetSprite;
    
    [Tooltip("受擊時要變成的顏色")]
    [SerializeField] private Color flashColor = Color.red;
    
    [Tooltip("變色維持的時間")]
    [SerializeField] private float flashDuration = 0.15f;

    private Color _originalColor;
    private Coroutine _flashCoroutine;

    private void Awake()
    {
        if (targetSprite == null) targetSprite = GetComponent<SpriteRenderer>();
        
        if (targetSprite != null)
        {
            // 記住原本的顏色 (通常是白色)
            _originalColor = targetSprite.color;
        }
    }

    /// <summary>
    /// 給 UnityEvent (onTakeDamageVisuals) 呼叫的公開方法
    /// </summary>
    public void TriggerFlash()
    {
        if (targetSprite == null) return;

        // 如果短時間內連續被打到，打斷上一次的計時，重新閃爍
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
        }
        _flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // 變成紅色
        targetSprite.color = flashColor;
        
        // 等待
        yield return new WaitForSeconds(flashDuration);
        
        // 變回原色
        targetSprite.color = _originalColor;
    }

    private void OnDisable()
    {
        // 防呆：如果敵人在閃紅色的瞬間死亡被隱藏 (SetActive(false))，確保顏色會重置回來
        if (targetSprite != null)
        {
            targetSprite.color = _originalColor;
        }
    }
}