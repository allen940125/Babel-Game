using System;
using UnityEngine;

/// <summary>
/// 掛在警告 Prefab 的 root 上。
/// 唯一職責：確保 Icon 子物件永遠保持世界角度 0，不受發射器旋轉影響。
/// 警告：絕對不要在此腳本中操作 LineRenderer，繪圖職責已統一交由 TrajectoryVisualizer 處理。
/// </summary>
public class WarningIndicator : MonoBehaviour
{
    [Header("要被鎖定不旋轉的子物件")]
    [Tooltip("通常是驚嘆號或紅圈圖示。")]
    public Transform icon;

    private void Start()
    {
        // 嚴格鎖定世界座標角度為 0
        if (icon != null)
        {
            icon.rotation = Quaternion.identity;
        }
    }

    // private void LateUpdate()
    // {
    //     // 嚴格鎖定世界座標角度為 0
    //     if (icon != null)
    //     {
    //         icon.rotation = Quaternion.identity;
    //     }
    // }
}