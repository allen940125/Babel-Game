using UnityEngine;

public class PhaseTransitionController : MonoBehaviour
{
    [SerializeField] private GameObject phase2Prefab;
    [SerializeField] private EntityHealthComponent healthComp;
    [SerializeField] private Rigidbody rb; // 若需要保留動能

    private void OnEnable()
    {
        healthComp = GetComponent<EntityHealthComponent>();
        rb = GetComponent<Rigidbody>();
        // 假設你的 Health Component 有死亡或扣血事件
        healthComp.OnDeath += TriggerTransition; 
    }

    private void TriggerTransition()
    {
        // 1. 生成階段二物件，並完全複製當前的座標與旋轉
        GameObject phase2Instance = Instantiate(phase2Prefab, transform.position, transform.rotation);

        // 2. 物理動能交接 (如果你的按鈕在移動中被打爆，切換後不能在空中瞬間停住)
        if (rb != null && phase2Instance.TryGetComponent<Rigidbody>(out var phase2Rb))
        {
            phase2Rb.linearVelocity = rb.linearVelocity;
            phase2Rb.angularVelocity = rb.angularVelocity;
        }

        // 3. 銷毀第一階段 (自己)
        Destroy(gameObject);
    }
}