using Game.SceneManagement;
using UnityEngine;

namespace Gamemanager
{
    // ======================
    // Common Event Streams
    // ======================
    
    public class GameInitializedEvent : GameEventMessageBase
    {
        public int SavePointValue;
    }
    
    // ======================
    // Inventory Event Streams
    // ======================
    
    public class PlayerBagRefreshedEvent : GameEventMessageBase
    {
        public ItemControllerType ItemControllerType;
    }

    public class InventoryItemClickedEvent : GameEventMessageBase
    {
        public InventoryItemRuntimeData StoredInventoryItemRuntimeData;
    }
    
    public class ItemAddedToBagEvent : GameEventMessageBase
    {
        public int ItemID;
        public int Quantity;
    }
    
    // ======================
    // Store Event Streams
    // ======================

    public class StoreItemsRefreshedEvent : GameEventMessageBase
    {
        public ItemControllerType ItemControllerType;
    }
    
    public class StoreItemClickedEvent : GameEventMessageBase
    {
        public StoreItemRuntimeData StoreItemData;
    }

    public class PurchaseItemClickedEvent : GameEventMessageBase
    {
        
    }

    // ======================
    // Scene Event Streams
    // ======================
    
    public class SceneTransitionStartedEvent : GameEventMessageBase
    {

    }

    public class SceneLoadedEvent : GameEventMessageBase
    {
        // 我們需要一個公共欄位或屬性來攜帶數據
        public SceneType SceneType { get; private set; }

        // 構造函數：在發送事件時，必須傳入 SceneType
        public SceneLoadedEvent(SceneType sceneType)
        {
            this.SceneType = sceneType;
        }
    }
    
    // ======================
    // Input Player Event Streams
    // ======================

    public class FixedUpdateEvent : GameEventMessageBase
    {
        
    }
    
    public class MovementKeyPressedEvent : GameEventMessageBase
    {
        public Vector2 MoveInput;
    }
    
    public class CursorToggledEvent : GameEventMessageBase
    {
        public bool? ShowCursor;
    }

    public class WalkToggledEvent : GameEventMessageBase
    {
        public bool WalkToggled = false;
    }

    public class RunPressedEvent : GameEventMessageBase
    {
        public bool RunPressed = false;
    }

    public class JumpPressedEvent : GameEventMessageBase
    {
        public bool JumpPressed = false;
    }

    public class AttackPressedEvent : GameEventMessageBase
    {
        public bool AttackPressed = false;
    }
    
    // ======================
    // Input UI Event Streams
    // ======================
    
    public class EscapeKeyPressedEvent : GameEventMessageBase
    {
        
    }

    public class OpenBackpackKeyPressedEvent : GameEventMessageBase
    {
        
    }
    
    // ======================
    // Battle Event Streams
    // ======================

    public class FightButtonClickEvent : GameEventMessageBase
    {
        
    }

    public class BossEnterIdlePhaseEvent : GameEventMessageBase
    {
        
    }

    public class BossEnterAttackingPhaseEvent : GameEventMessageBase
    {
        
    }
    
    public class BossEnterVulnerablePhaseEvent : GameEventMessageBase
    {
        
    }
    
}
