using System;
using UnityEngine;

namespace GameFramework.Actors
{
    [Serializable]
    public class PlayerFallData
    {
        [field: Tooltip("Having higher numbers might not read collisions with shallow colliders correctly.")]
        [field: SerializeField] public bool canMovementInTheAir;
        [field: SerializeField] [field: Range(0f, 2f)] public float SpeedModifier { get; private set; } = 1f;
        [field: SerializeField] [field: Range(0f, 10f)] public float FallSpeedLimit { get; private set; } = 10f;
        [field: SerializeField] [field: Range(0f, 100f)] public float MinimumDistanceToBeConsideredHardFall { get; private set; } = 3f;
    }
}