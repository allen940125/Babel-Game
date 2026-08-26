using Datamanager;
using System;
using UniRx;
using UnityEngine;

[System.Serializable]
public class MainGameMediator
{
    private CompositeDisposable disposable_ = new CompositeDisposable();

    [field: SerializeField] public RealTimePlayerData RealTimePlayerData { get; private set; } = new RealTimePlayerData();

    // ==========================================
    // ★ 加上 [field: SerializeField] 強制暴露給 Inspector 監視！
    // ==========================================
    [field: SerializeField] 
    public EntityRuntime CurrentPlayerRuntime { get; private set; }
    
    [field: SerializeField] 
    public EntityRuntime CurrentBossRuntime { get; private set; }

    public void MainGameMediatorInit()
    {
        RealTimePlayerData = GameContainer.Get<DataManager>().realTimePlayerData;
    }

    public void RegisterCurrentPlayer(EntityRuntime playerRuntime) { CurrentPlayerRuntime = playerRuntime; }
    public void UnregisterCurrentPlayer() { CurrentPlayerRuntime = null; }

    public void RegisterCurrentBoss(EntityRuntime bossRuntime) 
    { 
        CurrentBossRuntime = bossRuntime; 
        Debug.Log("<color=red>[系統] Boss 實體已向中繼站報到！</color>");
    }
    public void UnregisterCurrentBoss() { CurrentBossRuntime = null; }

    public void DisposeObserber() { disposable_.Dispose(); }
    public void AddToDisposables(IDisposable disposable) { disposable_.Add(disposable); }
}