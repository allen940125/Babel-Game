using UnityEngine;

[System.Serializable]
public class RealTimePlayerData
{
    // ★ 這裡只留下「跨場景、與戰鬥狀態無關」的進度資料
    public int NowLevel = 0;
    
    // 例如未來可能擴充：
    // public int TotalGold = 0;
    // public List<string> UnlockedWeapons; 

    // (以前的 PlayerDrunkennessValue, PlayerCurWineBottle 等全部刪除，交給 EntityRuntime 裡的 Trait 管！)
}