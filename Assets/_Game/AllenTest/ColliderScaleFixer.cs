using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ColliderScaleFixer : MonoBehaviour
{
    private struct CapsuleData
    {
        public CapsuleCollider Collider;
        public float Radius;
        public float Height;
        public Vector3 Center;
    }

    private struct BoxData
    {
        public BoxCollider Collider;
        public Vector3 Size;
        public Vector3 Center;
    }

    private struct SphereData
    {
        public SphereCollider Collider;
        public float Radius;
        public Vector3 Center;
    }

    private struct TransformSnapshot
    {
        public Transform Target;
        public Vector3 LocalScale;
    }

    [Header("鎖定選項")]
    [Tooltip("是否在每幀 LateUpdate 強制壓制動畫機或外部腳本對 Scale 的篡改")]
    [SerializeField] private bool lockEveryFrame = false;

    private readonly List<CapsuleData> _capsules = new List<CapsuleData>();
    private readonly List<BoxData> _boxes = new List<BoxData>();
    private readonly List<SphereData> _spheres = new List<SphereData>();
    private readonly List<TransformSnapshot> _transforms = new List<TransformSnapshot>();

    private void Awake()
    {
        RecordAllStates();
        LogScaleDiagnostics();
        ApplyReset();
    }

    private void LateUpdate()
    {
        if (lockEveryFrame)
        {
            ApplyReset();
        }
    }

    [ContextMenu("Record States (Manual)")]
    public void RecordAllStates()
    {
        _capsules.Clear();
        _boxes.Clear();
        _spheres.Clear();
        _transforms.Clear();

        // 記錄 Transform 層級
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            _transforms.Add(new TransformSnapshot
            {
                Target = t,
                LocalScale = t.localScale
            });
        }

        // 記錄 CapsuleCollider 原始幾何參數
        foreach (var col in GetComponentsInChildren<CapsuleCollider>(true))
        {
            _capsules.Add(new CapsuleData
            {
                Collider = col,
                Radius = col.radius,
                Height = col.height,
                Center = col.center
            });
        }

        // 記錄 BoxCollider 原始幾何參數
        foreach (var col in GetComponentsInChildren<BoxCollider>(true))
        {
            _boxes.Add(new BoxData
            {
                Collider = col,
                Size = col.size,
                Center = col.center
            });
        }

        // 記錄 SphereCollider 原始幾何參數
        foreach (var col in GetComponentsInChildren<SphereCollider>(true))
        {
            _spheres.Add(new SphereData
            {
                Collider = col,
                Radius = col.radius,
                Center = col.center
            });
        }
    }

    [ContextMenu("Apply Reset")]
    public void ApplyReset()
    {
        // 1. 強制重置所有 Transform Scale
        for (int i = 0; i < _transforms.Count; i++)
        {
            if (_transforms[i].Target != null)
            {
                _transforms[i].Target.localScale = _transforms[i].LocalScale;
            }
        }

        // 2. 強制重置 Collider 內部參數
        for (int i = 0; i < _capsules.Count; i++)
        {
            var data = _capsules[i];
            if (data.Collider != null)
            {
                data.Collider.radius = data.Radius;
                data.Collider.height = data.Height;
                data.Collider.center = data.Center;
            }
        }

        for (int i = 0; i < _boxes.Count; i++)
        {
            var data = _boxes[i];
            if (data.Collider != null)
            {
                data.Collider.size = data.Size;
                data.Collider.center = data.Center;
            }
        }

        for (int i = 0; i < _spheres.Count; i++)
        {
            var data = _spheres[i];
            if (data.Collider != null)
            {
                data.Collider.radius = data.Radius;
                data.Collider.center = data.Center;
            }
        }
    }

    private void LogScaleDiagnostics()
    {
        foreach (var col in GetComponentsInChildren<Collider>(true))
        {
            Vector3 lossy = col.transform.lossyScale;
            // 檢查全域縮放各軸是否不均勻 (容差 0.001)
            bool isNonUniform = Mathf.Abs(lossy.x - lossy.y) > 0.001f ||
                                Mathf.Abs(lossy.y - lossy.z) > 0.001f ||
                                Mathf.Abs(lossy.x - lossy.z) > 0.001f;

            if (isNonUniform)
            {
                Debug.LogWarning(
                    $"[ColliderScaleFixer] 偵測到碰撞體異常全域縮放！\n" +
                    $"物件名稱: {col.gameObject.name}\n" +
                    $"碰撞體類型: {col.GetType().Name}\n" +
                    $"LocalScale: {col.transform.localScale}\n" +
                    $"LossyScale (實際世界縮放): {lossy}\n" +
                    $"-> 這正是 Capsule/Sphere 會被迫以最大軸膨脹的原因。",
                    col.gameObject
                );
            }
        }
    }
}