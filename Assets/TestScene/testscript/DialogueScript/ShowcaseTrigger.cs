using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 讓「調查物件 / 觸發區 / 任何事件」都能展示大立繪，不用寫程式。
/// 用法：掛在任何物件上，Sprite 填圖，
/// 然後在 InteractableObject 的 On Interacted 事件（或任何 UnityEvent）裡選這支腳本的 Play()。
/// </summary>
public class ShowcaseTrigger : MonoBehaviour
{
    [SerializeField] private Sprite sprite;
    [Tooltip("-1 = 用 ShowcaseOverlay 的預設值；0 = 只能點擊才消失；>0 = 幾秒後自動消失")]
    [SerializeField] private float autoHideSeconds = -1f;
    [Tooltip("勾選後只會展示一次")]
    [SerializeField] private bool onlyOnce;

    private bool played;

    public void Play()
    {
        if (onlyOnce && played) return;
        played = true;
        ShowcaseOverlay.Play(sprite, autoHideSeconds).Forget();
    }
}