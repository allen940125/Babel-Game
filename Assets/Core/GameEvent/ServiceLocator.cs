using System;
using System.Collections.Generic;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

    public static void Register<T>(T service)
    {
        if (_services.ContainsKey(typeof(T)))
        {
            UnityEngine.Debug.LogWarning($"[ServiceLocator] 服務 {typeof(T)} 已存在，將被覆蓋。");
        }
        _services[typeof(T)] = service;
    }

    public static T Get<T>()
    {
        if (_services.TryGetValue(typeof(T), out var service))
        {
            return (T)service;
        }
        throw new Exception($"[ServiceLocator] 找不到註冊的服務: {typeof(T)}。請確認是否已在 Awake 階段註冊。");
    }

    public static void Clear()
    {
        _services.Clear();
    }
}