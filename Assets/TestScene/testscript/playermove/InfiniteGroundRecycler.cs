using UnityEngine;

/// <summary>
/// 無限延伸的地面：用 N 塊長度相同的地面（可以帶路燈、牆、樹等子物件），
/// 玩家往前跑時，把已經跑到身後的那一塊搬到最前面接上，看起來就是無限長的路。
///
/// 使用方式：
/// 1. 做一塊地面 prefab（長度 = tileLength，沿著跑步方向），pivot 放在中心。
/// 2. 沿著跑步方向連續擺 3~4 塊，每塊相隔剛好 tileLength（第一塊放在玩家附近）。
/// 3. 把這幾塊拖進 Tiles，Target 拖主角，Direction 填跟 RunnerMover 一樣的方向。
/// </summary>
public class InfiniteGroundRecycler : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Transform[] tiles;

    [Tooltip("要跟 RunnerMover 的 Direction 一致")]
    [SerializeField] private Vector3 direction = Vector3.right;

    [Tooltip("每一塊地面沿著跑步方向的長度")]
    [SerializeField] private float tileLength = 20f;

    [Tooltip("地面中心落後玩家幾個 tileLength 以上才搬到前面（1.5 = 完全跑過去了才回收）")]
    [SerializeField] private float recycleBehindTiles = 1.5f;

    private void LateUpdate()
    {
        if (target == null || tiles == null || tiles.Length == 0) return;

        Vector3 dir = direction.normalized;
        float playerPos = Vector3.Dot(target.position, dir);

        foreach (var tile in tiles)
        {
            if (tile == null) continue;

            float tilePos = Vector3.Dot(tile.position, dir);
            if (playerPos - tilePos > tileLength * recycleBehindTiles)
            {
                // 搬到整排的最前面（一次跳過全部 tile 的總長度）
                tile.position += dir * (tileLength * tiles.Length);
            }
        }
    }
}
