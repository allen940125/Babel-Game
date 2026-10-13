using UnityEngine;
using DG.Tweening;

public class HoverFloat : MonoBehaviour
{
    [Header("視覺目標 (根物件的子物件，不要放 Collider)")]
    [SerializeField] private Transform visual;

    [Header("懸停上浮")]
    [SerializeField] private float floatHeight = 0.15f;
    [SerializeField] private float enterDuration = 0.2f;
    [SerializeField] private float exitDuration = 0.25f;
    [SerializeField] private Ease enterEase = Ease.OutQuad;
    [SerializeField] private Ease exitEase = Ease.InOutQuad;

    [Header("左鍵粒子特效")]
    [SerializeField] private ParticleSystem clickParticle;

    private Vector3 _baseLocalPosition;
    private float _currentOffset;
    private Tween _tween;

    private void Awake()
    {
        _baseLocalPosition = visual.localPosition;
    }

    private void OnMouseEnter() => TweenOffset(floatHeight, enterDuration, enterEase);

    private void OnMouseExit() => TweenOffset(0f, exitDuration, exitEase);

    private void OnMouseDown()
    {
        clickParticle.transform.position = visual.position;
        clickParticle.Play();
    }

    private void OnDisable()
    {
        _tween?.Kill();
        _currentOffset = 0f;
        visual.localPosition = _baseLocalPosition;
    }

    // 在 LateUpdate 套用，確保疊加在物理/拖曳移動之後，且始終沿世界 Y 軸上浮
    private void LateUpdate()
    {
        Vector3 worldUp = visual.parent.InverseTransformVector(Vector3.up * _currentOffset);
        visual.localPosition = _baseLocalPosition + worldUp;
    }

    private void TweenOffset(float target, float duration, Ease ease)
    {
        _tween?.Kill();
        _tween = DOTween.To(() => _currentOffset, v => _currentOffset = v, target, duration)
                        .SetEase(ease)
                        .SetLink(gameObject);
    }
}