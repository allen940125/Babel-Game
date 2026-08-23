using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace GameFramework.Actors
{
    public class CameraZoom : MonoBehaviour
    {
        [Header("Distance Settings")]
        [SerializeField] private float defaultDistance = 6f;
        [SerializeField] private float minimumDistance = 1f;
        [SerializeField] private float maximumDistance = 12f;

        [Header("Zoom Settings")]
        [SerializeField] private float smoothing = 4f;
        
        // 1. 修改：拿掉 Range 限制，因為 Input System 的滾輪數值可能是 0.1 也可能是 120
        [Header("Sensitivity")]
        [Tooltip("如果滾輪太慢，請把這個數字改大 (試試看 1, 10, 甚至 100)")]
        [SerializeField] private float scrollSensitivity = 1f; 
        
        [Tooltip("手機觸控縮放靈敏度")]
        [SerializeField] private float touchSensitivity = 0.01f; 

        // 2. 新增：反轉滾輪方向開關
        [Header("Settings")]
        [SerializeField] private bool invertScroll = false;

        [Header("Input (PC Only)")]
        [SerializeField] private InputActionReference zoomInputAction;

        private CinemachinePositionComposer cinemachinePositionComposer;
        private float currentTargetDistance;

        private void Awake()
        {
            cinemachinePositionComposer = GetComponent<CinemachinePositionComposer>();
            
            if (cinemachinePositionComposer != null)
            {
                currentTargetDistance = cinemachinePositionComposer.CameraDistance;
            }
            else
            {
                Debug.LogError("找不到 CinemachinePositionComposer！請檢查組件。");
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (zoomInputAction != null && zoomInputAction.action != null)
                zoomInputAction.action.Enable();
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            if (zoomInputAction != null && zoomInputAction.action != null)
                zoomInputAction.action.Disable();
            EnhancedTouchSupport.Disable();
        }

        private void Update()
        {
            HandleInput();
            ApplyZoom();
        }

        private void HandleInput()
        {
            float zoomDelta = 0f;

            // --- 手機雙指 ---
            if (Touch.activeTouches.Count >= 2)
            {
                var touch0 = Touch.activeTouches[0];
                var touch1 = Touch.activeTouches[1];

                Vector2 touch0PrevPos = touch0.screenPosition - touch0.delta;
                Vector2 touch1PrevPos = touch1.screenPosition - touch1.delta;

                float prevMagnitude = (touch0PrevPos - touch1PrevPos).magnitude;
                float currentMagnitude = (touch0.screenPosition - touch1.screenPosition).magnitude;

                float difference = currentMagnitude - prevMagnitude;
                
                // 手機通常不需要反轉，但如果你覺得怪也可以乘上 -1
                zoomDelta = difference * touchSensitivity;
            }
            // --- PC 滾輪 ---
            else if (zoomInputAction != null && zoomInputAction.action != null)
            {
                // 讀取滾輪值
                float scrollValue = zoomInputAction.action.ReadValue<float>();
                
                // 3. 修改：加入反轉邏輯
                if (Mathf.Abs(scrollValue) > 0)
                {
                    // 根據 invertScroll 決定正負號
                    float direction = invertScroll ? -1f : 1f;
                    
                    // 計算縮放量
                    zoomDelta = scrollValue * scrollSensitivity * direction;
                }
            }

            // --- 應用縮放 (拉近/拉遠) ---
            // 邏輯：zoomDelta > 0 代表要「拉近」(距離變小)
            if (Mathf.Abs(zoomDelta) > 0)
            {
                currentTargetDistance -= zoomDelta; 
                currentTargetDistance = Mathf.Clamp(currentTargetDistance, minimumDistance, maximumDistance);
            }
        }

        private void ApplyZoom()
        {
            if (cinemachinePositionComposer == null) return;

            float currentDistance = cinemachinePositionComposer.CameraDistance;

            if (Mathf.Abs(currentDistance - currentTargetDistance) > 0.001f)
            {
                float lerpedZoomValue = Mathf.Lerp(currentDistance, currentTargetDistance, smoothing * Time.deltaTime);
                cinemachinePositionComposer.CameraDistance = lerpedZoomValue;
            }
        }
    }
}