using UnityEngine;
using Gamemanager; // 只有玩家需要引用這個

public class PlayerCarHealth : BaseCarHealth
{
    private void OnEnable()
    {
        Debug.Log("註冊玩家扣血事件");
        // 訂閱：只有玩家需要聽這個測試事件
        GameManager.Instance.MainGameEvent.SetSubscribe(
            GameManager.Instance.MainGameEvent.OnPlayerHurtPressedEvent, 
            OnPlayerHurtPressedEvent
        );
    }

    private void OnDisable()
    {
        Debug.Log("註銷玩家扣血事件");
        GameManager.Instance.MainGameEvent.Unsubscribe<PlayerHurtPressedEvent>(OnPlayerHurtPressedEvent);
    }

    // 事件轉發：收到事件 -> 呼叫父類別的扣血
    private void OnPlayerHurtPressedEvent(PlayerHurtPressedEvent cmd)
    {
        // 這裡可以加入玩家特有的公式 (例如防禦力)
        float damageAmount = 50f * cmd.HurtValue; 
        Debug.Log("觸發玩家扣血事件");
        TakeDamage(damageAmount);
        
    }

    // 覆寫死亡邏輯
    protected override void Die()
    {
        base.Die(); // 先執行爆炸、炸飛

        Debug.Log(">>> PLAYER DIED! GAME OVER! <<<");
        
        // TODO: 呼叫 GameManager 顯示失敗畫面
        GameManager.Instance.MainGameEvent.Send(new GameOverEvent(GameOverReason.CarDestroyed));
    }
}