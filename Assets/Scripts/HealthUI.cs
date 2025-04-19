using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthUI : MonoBehaviour
{
    [Header("引用")]
    public PlayerHealth playerHealth; // 玩家生命值组件
    
    [Header("UI元素")]
    public GameObject heartPrefab; // 心形图标预制体
    public Transform heartsContainer; // 心形图标的容器
    public TMP_Text healthText; // 文本显示(可选)
    
    [Header("设置")]
    public Vector2 heartSize = new Vector2(50, 50); // 心形图标大小
    public Vector2 heartSpacing = new Vector2(10, 0); // 心形图标间距
    
    // 心形图标列表
    private Image[] heartImages;
    
    private void Start()
    {
        // 如果没有指定玩家生命值组件，尝试查找
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerHealth = player.GetComponent<PlayerHealth>();
            }
        }
        
        if (playerHealth == null)
        {
            Debug.LogError("未找到PlayerHealth组件!");
            return;
        }
        
        // 初始化心形图标
        InitializeHearts();
        
        // 更新显示
        UpdateHealthDisplay();
    }
    
    private void Update()
    {
        // 实时更新生命值显示
        UpdateHealthDisplay();
    }
    
    // 初始化心形图标
    private void InitializeHearts()
    {
        if (heartPrefab == null || heartsContainer == null) return;
        
        // 清除现有的心形图标
        foreach (Transform child in heartsContainer)
        {
            Destroy(child.gameObject);
        }
        
        // 创建新的心形图标
        heartImages = new Image[playerHealth.maxHealth];
        
        for (int i = 0; i < playerHealth.maxHealth; i++)
        {
            GameObject heartObj = Instantiate(heartPrefab, heartsContainer);
            RectTransform rectTransform = heartObj.GetComponent<RectTransform>();
            
            // 设置大小和位置
            rectTransform.sizeDelta = heartSize;
            rectTransform.anchoredPosition = new Vector2(i * (heartSize.x + heartSpacing.x), 0);
            
            // 获取Image组件
            heartImages[i] = heartObj.GetComponent<Image>();
            
            // 命名
            heartObj.name = "Heart_" + (i + 1);
        }
    }
    
    // 更新生命值显示
    private void UpdateHealthDisplay()
    {
        if (playerHealth == null) return;
        
        // 更新文本显示
        if (healthText != null)
        {
            healthText.text = "生命: " + playerHealth.currentHealth + " / " + playerHealth.maxHealth;
        }
        
        // 更新心形图标
        if (heartImages != null)
        {
            for (int i = 0; i < heartImages.Length; i++)
            {
                if (heartImages[i] != null)
                {
                    // 根据当前生命值设置心形图标的显示状态
                    heartImages[i].color = (i < playerHealth.currentHealth) ? 
                        Color.red : new Color(0.5f, 0.5f, 0.5f, 0.5f);
                }
            }
        }
    }
} 