using UnityEngine;

public class EnemyCarHealth : BaseCarHealth
{
    [Header("敵人設定")]
    public int scoreValue = 100; // 打死這隻加多少分

    // 敵人通常是被撞死，或者被子彈打死，
    // 所以這裡不需要 OnEnable 訂閱事件，
    // 而是等待 BaseCarController 的 OnCollisionEnter 來呼叫 TakeDamage

    protected override void Die()
    {
        base.Die(); // 先執行爆炸、炸飛

        Debug.Log(">>> ENEMY DESTROYED! <<<");

        // TODO: 加分
        // GameManager.Instance.AddScore(scoreValue);

        // 敵人死掉後，通常過幾秒要把屍體清掉，不然場景會太亂
        Destroy(gameObject, 5f); // 5秒後銷毀屍體
    }
}