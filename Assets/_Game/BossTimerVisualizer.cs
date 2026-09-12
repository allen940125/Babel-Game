using UnityEngine;
using Gamemanager; // 假設 GameManager 位於此命名空間

public class BossTimerVisualizer : MonoBehaviour
{
    [Header("目標渲染器 (掛載該 Shader 的物件)")]
    [SerializeField] private Renderer targetRenderer;
    
    private Material _runtimeMaterial;
    private int _fillAmountPropertyID;

    private void Start()
    {
        if (targetRenderer == null)
        {
            Debug.LogError("BossTimerVisualizer: 未綁定 Target Renderer！");
            this.enabled = false;
            return;
        }

        // 1. 取得執行期材質的獨立拷貝 (避免修改到專案共用材質)
        _runtimeMaterial = targetRenderer.material;
        
        // 2. 將字串轉換為效能較佳的 Hash ID
        _fillAmountPropertyID = Shader.PropertyToID("_FillAmount");
    }

    private void Update()
    {
        // 1. 防呆：確保 Mediator 與 Boss 實體存在
        if (GameManager.Instance == null || GameManager.Instance.MainGameMediator == null) return;
        
        var bossRuntime = GameManager.Instance.MainGameMediator.CurrentBossRuntime;
        if (bossRuntime == null) return;

        // 2. 獲取 Boss 身上的 TimerTrait
        // 注意：這裡假設你的 EntityRuntime 有 GetTrait<T>() 方法，請依據你實際的 API 替換
        var timerTrait = bossRuntime.GetTrait<TimerTrait>();
        
        if (timerTrait != null)
        {
            // 3. 寫入 Shader 參數
            _runtimeMaterial.SetFloat(_fillAmountPropertyID, timerTrait.TimerRatio);
        }
    }

    private void OnDestroy()
    {
        // 釋放動態建立的材質，避免 Memory Leak
        if (_runtimeMaterial != null)
        {
            Destroy(_runtimeMaterial);
        }
    }
}