public interface IMovementStateData
{
    float SpeedModifier { get; }
    float Acceleration { get; }  // 可選
    float Deceleration { get; }  // 可選
}