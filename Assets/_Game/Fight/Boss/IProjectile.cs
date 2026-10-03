using UnityEngine;

public interface IProjectile
{
    // 專司處理飛行軌跡
    void SetTrajectory(Vector3 direction, float speed);
}