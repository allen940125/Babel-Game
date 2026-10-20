using UnityEngine;

public class BossAction_SpawnPrefab : BossPhaseAction
{
    [Header("生成設定")]
    [Tooltip("要生成的物件 (護盾、衝擊波、特效等)")]
    public GameObject prefabToSpawn;
    
    [Tooltip("指定的生成位置 (若不填，預設為 Boss 本體位置)")]
    public Transform spawnPoint;

    [Header("生命週期設定")]
    [Tooltip("勾選後，當 Boss 離開此階段時，會自動銷毀該物件")]
    public bool destroyOnPhaseExit = true;

    private GameObject _spawnedInstance;

    protected override void ExecuteAction()
    {
        if (prefabToSpawn == null) return;

        // 如果場上已經有舊的，先清掉避免重複堆疊
        if (_spawnedInstance != null) Destroy(_spawnedInstance);

        // 決定生成位置：有指定就用指定的，沒指定就用掛載此腳本的物件位置
        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        // 生成新物件
        _spawnedInstance = Instantiate(prefabToSpawn, pos, rot);
        
        // 註冊到大腦，讓 Boss 死亡或大招結束時統一清場 (維持你原有的架構)
        stateMachine.RegisterMapMechanism(_spawnedInstance);
        
        Debug.Log($"<color=cyan>[Boss技能] 階段 {targetPhase} 生成了物件：{prefabToSpawn.name}</color>");
    }

    protected override void StopAction()
    {
        // 根據開關決定是否要在離開階段時銷毀
        if (destroyOnPhaseExit && _spawnedInstance != null)
        {
            Destroy(_spawnedInstance);
        }
    }
}