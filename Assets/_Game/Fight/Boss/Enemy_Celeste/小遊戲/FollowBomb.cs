using UnityEngine;

public class FollowBomb : MonoBehaviour
{
    [Header("追擊設定")]
    public string targetTag = "Player";
    public float followSpeed = 4.5f; 
    
    [Tooltip("相對於目標的偏移座標 (例如往上 2 單位，避免炸彈貼在地板)")]
    public Vector3 targetOffset = new Vector3(0, 2f, 0);

    [Header("引爆設定")]
    public GameObject explosionPrefab;  

    private Transform _target;

    private void Awake()
    {
        GameObject player = GameObject.FindGameObjectWithTag(targetTag);
        if (player != null) _target = player.transform;
    }

    private void Update()
    {
        if (_target != null)
        {
            // ★ 將目標位置加上你設定的 Offset
            Vector3 finalTargetPos = _target.position + targetOffset;
            
            transform.position = Vector3.MoveTowards(
                transform.position, 
                finalTargetPos, 
                followSpeed * Time.deltaTime
            );
        }
    }

    public void Detonate()
    {
        if (explosionPrefab != null) 
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}