using UnityEngine;
using DG.Tweening;
using System.Reflection;

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

    // 反射所需的快取變數
    private BaseButtonController3D _buttonController;
    private FieldInfo _isPhaseLockedField;
    private bool _wasLocked = false;

    private void Awake()
    {
        _baseLocalPosition = visual.localPosition;

        _buttonController = GetComponent<BaseButtonController3D>();
        if (_buttonController != null)
        {
            _isPhaseLockedField = typeof(BaseButtonController3D).GetField("_isPhaseLocked", BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }

    private void Update()
    {
        if (IsButtonLocked())
        {
            if (!_wasLocked)
            {
                _wasLocked = true;
                ForceReset();
            }
        }
        else
        {
            _wasLocked = false;
        }
    }

    private bool IsButtonLocked()
    {
        if (_buttonController != null && _isPhaseLockedField != null)
        {
            return (bool)_isPhaseLockedField.GetValue(_buttonController);
        }
        return false;
    }

    private void OnMouseEnter()
    {
        if (IsButtonLocked()) return;
        TweenOffset(floatHeight, enterDuration, enterEase);
    }

    private void OnMouseExit()
    {
        if (IsButtonLocked()) return;
        TweenOffset(0f, exitDuration, exitEase);
    }

    private void OnMouseDown()
    {
        if (IsButtonLocked()) return;
        // ★ 安全檢查：只有在 clickParticle 有被賦值的情況下才執行特效
        if (clickParticle != null) 
        {
            clickParticle.transform.position = visual.position;
            clickParticle.Play();
        }
        else
        {
            // 如果你希望在 Console 看到提示，可以把這行打開
            // Debug.LogWarning($"[HoverFloat] {gameObject.name} 的 clickParticle 沒有在 Inspector 中設定！");
        }
    }

    private void OnDisable()
    {
        ForceReset();
    }

    private void ForceReset()
    {
        _tween?.Kill();
        _currentOffset = 0f;
        if (visual != null)
        {
            visual.localPosition = _baseLocalPosition;
        }
    }

    private void LateUpdate()
    {
        if (visual != null && visual.parent != null)
        {
            Vector3 worldUp = visual.parent.InverseTransformVector(Vector3.up * _currentOffset);
            visual.localPosition = _baseLocalPosition + worldUp;
        }
    }

    private void TweenOffset(float target, float duration, Ease ease)
    {
        _tween?.Kill();
        _tween = DOTween.To(() => _currentOffset, v => _currentOffset = v, target, duration)
                        .SetEase(ease)
                        .SetLink(gameObject);
    }
}