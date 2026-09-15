using UnityEngine;
using DG.Tweening;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    public int vibrato = 20;
    public float randomness = 90f;

    void Awake() => Instance = this;

    public static void Shake(float intensity, float duration)
    {
        Instance.transform.DOShakePosition(duration, new Vector3(intensity, intensity, 0f), Instance.vibrato, Instance.randomness);
    }
}