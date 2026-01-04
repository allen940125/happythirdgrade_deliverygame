using UnityEngine;
using Gamemanager;

public class PlayerCarController : BaseCarController
{
    private Vector2 movementInput;

    private void Awake()
    {
        // 只有玩家需要註冊自己
        GameManager.Instance.SetPlayer(gameObject);
        
        InventoryManager.Instance.AddItem(100, 500);
        InventoryManager.Instance.AddItem(999, 1);
        InventoryManager.Instance.AddItem(100, 500);
        InventoryManager.Instance.AddItem(999, 1);
        InventoryManager.Instance.AddItem(100, 500);
        InventoryManager.Instance.AddItem(999, 1);
        InventoryManager.Instance.AddItem(100, 500);
        InventoryManager.Instance.AddItem(999, 1);
        InventoryManager.Instance.AddItem(100, 500);
        InventoryManager.Instance.AddItem(999, 1);
            SaveManager.Instance.SaveGame(); // 統一保存存檔
            SaveManager.Instance.SaveSettings();
    }

    private void OnEnable()
    {
        // 訂閱玩家輸入事件
        GameManager.Instance.MainGameEvent.SetSubscribe(
            GameManager.Instance.MainGameEvent.OnMovementKeyPressedEvent,
            cmd => {
                movementInput = cmd.MoveInput;
            }
        );
    }

    // 玩家特有的 Update：處理輸入轉換
    private void Update()
    {
        float steer = movementInput.x;
        float motor = 0f;
        float brake = 0f;

        // 你的 Auto-Drive 邏輯
        if (movementInput.y < 0f) 
        {
            motor = -1f; // 倒車
        }
        else 
        {
            motor = 1f; // 自動前進
        }

        // 重要！把算好的結果傳給父類別
        SetInputs(steer, motor, brake);
    }
    
    protected override void OnCarCrash(CrashLevel level, float damageFactor, float impactForce, Vector3 hitNormal)
    {
        // 注意：這裡我們【不】呼叫 base.OnCarCrash(...) 
        // 因為我們要用事件來觸發扣血，如果呼叫 base 就會扣兩次

        // 1. 計算傷害 (或是把計算邏輯寫在 Health 裡也可以，這裡先算好傳過去)
        float calculatedDamageFactor = damageFactor; 
        if (level == CrashLevel.Heavy) calculatedDamageFactor *= 2f; // 假設嚴重撞擊係數加倍

        // 2. 發送事件！
        // 這樣 PlayerCarHealth 會收到 -> 扣血
        // 你的 UI Manager 也會收到 -> 更新血條 / 震動相機
        GameManager.Instance.MainGameEvent.Send(new PlayerHurtPressedEvent() 
        { 
            HurtValue = calculatedDamageFactor, 
            SCrashLevel = level // 把撞擊等級也傳出去，UI可能需要
        });

        Debug.Log($"[Player] 發生撞擊，已發送事件。等級: {level}");
    }
}