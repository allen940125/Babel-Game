using System.Collections.Generic;
using UnityEngine;

public class GroundTrigger : MonoBehaviour
{
    [Header("地板的 Layer 設定")]
    [SerializeField] private LayerMask groundLayer;

    public bool IsGrounded => groundedObjects.Count > 0;

    private HashSet<GameObject> groundedObjects = new HashSet<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        if (IsInGroundLayer(other.gameObject))
        {
            groundedObjects.Add(other.gameObject);
            Debug.Log("玩家踩到地面，總數: " + groundedObjects.Count);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsInGroundLayer(other.gameObject))
        {
            groundedObjects.Remove(other.gameObject);
            Debug.Log("玩家離開地面，總數: " + groundedObjects.Count);
        }
    }

    private bool IsInGroundLayer(GameObject obj)
    {
        return (groundLayer.value & (1 << obj.layer)) != 0;
    }
}