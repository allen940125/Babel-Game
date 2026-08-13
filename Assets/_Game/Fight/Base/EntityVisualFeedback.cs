using System.Collections;
using UnityEngine;

// 專門處理視覺反饋的組件
public class EntityVisualFeedback : MonoBehaviour
{
    [Header("視覺設定")]
    public SpriteRenderer sr;
    public Color damageColor = Color.red;
    public float flashDuration = 0.15f; // 單次閃爍速度
    public float totalInvincibilityTime = 1.5f; // 視覺上要閃多久

    private void Awake()
    {
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
    }

    // 這個方法要開放給 public，讓 EntityHealthComponent 的 UnityEvent 來呼叫
    public void PlayDamageFlash()
    {
        if (sr != null && gameObject.activeInHierarchy)
        {
            StopAllCoroutines(); // 避免連續受傷導致協程打架
            StartCoroutine(DamageFlashRoutine());
        }
    }

    private IEnumerator DamageFlashRoutine()
    {
        float timer = 0;
        // 在無敵時間內持續閃爍
        while (timer < totalInvincibilityTime)
        {
            Color c = damageColor;
            c.a = (Mathf.FloorToInt(timer / flashDuration) % 2 == 0) ? 0.4f : 1f;
            sr.color = c;
            
            yield return null;
            timer += Time.deltaTime;
        }

        // 結束後恢復原狀
        sr.color = Color.white;
    }
}