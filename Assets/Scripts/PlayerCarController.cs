using UnityEngine;
using Gamemanager;

public class PlayerCarController : SimpleCarController
{
    private Vector2 movementInput;

    private void Awake()
    {
        // 只有玩家需要註冊自己
        GameManager.Instance.SetPlayer(gameObject);
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
}