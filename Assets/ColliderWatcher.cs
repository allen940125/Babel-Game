using UnityEngine;

[DefaultExecutionOrder(-9999)] // 確保比所有腳本更早執行
public class ColliderWatcher : MonoBehaviour
{
    private BoxCollider _col;
    private Vector3 _lastSize;

    private void Awake()
    {
        _col = GetComponent<BoxCollider>();
        if (_col != null)
        {
            _lastSize = _col.size;
            Debug.Log($"[Watcher-Awake] 初始 Size: {_lastSize}");
        }
    }

    private void Start()
    {
        CheckSize("Start");
    }

    private void LateUpdate()
    {
        CheckSize("LateUpdate");
    }

    private void CheckSize(string phase)
    {
        if (_col == null) return;
        
        if (_col.size != _lastSize)
        {
            Debug.LogError($"[異常抓取] {_col.gameObject.name} 的 BoxCollider Size 在 {phase} 階段被修改了！\n從 {_lastSize} 變成 {_col.size}");
            _lastSize = _col.size;
            
            // 印出當下的呼叫堆疊，有助於追蹤是誰引發了這幀的變化
            Debug.Log(System.Environment.StackTrace);
        }
    }
}