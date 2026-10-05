using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 九宮格裡的一格。掛在「Button」prefab 上即可（Button 本身的 Image 當圖片，
/// 底下的 TextMeshPro 文字當標籤）。沒有素材時用色塊 + 文字佔位，
/// 有 Sprite 之後會自動改顯示圖片並隱藏文字。
/// </summary>
public class CaptchaCell : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI label;

    public bool IsCorrect { get; private set; }
    public event Action<CaptchaCell> Clicked;

    private Color baseColor = Color.white;
    private bool wired;

    /// <summary>由 CaptchaMinigame 在生成後呼叫（不能依賴 Awake，因為父物件可能還沒啟用）。</summary>
    public void Init()
    {
        if (wired) return;
        wired = true;

        if (button == null) button = GetComponent<Button>();
        if (image == null) image = GetComponent<Image>();
        if (label == null) label = GetComponentInChildren<TextMeshProUGUI>(true);

        button.onClick.AddListener(() => Clicked?.Invoke(this));
    }

    public void Setup(CaptchaOption option, bool isCorrect)
    {
        IsCorrect = isCorrect;

        bool hasSprite = option.sprite != null;
        image.sprite = option.sprite;
        baseColor = hasSprite ? Color.white : option.color;
        image.color = baseColor;

        if (label != null)
        {
            label.text = option.label;
            label.gameObject.SetActive(!hasSprite); // 有圖就不顯示佔位文字
        }
    }

    public void SetInteractable(bool value)
    {
        button.interactable = value;
    }

    public void SetTint(Color tint)
    {
        image.color = tint;
    }

    public void ResetTint()
    {
        image.color = baseColor;
    }
}
