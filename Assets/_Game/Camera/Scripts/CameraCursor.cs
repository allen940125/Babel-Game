using Gamemanager;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch; // 引入增強觸控 API
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch; // 指定 Touch 類型

namespace GameFramework.Actors
{
    public class CameraCursor : MonoBehaviour
    {
        // ====================================================================
        // 1. 菜單/UI 切換輸入
        // ====================================================================
        [Header("游標/菜單 切換 輸入")]
        [SerializeField] private InputActionReference cameraToggleInputAction;
        [SerializeField] private bool startHidden = false; 

        // ====================================================================
        // 2. ARPG 相機自由調整 輸入 (PC: MMB)
        // ====================================================================
        [Header("ARPG 相機自由調整 輸入 (PC用)")]
        [SerializeField] private InputActionReference cameraAdjustAction;

        // ====================================================================
        // 3. Cinemachine 控制器配置
        // ====================================================================
        [Header("Cinemachine 控制器")]
        [SerializeField] private CinemachineInputAxisController cinemachineInputAxisController;
        [SerializeField] private bool disableCameraLookOnCursorVisible;
        [SerializeField] private bool disableCameraZoomOnCursorVisible;
        [Tooltip("Cinemachine 版本相容性設定")]
        [SerializeField] private bool fixedCinemachineVersion = true;

        [Header("Input References")] 
        public InputActionReference lookAction;

        [Header("Mobile Settings")]
        [Tooltip("手機上是否只允許在螢幕右側滑動來旋轉相機 (避免跟左側移動搖桿衝突)")]
        [SerializeField] private bool onlyRotateOnRightSide = true;
        [Tooltip("右側觸控區域的比例 (0.5 代表螢幕右半邊)")]
        [Range(0.1f, 0.9f)] 
        [SerializeField] private float rightSideRatio = 0.5f;

        // ====================================================================
        // 4. 狀態追蹤變數
        // ====================================================================
        private bool isPcCameraAdjusting = false; // PC: 追蹤 MMB 是否按住
        private bool isTouchCameraAdjusting = false; // Mobile: 追蹤是否有有效觸控
        private bool isMenuOpen = false;        

        private void Awake()
        {
            Debug.Log("Camera Cursor Awake");
            
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // 訂閱輸入事件
            cameraToggleInputAction.action.started += OnCameraCursorToggled;

            if (cameraAdjustAction != null)
            {
                cameraAdjustAction.action.started += OnCameraAdjustStarted;
                cameraAdjustAction.action.canceled += OnCameraAdjustCanceled;
            }

            GameManager.Instance.MainGameEvent.SetSubscribe(
                GameManager.Instance.MainGameEvent.OnCursorToggledEvent, 
                cmd => { ToggleCursor(cmd.ShowCursor ?? false); }
            );
        }

        private void OnEnable()
        {
            cameraToggleInputAction.asset.Enable();
            if (cameraAdjustAction != null) cameraAdjustAction.asset.Enable();
            
            // 啟用增強觸控支援 (重要：手機觸控需要這個)
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            cameraToggleInputAction.action.started -= OnCameraCursorToggled;
            if (cameraAdjustAction != null)
            {
                cameraAdjustAction.action.started -= OnCameraAdjustStarted;
                cameraAdjustAction.action.canceled -= OnCameraAdjustCanceled;
            }
            
            cameraToggleInputAction.asset.Disable();
            if (cameraAdjustAction != null) cameraAdjustAction.asset.Disable();
            
            EnhancedTouchSupport.Disable();
            
            GameManager.Instance.MainGameEvent.Unsubscribe<CursorToggledEvent>();
        }

        private void Update()
        {
            // 每一幀檢查手機觸控狀態
            CheckMobileTouchInput();
            
            // 更新控制器狀態
            UpdateControllerState();
        }

        // ====================================================================
        // 手機觸控邏輯 (新增)
        // ====================================================================
        private void CheckMobileTouchInput()
        {
            // 如果菜單開著，不處理觸控旋轉
            if (isMenuOpen) 
            {
                isTouchCameraAdjusting = false;
                return;
            }

            isTouchCameraAdjusting = false;

            // 遍歷所有觸控點
            foreach (var touch in Touch.activeTouches)
            {
                // 我們只關心剛按下或移動中的手指
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began || 
                    touch.phase == UnityEngine.InputSystem.TouchPhase.Moved || 
                    touch.phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                {
                    // 判斷觸控位置
                    if (onlyRotateOnRightSide)
                    {
                        // 如果觸控點在螢幕右側 (X > 螢幕寬度 * (1 - 比例))
                        float splitX = Screen.width * (1.0f - rightSideRatio);
                        if (touch.screenPosition.x > splitX)
                        {
                            isTouchCameraAdjusting = true;
                            break; // 只要有一根手指在右邊，就視為旋轉
                        }
                    }
                    else
                    {
                        // 全螢幕都可以旋轉 (不推薦，會跟移動搖桿打架)
                        isTouchCameraAdjusting = true;
                        break;
                    }
                }
            }
        }

        // ====================================================================
        // 核心邏輯 - 更新 Cinemachine 控制器啟用狀態
        // ====================================================================
        private void UpdateControllerState()
        {
            if (fixedCinemachineVersion) return; // 舊版相容模式不處理

            if (isMenuOpen)
            {
                cinemachineInputAxisController.enabled = false;
            }
            else
            {
                // 只要 (PC按住中鍵) 或者 (手機觸摸右螢幕) 其中一個成立，就啟用相機控制
                bool shouldEnable = isPcCameraAdjusting || isTouchCameraAdjusting;
                cinemachineInputAxisController.enabled = shouldEnable;
            }
        }

        // ====================================================================
        // PC 輸入回調
        // ====================================================================
        private void OnCameraAdjustStarted(InputAction.CallbackContext context)
        {
            if (!isMenuOpen) isPcCameraAdjusting = true;
        }

        private void OnCameraAdjustCanceled(InputAction.CallbackContext context)
        {
            isPcCameraAdjusting = false;
        }

        // ====================================================================
        // 菜單切換邏輯
        // ====================================================================
        private void OnCameraCursorToggled(InputAction.CallbackContext context)
        {
            ToggleCursor(!isMenuOpen);
        }

        private void ToggleCursor(bool? showMenu = null)
        {
            isMenuOpen = showMenu ?? !isMenuOpen;

            // 切換菜單時，強制重置操作狀態
            if (isMenuOpen)
            {
                isPcCameraAdjusting = false;
                isTouchCameraAdjusting = false;
                if (!fixedCinemachineVersion)
                {
                    cinemachineInputAxisController.enabled = false;
                }
            }
            // 關閉菜單時的啟用狀態會由 Update() 中的 UpdateControllerState 自動處理
        }
    }
}