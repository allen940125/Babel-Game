using UnityEngine;

public class BossAction_SpawnShield : BossPhaseAction
{
    [Header("護盾設定")]
    public GameObject shieldPrefab;
    [Tooltip("指定護盾生成的絕對位置 (例如 Boss 身前、或場地中央)")]
    public Transform shieldSpawnPoint;

    private GameObject _activeShield;

    protected override void ExecuteAction()
    {
        if (shieldPrefab == null || shieldSpawnPoint == null) return;

        // 如果場上已經有舊的護盾，先清掉
        if (_activeShield != null) Destroy(_activeShield);

        // 生成新護盾
        _activeShield = Instantiate(shieldPrefab, shieldSpawnPoint.position, shieldSpawnPoint.rotation);
        
        // 註冊到大腦，讓 Boss 攻擊結束時統一清場 (可選)
        stateMachine.RegisterMapMechanism(_activeShield);
        
        Debug.Log($"<color=cyan>[Boss技能] 生成了防禦護盾！</color>");
    }

    protected override void StopAction()
    {
        // 如果你希望離開攻擊階段時護盾要消失，寫在這裡
        if (_activeShield != null)
        {
            Destroy(_activeShield);
        }
    }
}