using UnityEngine;
using Gamemanager;
using Game.SceneManagement;
using UnityEngine.Serialization;

[RequireComponent(typeof(Collider), typeof(EntityCore))]
public class OverworldEnemyEncounter : MonoBehaviour
{
    [SerializeField] private SceneType battleScene = SceneType.GameScene; 
    
    private bool _hasTriggered = false;
    private EntityCore _core;

    private void Awake() 
    {
        GetComponent<Collider>().isTrigger = true; 
        _core = GetComponent<EntityCore>(); 
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. 先確認物理引擎有沒有運作
        Debug.Log($"[明雷怪] 發生碰撞！撞到我的物件是: {other.gameObject.name} (Tag: {other.tag})");

        // 2. 檢查是否已經觸發過
        if (_hasTriggered)
        {
            Debug.Log("[明雷怪] 已經觸發過戰鬥，忽略此次碰撞。");
            return;
        }

        // 3. 檢查大腦是否遺失
        if (_core == null)
        {
            Debug.LogError("[明雷怪] 致命錯誤：找不到 EntityCore！");
            return;
        }

        Debug.Log("<color=green>[明雷怪] 確認是玩家！準備發送 StartBattleEvent！</color>");
        _hasTriggered = true;
            
        GameManager.Instance.MainGameEvent.Send(new StartBattleEvent()
        {
            EnemyData = _core.RuntimeData, 
            BattleScene = battleScene
        });
            
        Debug.Log("<color=green>[明雷怪] 廣播發送完畢！</color>");
        
        // // 4. 檢查是不是玩家
        // var playerCtrl = other.GetComponent<PlayerAdventureController>();
        // if (playerCtrl != null)
        // {
        //     Debug.Log("<color=green>[明雷怪] 確認是玩家！準備發送 StartBattleEvent！</color>");
        //     _hasTriggered = true;
        //     
        //     GameManager.Instance.MainGameEvent.Send(new StartBattleEvent()
        //     {
        //         EnemyData = _core.RuntimeData, 
        //         BattleScene = battleScene
        //     });
        //     
        //     Debug.Log("<color=green>[明雷怪] 廣播發送完畢！</color>");
        // }
        // else
        // {
        //     Debug.Log($"[明雷怪] 攔截：撞到的 {other.gameObject.name} 身上沒有 PlayerAdventureController 腳本。");
        // }
    }
}