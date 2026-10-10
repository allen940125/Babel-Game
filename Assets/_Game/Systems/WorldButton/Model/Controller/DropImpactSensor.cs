using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(DraggableBehavior3D))]
// 拔除 [RequireComponent(typeof(BoxCollider))] 
public class DropImpactSensor : MonoBehaviour
{
    [Header("效能與目標設定")]
    public List<string> validTags = new List<string>() { "EnemyBullet" };

    [Header("打擊判定框")]
    [Tooltip("請放入專門用來做砸擊判定的 BoxCollider (建議設為 Is Trigger)")]
    [SerializeField] private BoxCollider smashHitbox;

    private void OnEnable() => GetComponent<DraggableBehavior3D>().OnDropped += HandleDrop;
    private void OnDisable() => GetComponent<DraggableBehavior3D>().OnDropped -= HandleDrop;

    private void HandleDrop()
    {
        if (smashHitbox == null)
        {
            Debug.LogError($"[錯誤] {gameObject.name} 的 DropImpactSensor 未綁定 smashHitbox！");
            return;
        }

        // 完美對齊你自訂的 Hitbox 中心點與尺寸 (支援縮放與旋轉)
        Vector3 center = smashHitbox.transform.TransformPoint(smashHitbox.center);
        Vector3 halfExtents = Vector3.Scale(smashHitbox.size, smashHitbox.transform.lossyScale) * 0.5f;

        Collider[] hits = Physics.OverlapBox(center, halfExtents, smashHitbox.transform.rotation);

        foreach (var hit in hits)
        {
            if (hit.gameObject == this.gameObject || hit.transform.IsChildOf(this.transform)) continue;

            if (validTags.Count > 0 && !validTags.Contains(hit.tag)) continue;

            if (hit.TryGetComponent(out SmashableTarget target))
            {
                target.TriggerSmash();
                Debug.Log($"<color=cyan>[砸擊成功] 成功砸中目標: {hit.gameObject.name}</color>");
            }
        }
    }
}