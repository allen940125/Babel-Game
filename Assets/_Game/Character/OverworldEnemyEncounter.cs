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
        _core = GetComponent<EntityCore>(); // 取得這隻明雷怪的大腦
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasTriggered || _core == null) return;

        if (other.GetComponent<PlayerAdventureController>() != null)
        {
            _hasTriggered = true;
            
            GameManager.Instance.MainGameEvent.Send(new StartBattleEvent()
            {
                EnemyData = _core.RuntimeData, // ★ 傳遞大腦裡的活體資料！
                BattleScene = battleScene
            });
        }
    }
}