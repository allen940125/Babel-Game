using UnityEngine;

namespace GameFramework.Actors
{ 
    [CreateAssetMenu(fileName = "PlayerSO", menuName = "Data/Characters/Player/PlayerSO")]
    public class PlayerSO : ScriptableObject
    {
        [field: Header("玩家地上狀態資料")]
        [field: SerializeField] public PlayerGroundedData GroundedData { get; private set; }
        
        [field: Header("玩家天空狀態資料")]
        [field: SerializeField] public PlayerAirborneData AirborneData { get; private set; }

    }

}