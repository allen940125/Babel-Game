using UnityEngine;
using Gamemanager;
using Game.SceneManagement;
using UnityEngine.Serialization;

[RequireComponent(typeof(Collider))]
public class OverworldEnemyEncounter : MonoBehaviour
{
    [FormerlySerializedAs("enemySO")] [SerializeField] private EntityRuntime enemy;
    [SerializeField] private SceneType battleScene = SceneType.GameScene; // 直接選擇 Enum
    
    private bool _hasTriggered = false;

    private void Awake() => GetComponent<Collider>().isTrigger = true; 

    private void OnTriggerEnter(Collider other)
    {
        if (_hasTriggered) return;

        if (other.GetComponent<PlayerAdventureController>() != null)
        {
            _hasTriggered = true;
            
            // 將事件丟向天空
            GameManager.Instance.MainGameEvent.Send(new StartBattleEvent()
            {
                EnemyData = enemy,
                BattleScene = battleScene
            });
        }
    }
}