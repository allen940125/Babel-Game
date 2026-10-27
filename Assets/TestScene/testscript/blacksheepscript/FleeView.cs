using UnityEngine;

/// <summary>逃跑場景用的小工具：畫面比例座標 ↔ 世界座標。</summary>
public static class FleeView
{
    /// <summary>把畫面比例座標（0~1，可超出）轉成 Z = 0 平面上的世界座標。正交與透視相機都適用。</summary>
    public static Vector3 ViewportToPlane(Camera cam, float vx, float vy)
    {
        if (cam == null) return Vector3.zero;

        Ray ray = cam.ViewportPointToRay(new Vector3(vx, vy, 0f));
        var plane = new Plane(Vector3.forward, Vector3.zero);
        return plane.Raycast(ray, out float d) ? ray.GetPoint(d) : ray.origin;
    }
}
