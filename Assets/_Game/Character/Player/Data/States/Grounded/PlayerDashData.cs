using System;
using UnityEngine;

namespace GameFramework.Actors
{
    [Serializable]
    public class PlayerDashData
    {
        [field: SerializeField] [field: Range(1f, 3f)] public float SpeedModifier { get; private set; } = 2f;
        [field: SerializeField] public PlayerRotationData RotationData { get; private set; }

        [field: Header("連續衝刺判定時間")]
        [field: SerializeField] [field: Range(0f, 2f)] public float TimeToBeConsideredConsecutive { get; private set; } = 1f;

        [field: Header("最大連續衝刺次數限制")]
        [field: SerializeField] [field: Range(1, 10)] public int ConsecutiveDashesLimitAmount { get; private set; } = 2;

        [field: Header("連續衝刺冷卻時間")]
        [field: SerializeField] [field: Range(0f, 5f)] public float DashLimitReachedCooldown { get; private set; } = 1.75f;
    }
}