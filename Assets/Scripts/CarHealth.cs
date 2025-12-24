using UnityEngine;
using System.Collections.Generic;
// using Gamemanager; // 父類別通常不需要依賴 GameManager 的事件

public class BaseCarHealth : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // 內嵌類別 (保持不變)
    // ──────────────────────────────────────────────
    [System.Serializable]
    public class DamageEffectStage
    {
        public string name;
        [Range(0f, 1f)]
        public float healthPercentageThreshold;
        public ParticleSystem particleEffect;
    }

    [Header("基礎血量設定")]
    public float maxHealth = 100f;
    [SerializeField] protected float currentHealth; // 改成 protected 讓子類別可以讀取

    [Header("通用特效")]
    public ParticleSystem deathExplosion;
    public List<DamageEffectStage> damageStages;

    protected BaseCarController carController; // 改成 BaseCarController 以支援所有車

    protected virtual void Awake() // 改成 protected virtual
    {
        carController = GetComponent<BaseCarController>();
        currentHealth = maxHealth;
        
        // 初始化特效狀態
        foreach (var stage in damageStages)
        {
            if (stage.particleEffect != null) stage.particleEffect.Stop();
        }
    }

    // 給 Manager 呼叫的初始化
    public void InitializeHealth(float newMaxHealth)
    {
        this.maxHealth = newMaxHealth;
        this.currentHealth = maxHealth;
        
        // 重置特效
        foreach (var stage in damageStages)
        {
            if (stage.particleEffect != null) stage.particleEffect.Stop();
        }
    }

    // ──────────────────────────────────────────────
    // 核心受傷邏輯 (公開讓外部呼叫)
    // ──────────────────────────────────────────────
    public virtual void TakeDamage(float amount)
    {
        if (currentHealth <= 0) return; // 已經死了就不再扣血

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);

        // Debug.Log($"{gameObject.name} 扣血: {amount} | 剩餘: {currentHealth}");

        UpdateDamageEffects();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected void UpdateDamageEffects()
    {
        float healthPercent = currentHealth / maxHealth;

        foreach (var stage in damageStages)
        {
            if (stage.particleEffect == null) continue;

            if (healthPercent <= stage.healthPercentageThreshold)
            {
                if (!stage.particleEffect.isPlaying) stage.particleEffect.Play();
            }
            else
            {
                if (stage.particleEffect.isPlaying) stage.particleEffect.Stop();
            }
        }
    }

    // ──────────────────────────────────────────────
    // 死亡邏輯 (Virtual 讓子類別可以改寫)
    // ──────────────────────────────────────────────
    protected virtual void Die()
    {
        // 1. 共用行為：車子失去動力
        if (carController != null)
        {
            carController.SetDrivable(false);
        }

        // 2. 共用行為：爆炸特效
        if (deathExplosion != null)
        {
            // 讓特效脫離父物件，避免車子被 Destroy 時特效跟著消失
            deathExplosion.transform.parent = null; 
            deathExplosion.Play();
            Destroy(deathExplosion.gameObject, 3f); 
        }

        // 3. 共用行為：物理炸飛
        if(GetComponent<Rigidbody>())
        {
            GetComponent<Rigidbody>().AddExplosionForce(50000f, transform.position + Vector3.down, 5f);
        }
        
        Debug.Log($"{gameObject.name} 已被摧毀 (Base Logic)");
    }
}