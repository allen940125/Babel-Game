using System;
using UnityEngine;

namespace GameFramework.Actors
{
    [Serializable]
    public class PlayerSprintData : IMovementStateData
    {
        [field: SerializeField] [field: Range(1f, 3f)] public float SpeedModifier { get; private set; } = 1.7f;
       
        [field: SerializeField] [field: Range(0f, 1f)]  public float Acceleration { get; private set; }

        [field: SerializeField] [field: Range(0f, 1f)]  public float Deceleration { get; private set; }
    }
}