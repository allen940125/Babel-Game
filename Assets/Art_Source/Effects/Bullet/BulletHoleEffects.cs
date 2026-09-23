using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class BulletHoleEffects : MonoBehaviour
{
    [Header("彈孔")]
    public GameObject[] bulletHolePrefabs;
    public int minHoleCount = 1;
    public int maxHoleCount = 3;
    public float holeSpreadRadius = 0.3f;
    public float holeScale = 2f;
    public float holePopDuration = 0.2f;
    public float holeLifetime = 5f;
    public float holeFadeDuration = 0.5f;

    [Header("震動")]
    public float shakeIntensity = 0.15f;
    public float shakeDuration = 0.15f;

    public void PlayExplodeEffect(Vector3 point, Vector3 normal, Transform hitTransform)
    {
        SpawnBulletHoles(point, normal, hitTransform);

        //螢幕震動備份
        //CameraShake.Shake(shakeIntensity, shakeDuration);
    }

    void SpawnBulletHoles(Vector3 hitPoint, Vector3 normal, Transform hitTransform)
    {
        int count = Random.Range(minHoleCount, maxHoleCount + 1);
        Vector3 tangent = Vector3.Cross(normal, Vector3.up).normalized;
        Vector3 bitangent = Vector3.Cross(normal, tangent);

        List<GameObject> pool = new List<GameObject>(bulletHolePrefabs);
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * holeSpreadRadius;
            Vector3 pos = hitPoint + tangent * offset.x + bitangent * offset.y + normal * 0.01f;
            Quaternion rot = Quaternion.LookRotation(-normal, tangent) * Quaternion.Euler(0, 0, Random.Range(0f, 360f));

            GameObject hole = Instantiate(pool[i % pool.Count], pos, rot, hitTransform);
            Vector3 targetScale = hole.transform.localScale * holeScale;
            hole.transform.localScale = Vector3.zero;
            hole.transform.DOScale(targetScale, holePopDuration).SetEase(Ease.OutBack);

            DOVirtual.DelayedCall(holeLifetime, () =>
                hole.GetComponentInChildren<Renderer>().material.DOFade(0f, holeFadeDuration).OnComplete(() => Destroy(hole)));
        }
    }
}