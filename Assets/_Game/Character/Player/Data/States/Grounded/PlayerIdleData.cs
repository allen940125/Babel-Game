using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Actors
{
    [Serializable]
    public class PlayerIdleData : IMovementStateData
    {
        [field: SerializeField] [field: Range(0f, 1f)] public float SpeedModifier { get; private set; } = 0f;
       
        [field: SerializeField] [field: Range(0f, 1f)]  public float Acceleration { get; private set; }

        [field: SerializeField] [field: Range(0f, 1f)]  public float Deceleration { get; private set; }
    }
}