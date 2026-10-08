using UnityEngine;
using UnityEngine.InputSystem;   // Unity 6 預設使用新版 Input System

public class FxTester : MonoBehaviour
{
    public enum SpawnMode
    {
        AtMouse,        // 在滑鼠位置生成
        AtThisObject,   // 在本物件位置生成
        AtTarget        // 在指定 Target 位置生成
    }

    [Header("測試目標")]
    [SerializeField] GameObject fxPrefab;
    [SerializeField] SpawnMode spawnMode = SpawnMode.AtMouse;
    [SerializeField] Transform target;              // AtTarget 模式使用

    [Header("朝向")]
    [SerializeField] bool faceMouseDirection = true;   // 從本物件朝向滑鼠方向
    [SerializeField] float angleOffset = 0f;           // 額外角度修正
    [SerializeField] float randomAngle = 0f;           // 隨機角度 ±
    [SerializeField] bool randomFlipY = false;         // 隨機上下翻轉

    [Header("觸發")]
    [SerializeField] bool clickToSpawn = true;      // 滑鼠左鍵
    [SerializeField] Key spawnKey = Key.Space;      // 或按鍵
    [SerializeField] bool autoLoop = false;         // 自動循環生成
    [SerializeField] float loopInterval = 1f;

    [Header("輔助")]
    [SerializeField] bool autoDestroy = false;      // 特效本身沒自毀時使用
    [SerializeField] float destroyAfter = 2f;
    [SerializeField] bool drawGizmo = true;

    Camera cam;
    float loopTimer;

    void Awake() => cam = Camera.main;

    void Update()
    {
        if (fxPrefab == null) return;

        bool triggered = false;

        if (clickToSpawn && Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
            triggered = true;

        if (Keyboard.current != null &&
            Keyboard.current[spawnKey].wasPressedThisFrame)
            triggered = true;

        if (autoLoop)
        {
            loopTimer += Time.unscaledDeltaTime;
            if (loopTimer >= loopInterval)
            {
                loopTimer = 0f;
                triggered = true;
            }
        }

        if (triggered) Spawn();
    }

    [ContextMenu("Spawn Now")]
    public void Spawn()
    {
        if (fxPrefab == null) return;

        Vector2 mouseWorld = GetMouseWorld();
        Vector2 pos = spawnMode switch
        {
            SpawnMode.AtMouse => mouseWorld,
            SpawnMode.AtTarget => target ? (Vector2)target.position : transform.position,
            _ => transform.position
        };

        float angle = angleOffset;
        if (faceMouseDirection)
        {
            Vector2 origin = spawnMode == SpawnMode.AtMouse
                ? (Vector2)transform.position
                : pos;
            Vector2 dir = mouseWorld - origin;
            if (dir.sqrMagnitude > 0.0001f)
                angle += Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }
        angle += Random.Range(-randomAngle, randomAngle);

        GameObject go = Instantiate(fxPrefab, pos, Quaternion.Euler(0, 0, angle));

        if (randomFlipY && Random.value > 0.5f)
        {
            Vector3 s = go.transform.localScale;
            s.y *= -1f;
            go.transform.localScale = s;
        }

        if (autoDestroy) Destroy(go, destroyAfter);
    }

    Vector2 GetMouseWorld()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || Mouse.current == null) return transform.position;

        Vector3 p = Mouse.current.position.ReadValue();
        p.z = Mathf.Abs(cam.transform.position.z);
        return cam.ScreenToWorldPoint(p);
    }

    void OnDrawGizmos()
    {
        if (!drawGizmo) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}