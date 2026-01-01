using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameSpeedManager : MonoBehaviour
{
    [SerializeField] private float _defaultTimeScale = 1f;
    [SerializeField] private float _step = 0.1f; // 每次調整的增量

    void Update()
    {
        #region 測試用

        if (Keyboard.current.rightBracketKey.wasPressedThisFrame) // ] 增加速度
        {
            Debug.Log("鍵盤上的 ] 鍵被按下了 (Was Pressed This Frame)！");
            IncreaseSpeed();
        }

        if (Keyboard.current.leftBracketKey.wasPressedThisFrame) // [ 減少速度
        {
            Debug.Log("鍵盤上的 [ 鍵被按下了 (Was Pressed This Frame)！");
            DecreaseSpeed();
        }

        #endregion
    }

    private void OnValidate()
    {
        SetGameSpeed(_defaultTimeScale);
    }

    /// <summary>
    /// 設定遊戲速度（0 = 暫停, 1 = 正常速度, >1 = 加速, <1 = 慢動作）
    /// </summary>
    public void SetGameSpeed(float speed)
    {
        speed = Mathf.Clamp(speed, 0f, 10f);
        Time.timeScale = speed;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    public void ResetGameSpeed() => SetGameSpeed(_defaultTimeScale);

    public void PauseGame() => SetGameSpeed(0f);

    public void ResumeGame() => SetGameSpeed(_defaultTimeScale);

    public void SetDefaultSpeed(float speed)
    {
        _defaultTimeScale = Mathf.Clamp(speed, 0.1f, 10f);
    }

    /// <summary>
    /// 增加遊戲速度
    /// </summary>
    public void IncreaseSpeed()
    {
        SetGameSpeed(Time.timeScale + _step);
    }

    /// <summary>
    /// 減少遊戲速度
    /// </summary>
    public void DecreaseSpeed()
    {
        SetGameSpeed(Time.timeScale - _step);
    }
}