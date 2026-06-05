using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Actors
{
    [CreateAssetMenu(menuName = "Combat/WeaponConfig")]
    public class WeaponConfig : ScriptableObject
    {
        public string WeaponName;
    
        // 這裡就是你的 Combo 列表！
        // List 的長度就是 MaxCombo，第 0 個就是第一下，第 1 個就是第二下
        public List<AttackActionData> ComboSteps; 
        
        // 取得當前段數的資料，如果超過長度就拿最後一個或重置 (看你邏輯)
        public AttackActionData GetStep(int index)
        {
            if (ComboSteps == null || ComboSteps.Count == 0) return null;
            if (index >= ComboSteps.Count) return ComboSteps[ComboSteps.Count - 1];
            return ComboSteps[index];
        }
    }
}
