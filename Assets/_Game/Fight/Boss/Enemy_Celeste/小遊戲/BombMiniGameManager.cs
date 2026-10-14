using UnityEngine;

public class BombMiniGameManager : MonoBehaviour
{
    [Header("炸彈設定")]
    public FollowBomb bombPrefab;
    public Transform bombSpawnPoint; 

    private FollowBomb _spawnedBomb;

    private void Start()
    {
        if (bombPrefab != null && bombSpawnPoint != null)
        {
            _spawnedBomb = Instantiate(bombPrefab, bombSpawnPoint.position, Quaternion.identity);
        }
    }

    public void ReportTripwireBreached()
    {
        if (_spawnedBomb != null)
        {
            _spawnedBomb.Detonate();
        }
        Destroy(gameObject); 
    }

    // ★ 核心防漏：當這個總管因為任何原因被銷毀 (例如 Boss 死亡、階段切換)
    // 強制把孤兒炸彈一起清除，防止記憶體洩漏與邏輯錯誤
    private void OnDestroy()
    {
        if (_spawnedBomb != null)
        {
            Destroy(_spawnedBomb.gameObject);
        }
    }
}