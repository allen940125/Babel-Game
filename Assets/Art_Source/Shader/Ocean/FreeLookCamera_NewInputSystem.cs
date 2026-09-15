using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 自由視角攝影機控制器
/// 操作方式：
///   - 按住滑鼠右鍵 + 滑鼠移動：旋轉視角
///   - W / A / S / D：前後左右移動
///   - Q / E：垂直下降 / 上升
///   - 左 Shift：加速移動
///   - 滑鼠滾輪：加快或減慢移動速度
/// </summary>
public class FreeLookCamera : MonoBehaviour
{
    [Header("移動設定")]
    [Tooltip("基本移動速度（單位：米/秒）")]
    public float moveSpeed = 5f;

    [Tooltip("按住 Shift 時的加速倍率")]
    public float sprintMultiplier = 3f;

    [Tooltip("滾輪調整速度時的靈敏度")]
    public float scrollSpeedStep = 0.01f;

    [Header("旋轉設定")]
    [Tooltip("滑鼠旋轉靈敏度")]
    public float lookSensitivity = 0.1f;

    [Tooltip("限制上下看的角度範圍")]
    public float pitchLimit = 89f;

    private float yaw;
    private float pitch;

    void Start()
    {
         Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null)
        {
            return;
        }

        HandleLook();
        HandleMove();
        HandleScrollSpeed();
    }

    void HandleLook()
    {
        bool rightMouseHeld = Mouse.current.rightButton.isPressed;

        if (rightMouseHeld)
        {
           Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            yaw += mouseDelta.x * lookSensitivity;
            pitch -= mouseDelta.y * lookSensitivity;
            pitch = Mathf.Clamp(pitch, -pitchLimit, pitchLimit);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void HandleMove()
    {
        float speed = moveSpeed;
        if (Keyboard.current.leftShiftKey.isPressed)
        {
            speed *= sprintMultiplier;
        }

        Vector3 move = Vector3.zero;

        if (Keyboard.current.wKey.isPressed) move += transform.forward;
        if (Keyboard.current.sKey.isPressed) move -= transform.forward;
        if (Keyboard.current.aKey.isPressed) move -= transform.right;
        if (Keyboard.current.dKey.isPressed) move += transform.right;
        if (Keyboard.current.eKey.isPressed) move += Vector3.up;
        if (Keyboard.current.qKey.isPressed) move -= Vector3.up;

        transform.position += move.normalized * speed * Time.deltaTime;
    }

    void HandleScrollSpeed()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            moveSpeed = Mathf.Max(0.5f, moveSpeed + scroll * scrollSpeedStep);
        }
    }
}
