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

    [Header("受擊特效")]
    public GameObject bulletHoleEffectPrefab;
    public Vector3 hitNormal = Vector3.back;

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

        SpawnHitEffect(transform.position, hitNormal);
    }

    // 提供重載供外部直接傳入確切命中點與法線
    public void PlayDamageFlash(Vector3 hitPoint, Vector3 normal)
    {
        SpawnHitEffect(hitPoint, normal);

        if (sr != null && gameObject.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(DamageFlashRoutine());
        }
    }

    private void SpawnHitEffect(Vector3 point, Vector3 normal)
    {
        if (bulletHoleEffectPrefab == null) return;

        // 在受擊位置生成掛載 BulletHoleEffects 的預製體
        GameObject effectObj = Instantiate(bulletHoleEffectPrefab, point, Quaternion.identity);
        BulletHoleEffects effect = effectObj.GetComponent<BulletHoleEffects>();

        if (effect != null)
        {
            effect.PlayExplodeEffect(point, normal, transform);
            // 彈孔生成完畢後，發射器本體即可銷毀，彈孔已掛在 transform 下
            Destroy(effectObj, 0.1f);
        }
        else
        {
            Destroy(effectObj);
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