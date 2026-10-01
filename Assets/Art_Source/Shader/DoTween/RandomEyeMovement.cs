using UnityEngine;
using DG.Tweening;

public class RandomEyeMovement : MonoBehaviour
{
    [Header("轉動範圍設定")]
    [SerializeField] private float maxAngleX = 20f;
    [SerializeField] private float maxAngleY = 25f;

    [Header("時間設定")]
    [SerializeField] private float minInterval = 1f;
    [SerializeField] private float maxInterval = 3f;
    [SerializeField] private float moveDuration = 0.5f;

    private Quaternion initialRotation;

    private void Start()
    {
        initialRotation = transform.localRotation;
        ScheduleNextMove();
    }

    private void ScheduleNextMove()
    {
        float delay = Random.Range(minInterval, maxInterval);

        DOVirtual.DelayedCall(delay, () =>
        {
            float x = Random.Range(-maxAngleX, maxAngleX);
            float y = Random.Range(-maxAngleY, maxAngleY);
            Quaternion target = initialRotation * Quaternion.Euler(x, y, 0f);

            transform.DOLocalRotateQuaternion(target, moveDuration)
                .SetEase(Ease.InOutSine)
                .OnComplete(ScheduleNextMove);
        });
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}