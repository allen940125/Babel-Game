using GameFramework.Actors;
using Gamemanager;
using UnityEngine;

public class PlayerDeathHandler : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<EntityHealthComponent>().OnDeath += HandlePlayerDeath;
    }

    private void HandlePlayerDeath()
    {
        Debug.Log("Player is dead");
        // 專心處理玩家專屬的輸入剝奪與全域 UI 呼叫
        GetComponent<PlayerController3D>().enabled = false;
        GameManager.Instance.MainGameEvent.Send(new GameOverEvent());
        GameManager.Instance.GameOver();
    }
}