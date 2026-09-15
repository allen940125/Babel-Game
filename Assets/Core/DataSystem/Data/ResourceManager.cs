using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class ResourceManager
{
    // 快取字典，避免重複載入相同的資源
    private static Dictionary<string, AsyncOperationHandle> _handleCache = new Dictionary<string, AsyncOperationHandle>();

    /// <summary>
    /// 非同步載入指定類型的資源
    /// </summary>
    public static async Task<T> LoadAssetAsync<T>(string key) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("[ResourceManager] 傳入的 Key 為空！");
            return null;
        }

        // 1. 若已經載入過，直接從快取返回
        if (_handleCache.TryGetValue(key, out AsyncOperationHandle cachedHandle))
        {
            return cachedHandle.Result as T;
        }

        // 2. 進行非同步載入
        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            _handleCache[key] = handle;
            return handle.Result;
        }
        else
        {
            Debug.LogError($"[ResourceManager] 資源載入失敗，Key: {key}");
            Addressables.Release(handle);
            return null;
        }
    }

    /// <summary>
    /// 釋放單一資源
    /// </summary>
    public static void ReleaseAsset(string key)
    {
        if (_handleCache.TryGetValue(key, out AsyncOperationHandle handle))
        {
            Addressables.Release(handle);
            _handleCache.Remove(key);
        }
    }
}