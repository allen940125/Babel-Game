using System.Collections;
using UnityEngine;

// 專門處理視覺反饋的組件 (包含受擊閃爍與殘血警示)
public class EntityVisualFeedback : MonoBehaviour
{
    [Header("視覺設定")]
    public SpriteRenderer sr;
    public Color damageColor = Color.red;
    public float flashDuration = 0.15f; 
    public float totalInvincibilityTime = 1.5f; 

    [Header("殘血警示設定 (Low Health)")]
    public bool enableLowHealthFlash = true;
    [Tooltip("血量低於此比例時開始平滑閃紅 (例如 0.3 = 30%)")]
    public float lowHealthThreshold = 0.3f;
    public Color lowHealthColor = new Color(1f, 0.4f, 0.4f); // 淡淡的紅色
    public float lowHealthPulseSpeed = 4f; // 呼吸燈閃爍速度

    [Header("受擊特效")]
    public GameObject bulletHoleEffectPrefab;
    public Vector3 hitNormal = Vector3.back;

    private EntityCore _core;
    private bool _isDamageFlashing = false;
    private Color _baseColor = Color.white;

    private void Awake()
    {
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        // 抓取大腦，用來隨時監聽血量比例
        _core = GetComponentInParent<EntityCore>();
    }

    private void Update()
    {
        // 如果正在播放受擊閃爍，或者目標已死，暫停殘血呼吸燈運算
        if (_isDamageFlashing || sr == null || !enableLowHealthFlash || _core == null || _core.RuntimeData == null) return;

        float hpRatio = (float)_core.RuntimeData.CurrentHealth / _core.RuntimeData.MaxHealth;

        // 判斷是否處於殘血且活著
        if (hpRatio > 0 && hpRatio <= lowHealthThreshold)
        {
            // 利用 PingPong 做出平滑呼吸燈效果 (0 到 1 之間來回)
            float lerp = Mathf.PingPong(Time.time * lowHealthPulseSpeed, 1f);
            sr.color = Color.Lerp(_baseColor, lowHealthColor, lerp);
        }
        else
        {
            // 血量健康時保持原色
            sr.color = _baseColor;
        }
    }

    // ==========================================
    // 受擊閃爍邏輯
    // ==========================================
    public void PlayDamageFlash()
    {
        if (sr != null && gameObject.activeInHierarchy)
        {
            StopAllCoroutines(); 
            StartCoroutine(DamageFlashRoutine());
        }

        SpawnHitEffect(transform.position, hitNormal);
    }

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

        GameObject effectObj = Instantiate(bulletHoleEffectPrefab, point, Quaternion.identity);
        BulletHoleEffects effect = effectObj.GetComponent<BulletHoleEffects>();

        if (effect != null)
        {
            effect.PlayExplodeEffect(point, normal, transform);
            Destroy(effectObj, 0.1f);
        }
        else
        {
            Destroy(effectObj);
        }
    }

    private IEnumerator DamageFlashRoutine()
    {
        _isDamageFlashing = true; // 上鎖，停止 Update 中的殘血呼吸燈
        float timer = 0;
        
        while (timer < totalInvincibilityTime)
        {
            Color c = damageColor;
            c.a = (Mathf.FloorToInt(timer / flashDuration) % 2 == 0) ? 0.4f : 1f;
            sr.color = c;

            yield return null;
            timer += Time.deltaTime;
        }

        // 解鎖，把顏色控制權還給 Update() 去決定應該是白色還是殘血紅色
        _isDamageFlashing = false; 
    }
}