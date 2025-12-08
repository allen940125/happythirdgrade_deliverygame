using UnityEngine;
using System.Collections.Generic; // 需要引用這個來使用 List

public class CarHealth : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 定義一個小類別來管理階段 (會在 Inspector 顯示)
    // ──────────────────────────────────────────────
    [System.Serializable]
    public class DamageEffectStage
    {
        [Tooltip("敘述 (例如: 輕微冒煙)")]
        public string name; 
        
        [Range(0f, 1f)]
        [Tooltip("當血量百分比低於此數值時觸發 (0.5 代表 50%)")]
        public float healthPercentageThreshold;

        [Tooltip("對應的粒子特效 (記得勾選 Loop)")]
        public ParticleSystem particleEffect;
    }

    [Header("血量設定")]
    public float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("特效設定")]
    [Tooltip("死亡時的爆炸特效 (一次性)")]
    public ParticleSystem deathExplosion;
    
    [Tooltip("持續性的損壞特效列表 (冒煙、起火)")]
    public List<DamageEffectStage> damageStages; 

    private SimpleCarController carController;

    private void Awake()
    {
        carController = GetComponent<SimpleCarController>();
        currentHealth = maxHealth;
        
        // 遊戲開始時，確保所有損壞特效都是關閉的
        foreach (var stage in damageStages)
        {
            if (stage.particleEffect != null)
                stage.particleEffect.Stop();
        }
    }

    private void OnEnable()
    {
        if (carController != null)
        {
            carController.OnCollisionHit += TakeCrashDamage;
        }
    }

    private void OnDisable()
    {
        if (carController != null)
        {
            carController.OnCollisionHit -= TakeCrashDamage;
        }
    }

    private void TakeCrashDamage(SimpleCarController.CrashLevel level, float damageFactor)
    {
        // 計算傷害
        float damageToTake = 50f * damageFactor; // 這裡可以依需求調整傷害係數
        currentHealth -= damageToTake;

        // 確保血量不低於 0
        currentHealth = Mathf.Max(0, currentHealth);

        Debug.Log($"<color=red>扣血: {damageToTake:F1}</color> | 剩餘: {currentHealth:F1}");

        // >> 檢查並更新特效狀態 <<
        UpdateDamageEffects();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // 這是新增的核心功能：檢查當前血量應該要開哪個特效
    private void UpdateDamageEffects()
    {
        // 計算當前血量百分比 (0.0 ~ 1.0)
        float healthPercent = currentHealth / maxHealth;

        foreach (var stage in damageStages)
        {
            if (stage.particleEffect == null) continue;

            // 如果目前血量 低於 設定的門檻
            if (healthPercent <= stage.healthPercentageThreshold)
            {
                // 如果特效還沒播放，就播放它
                if (!stage.particleEffect.isPlaying)
                {
                    stage.particleEffect.Play();
                }
            }
            else
            {
                // 如果血量高於門檻 (例如補血了)，就把特效關掉
                if (stage.particleEffect.isPlaying)
                {
                    stage.particleEffect.Stop();
                }
            }
        }
    }

    private void Die()
    {
        Debug.Log("車輛全毀！");

        // 1. 讓車子失去動力
        if (carController != null)
        {
            carController.SetDrivable(false);
        }

        // 2. 播放爆炸特效
        if (deathExplosion != null)
        {
            deathExplosion.transform.parent = null; 
            deathExplosion.Play();
            Destroy(deathExplosion.gameObject, 3f); 
        }

        // 3. (進階) 如果想要車子爆炸後飛起來，可以在這裡加一個推力
        GetComponent<Rigidbody>().AddExplosionForce(50000f, transform.position + Vector3.down, 5f);
    }
}