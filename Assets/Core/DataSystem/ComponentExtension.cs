using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class ComponentExtension
{
    // 統一紀錄所有視覺元件的非同步載入任務，利用 InstanceID 作為唯一辨識碼
    private static Dictionary<int, string> _loadingTasks = new Dictionary<int, string>();

    /// <summary>
    /// 針對 UI Image 的非同步載入擴充
    /// </summary>
    public static async void LoadSpriteAsync(this Image img, string addressableKey)
    {
        if (img == null) return;

        if (string.IsNullOrEmpty(addressableKey))
        {
            img.sprite = null;
            return;
        }

        int id = img.GetInstanceID();
        _loadingTasks[id] = addressableKey;

        Sprite sprite = await ResourceManager.LoadAssetAsync<Sprite>(addressableKey);

        if (img == null)
        {
            _loadingTasks.Remove(id);
            return;
        }

        if (_loadingTasks.TryGetValue(id, out string currentKey) && currentKey == addressableKey)
        {
            img.sprite = sprite;
            img.color = Color.white;
            _loadingTasks.Remove(id);
        }
    }

    /// <summary>
    /// 針對 3D 世界物件 SpriteRenderer 的非同步載入擴充
    /// </summary>
    public static async void LoadSpriteAsync(this SpriteRenderer renderer, string addressableKey)
    {
        if (renderer == null) return;

        if (string.IsNullOrEmpty(addressableKey))
        {
            renderer.sprite = null;
            return;
        }

        int id = renderer.GetInstanceID();
        _loadingTasks[id] = addressableKey; // 註冊最新的請求

        Sprite sprite = await ResourceManager.LoadAssetAsync<Sprite>(addressableKey);

        // 防呆 1：載入完成時物件是否已被銷毀（如被玩家撿走）
        if (renderer == null)
        {
            _loadingTasks.Remove(id);
            return;
        }

        // 防呆 2：競態條件檢查
        if (_loadingTasks.TryGetValue(id, out string currentKey) && currentKey == addressableKey)
        {
            renderer.sprite = sprite;
            _loadingTasks.Remove(id);
        }
    }
}