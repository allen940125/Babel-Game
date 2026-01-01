using Gamemanager;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game.Input
{
    public enum InputType
    {
        Player,
        UI,
    }

    public class InputManagers : GameInputActions.IPlayerActions, GameInputActions.IUIActions ,IInitializable
    {
        private GameInputActions _inputActions;

        private bool _uiOnly;

        public void Initialize()
        {
            if (_inputActions == null)
            {
                _inputActions = new GameInputActions();

                _inputActions.Player.SetCallbacks(this);
                _inputActions.UI.SetCallbacks(this);
            }
            
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnSceneLoadedEvent, OnSceneLoadedEvent);
        }

        public void Cleanup()
        {
            GameManager.Instance.MainGameEvent.Unsubscribe<SceneLoadedEvent>(OnSceneLoadedEvent);
        }

        #region 事件訂閱

        private void OnSceneLoadedEvent(SceneLoadedEvent cmd)
        {
            LoadSceneActionsMapConfig();
        }

        #endregion
        
        /// <summary>
        /// 載入場景時調整ActionsMap
        /// </summary>
        private void LoadSceneActionsMapConfig()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;

            InputConfig.SceneInput sceneInput = GameManager.Instance.GameSo.inputConfig.GetInputActionsForScene(currentSceneName);

            if (sceneInput != null)
            {
                foreach (InputType inputAction in sceneInput.startInputActions)
                {
                    Debug.Log($"[Input] 啟用輸入模式: {inputAction}");
                    SetInputActive(inputAction);
                }

                _uiOnly = sceneInput.uiOnly;
            }
            else
            {
                Debug.LogWarning($"[Input] 無輸入設定，預設啟用 Player 模式");
                SetInputActive(InputType.Player);
                _uiOnly = false;
            }

            // 額外 Debug：顯示目前啟用中的 map
            foreach (var map in _inputActions.asset.actionMaps)
            {
                Debug.Log($"[Input] Map: {map.name}, Enabled: {map.enabled}");
            }
        }


        #region Player

        public void OnAxes(InputAction.CallbackContext context)
        {
            Vector2 input = context.ReadValue<Vector2>();
    
            //Debug.Log($"OnAxes: {input}");

            if (context.phase == InputActionPhase.Performed && input != Vector2.zero)
            {
                GameManager.Instance.MainGameEvent.Send(new MovementKeyPressedEvent() { MoveInput = input });
            }
            else if (context.phase == InputActionPhase.Canceled || input == Vector2.zero)
            {
                GameManager.Instance.MainGameEvent.Send(new MovementKeyPressedEvent() { MoveInput = Vector2.zero });
            }
        }



        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed) // 按下
            {
                GameManager.Instance.MainGameEvent.Send(new JumpPressedEvent() { JumpPressed = true });
            }
            else if (context.phase == InputActionPhase.Canceled) // 放開
            {
                GameManager.Instance.MainGameEvent.Send(new JumpPressedEvent() { JumpPressed = false });
            }
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed) // 按下
            {
                GameManager.Instance.MainGameEvent.Send(new AttackPressedEvent() { AttackPressed = true });
            }
            else if (context.phase == InputActionPhase.Canceled) // 放開
            {
                GameManager.Instance.MainGameEvent.Send(new AttackPressedEvent() { AttackPressed = false });
            }
        }

        public void OnChargedAttack(InputAction.CallbackContext context)
        {

        }

        public void OnLook(InputAction.CallbackContext context)
        {

        }

        public void OnZoom(InputAction.CallbackContext context)
        {

        }

        public void OnLockEnemyToggle(InputAction.CallbackContext context)
        {

        }

        public void OnCursorToggle(InputAction.CallbackContext context)
        {

        }

        public void OnRun(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed) // 按下
            {
                GameManager.Instance.MainGameEvent.Send(new RunPressedEvent() { RunPressed = true });
            }
            else if (context.phase == InputActionPhase.Canceled) // 放開
            {
                GameManager.Instance.MainGameEvent.Send(new RunPressedEvent() { RunPressed = false });
            }
        }

        private bool _isWalking = false;

        public void OnWalkToggle(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed)
            {
                _isWalking = !_isWalking; // 切換狀態
                GameManager.Instance.MainGameEvent.Send(new WalkToggledEvent() { WalkToggled = _isWalking });
            }
        }

        public void OnRoll(InputAction.CallbackContext context)
        {

        }

        public void OnDash(InputAction.CallbackContext context)
        {

        }

        public void OnCameraRotation_Ctrl(InputAction.CallbackContext context)
        {

        }

        public void OnCameraRotation_Alt(InputAction.CallbackContext context)
        {

        }

        public void OnLockEnemy(InputAction.CallbackContext context)
        {

        }

        #endregion

        #region UI

        public void OnDialogue(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Started)
            {
                Debug.Log("啟動對話功能");
            }
        }

        public void OnBackPack(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Started)
            {
                Debug.Log("打開背包");
                GameManager.Instance.MainGameEvent.Send(new OpenBackpackKeyPressedEvent());  
            }
        }

        public void OnEscape(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Started)
            {
                Debug.Log("按下ESC");
                GameManager.Instance.MainGameEvent.Send(new EscapeKeyPressedEvent());  
            }
        }

        public void OnAdjust(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        #endregion

        #region 設置按鍵輸入行為

        /// <summary>
        /// 設置按鍵輸入地圖
        /// </summary>
        /// <param name="inputType"></param>
        private void SetActionsMap(InputType inputType)
        {
            Debug.Log("[Input] 切換輸入至：" + inputType.ToString());

            foreach (InputActionMap inputAction in _inputActions.asset.actionMaps)
            {
                if (inputAction.name == inputType.ToString())
                {
                    Debug.Log($"[Input] 啟用 {inputAction.name}");
                    inputAction.Enable();
                }
                else
                {
                    inputAction.Disable();
                }
            }
        }


        /// <summary>
        /// 進入UI界面
        /// </summary>
        private void EnterUI()
        {
            SetActionsMap(InputType.UI);

            // 顯示滑鼠
            //Cursor.lockState = CursorLockMode.None;
            //Cursor.visible = true;

            // 停止遊戲時間（如有需要）
            Time.timeScale = 1;
        }

        /// <summary>
        /// 退出UI界面
        /// </summary>
        private void EnterPlayer()
        {
            SetActionsMap(InputType.Player);

            // 隱藏滑鼠並將其位置設為螢幕中央
            //Cursor.lockState = CursorLockMode.Locked;
            //Cursor.visible = true;

            // 恢復遊戲時間
            Time.timeScale = 1;
        }

        /// <summary>
        /// 設置按鍵輸入狀態
        /// </summary>
        /// <param name="target">目標按鍵輸入狀態</param>
        public void SetInputActive(InputType inputType)
        {
            if (!_uiOnly)
            {
                switch (inputType)
                {
                    case InputType.Player:
                        {
                            EnterPlayer();
                            Debug.Log("按鍵操作設置為Player");
                            break;
                        }
                    case InputType.UI:
                        {
                            EnterUI();
                            Debug.Log("按鍵操作設置為UI");
                            break;
                        }
                }
            }
        }
        
        public void DebugCheckInputSwitch()
        {
            if (Keyboard.current.f1Key.wasPressedThisFrame)
            {
                Debug.Log("[Debug] F1 按下，切換到 Player");
                SetInputActive(InputType.Player);
            }

            if (Keyboard.current.f2Key.wasPressedThisFrame)
            {
                Debug.Log("[Debug] F2 按下，切換到 UI");
                SetInputActive(InputType.UI);
            }
        }

    }

    #endregion
}
