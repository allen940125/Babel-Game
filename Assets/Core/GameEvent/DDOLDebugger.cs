using UnityEngine;
using UnityEngine.SceneManagement;

public class DDOLDebugger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("==== 開始檢查 DDOL 場景 ====");
            
            // 由於 Unity 沒有直接取得 DDOL 場景的 API，我們用這個 hack 技巧
            GameObject temp = new GameObject("TempDDOLMarker");
            DontDestroyOnLoad(temp);
            Scene ddolScene = temp.scene;
            Destroy(temp);

            if (!ddolScene.IsValid()) return;

            GameObject[] rootObjects = ddolScene.GetRootGameObjects();
            foreach (var go in rootObjects)
            {
                Debug.Log($"<color=green>[Root]</color> {go.name}");
                foreach (Transform child in go.transform)
                {
                    Debug.Log($"   └─ <color=yellow>[Child]</color> {child.name}");
                }
            }
            Debug.Log("==== 檢查結束 ====");
        }
    }
}