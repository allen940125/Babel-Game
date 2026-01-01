using System;
using UnityEngine;

namespace GameFramework.Actors
{
    [Serializable]
    public class PlayerRunData : IMovementStateData
    {
        [field: SerializeField] [field: Range(1f, 2f)] public float SpeedModifier { get; private set; } = 1f;
       
        [field: SerializeField] [field: Range(0f, 1f)]  public float Acceleration { get; private set; }

        [field: SerializeField] [field: Range(0f, 1f)]  public float Deceleration { get; private set; }
    }
}