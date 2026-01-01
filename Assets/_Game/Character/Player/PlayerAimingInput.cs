using UnityEngine;
using UnityEngine.InputSystem;

namespace GameFramework.Actors
{
    public class PlayerAimingInput : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private LayerMask _groundLayer;

        [Header("Visual Feedback (LoL Style)")]
        [SerializeField] private Transform _aimingReticle; // 拖入那個壓扁的圓柱體
        [SerializeField] private float _reticleYOffset = 0.1f; // 稍微浮起一點點，避免與地板穿插 (Z-Fighting)

        [Header("Debug")]
        [SerializeField] private bool _showDebugGizmos = true;
        private Vector3 _currentHitPoint; // 存起來給 Gizmos 畫圖用

        private void Awake()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
        }

        public void HandleAimingInput(PlayerStateReusableData data, Transform playerTransform)
        {
            if (Mouse.current == null || _mainCamera == null) return;

            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Ray ray = _mainCamera.ScreenPointToRay(mouseScreenPosition);
            
            bool hasHit = false;
            Vector3 targetPosition = Vector3.zero;

            // 1. 射線檢測
            if (Physics.Raycast(ray, out RaycastHit hitInfo, 1000f, _groundLayer))
            {
                targetPosition = hitInfo.point;
                hasHit = true;
            }
            else
            {
                // 2. 數學平面備案
                Plane virtualPlane = new Plane(Vector3.up, playerTransform.position);
                if (virtualPlane.Raycast(ray, out float enter))
                {
                    targetPosition = ray.GetPoint(enter);
                    hasHit = true;
                }
            }

            if (hasHit)
            {
                // 更新數據
                data.MouseWorldPosition = targetPosition;
                _currentHitPoint = targetPosition;

                // --- 視覺化：讓準心跟隨 ---
                if (_aimingReticle != null)
                {
                    // 讓準心移動到該位置，但稍微抬高一點點 (Y + 0.1) 避免跟地板重疊閃爍
                    _aimingReticle.position = targetPosition + Vector3.up * _reticleYOffset;
                    
                    // (進階) 如果你想讓準心貼合斜坡的角度，可以用這行：
                    // _aimingReticle.up = hitInfo.normal; // 這需要 Raycast 成功才有 normal
                }
            }
        }

        // 這是更清楚的 Debug 畫法，在 Scene 視窗會看到一顆球
        private void OnDrawGizmos()
        {
            if (!_showDebugGizmos) return;

            Gizmos.color = Color.cyan;
            // 畫一顆實心球，比 DrawLine 清楚多了
            Gizmos.DrawSphere(_currentHitPoint, 0.2f);
            
            // 也可以畫一條線連回主角
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _currentHitPoint);
        }
    }
}