using UnityEngine;

public class EnemyAttackObject : MonoBehaviour
{
    [Header("基礎傷害設定")]
    public int damageAmount = 15; // 建議配合百分比血量 (如 100 滿血)，把數值稍微調大

    // ★ 升級為標準介面交易：發射 DamagePayload 封包，不再依賴具體的 PlayerController3D！
    protected void TryDealDamage(GameObject hitObject)
    {
        if (hitObject == null) return;

        // 1. 嘗試直接在碰撞體、或是其父物件上尋找受擊介面 IDamageable
        IDamageable damageable = hitObject.GetComponent<IDamageable>();
        if (damageable == null) damageable = hitObject.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            // 2. 打包標準傷害封包 (這裡如果敵人也有自己的 SO，可在此加入暴擊與加成)
            DamagePayload payload = new DamagePayload()
            {
                Damage = this.damageAmount,
                IsCrit = false, // 敵方普通彈幕預設無暴擊，或是可以加上亂數判定
                Source = this.gameObject
            };

            // 3. 點對點直接交割！
            damageable.TakeDamage(payload);
            Debug.Log($"<color=orange>[敵方命中] {gameObject.name} 對 {hitObject.name} 造成 {damageAmount} 點傷害！</color>");
        }
    }
}