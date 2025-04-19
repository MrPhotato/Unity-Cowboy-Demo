using UnityEngine;
using System.Collections;
using StarterAssets;

public class PlayerHealth : MonoBehaviour
{
    [Header("生命值设置")]
    public int maxHealth = 3; // 最大生命值
    public int currentHealth; // 当前生命值
    
    [Header("受伤和死亡")]
    public float invincibilityTime = 1f; // 受伤后的无敌时间
    public float deathDelay = 1f; // 死亡后销毁延迟
    
    [Header("视觉反馈")]
    public GameObject damageEffectPrefab; // 受伤特效预制体
    
    // 内部状态
    private bool isInvincible = false;
    private bool isDead = false;
    
    // 组件引用
    private ThirdPersonController playerController;
    private StarterAssetsInputs playerInputs;
    private Animator animator;
    
    private void Start()
    {
        // 初始化生命值
        currentHealth = maxHealth;
        
        // 获取组件引用
        playerController = GetComponent<ThirdPersonController>();
        playerInputs = GetComponent<StarterAssetsInputs>();
        animator = GetComponent<Animator>();
        
        Debug.Log("玩家生命值: " + currentHealth + "/" + maxHealth);
    }
    
    // 受到伤害
    public void TakeDamage(int damage)
    {
        // 如果无敌或已死亡，则无视伤害
        if (isInvincible || isDead)
            return;
            
        // 扣除生命值
        currentHealth -= damage;
        Debug.Log("玩家受到伤害! 剩余生命: " + currentHealth + "/" + maxHealth);
        
        // 触发受伤特效
        ShowDamageEffect();
        
        // 检查是否死亡
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // 短暂无敌时间
            StartCoroutine(BecomeTemporarilyInvincible());
        }
    }
    
    // 触发受伤特效
    private void ShowDamageEffect()
    {
        if (damageEffectPrefab != null)
        {
            // 在玩家位置实例化受伤特效
            GameObject effect = Instantiate(damageEffectPrefab, transform.position + Vector3.up, Quaternion.identity);
            Destroy(effect, 2f); // 2秒后销毁特效
        }
    }
    
    // 短暂无敌时间
    private IEnumerator BecomeTemporarilyInvincible()
    {
        isInvincible = true;
        
        // 可以在这里添加受伤时的视觉反馈，如闪烁效果
        
        yield return new WaitForSeconds(invincibilityTime);
        
        isInvincible = false;
    }
    
    // 处理死亡
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        currentHealth = 0;
        
        Debug.Log("玩家死亡!");
        
        // 触发死亡动画
        if (playerInputs != null)
        {
            playerInputs.DeadInput(true);
        }
        
        // 禁用玩家控制
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // 禁用碰撞体
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        
        // 延迟后销毁或隐藏
        StartCoroutine(DeathSequence());
    }
    
    // 死亡后的处理序列
    private IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(deathDelay);
        
        // 隐藏玩家
        gameObject.SetActive(false);
        
        // 在这里可以添加游戏结束、重新开始或重生的逻辑
    }
    
    // 重置生命值
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        isInvincible = false;
    }
} 