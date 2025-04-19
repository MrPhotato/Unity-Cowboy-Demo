using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class DirectDoorTransition : MonoBehaviour
{
    [Header("场景设置")]
    public string nextSceneName = "Demo"; // 默认设置为Demo场景，可在Inspector中修改
    public KeyCode[] interactionKeys = new KeyCode[] { KeyCode.F, KeyCode.E, KeyCode.Space }; // 多个交互键

    [Header("玩家检测")]
    public float activationDistance = 3.0f; // 触发距离
    public Transform doorTransform; // 门的位置
    private Transform playerTransform; // 玩家位置

    [Header("UI提示")]
    public GameObject promptCanvas; // 提示UI画布
    public TextMeshProUGUI promptText; // 提示文本
    public string promptMessage = "Press F To Enter"; // 自定义提示文本
    public Color textColor = Color.white; // 文本颜色
    public int fontSize = 24; // 字体大小
    
    // 内部状态变量
    private bool playerInRange = false;
    private bool doorLoaded = false;
    private float lastDistanceCheck = 0f;
    private float lastInputPoll = 0f;

    private void Awake()
    {
        Debug.Log("[DirectDoor] 直接门传送脚本已启动!");
        
        // 如果没有设置门的Transform，使用此脚本所在对象
        if (doorTransform == null)
        {
            doorTransform = transform;
            Debug.Log("[DirectDoor] 使用脚本所在对象作为门位置");
        }
        
        // 创建提示UI（如果未提供）
        if (promptCanvas == null)
        {
            CreatePromptUI();
        }
        else
        {
            // 确保初始隐藏
            promptCanvas.SetActive(false);
        }
        
        // 创建一个全局输入监听器
        GameObject obj = new GameObject("InputListener");
        InputListener listener = obj.AddComponent<InputListener>();
        listener.doorTransition = this;
        DontDestroyOnLoad(obj);
    }
    
    private void Start()
    {
        // 尝试查找玩家
        FindPlayer();
        
        // 每帧强制更新状态，确保UI正确显示
        InvokeRepeating("ForceStateCheck", 0.1f, 0.1f);
    }
    
    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            Debug.Log("[DirectDoor] 找到玩家: " + playerTransform.name);
        }
        else
        {
            Debug.LogWarning("[DirectDoor] 未找到Player标签的对象! 请确保玩家有Player标签");
        }
    }
    
    // 创建简单的提示UI
    private void CreatePromptUI()
    {
        // 创建画布
        GameObject canvas = new GameObject("DoorPromptCanvas");
        Canvas canvasComp = canvas.AddComponent<Canvas>();
        canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // 创建背景面板
        GameObject panel = new GameObject("PromptPanel");
        panel.transform.SetParent(canvas.transform, false);
        UnityEngine.UI.Image panelImage = panel.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0, 0, 0, 0.5f);
        
        // 设置面板大小和位置
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0);
        panelRect.anchorMax = new Vector2(0.5f, 0);
        panelRect.pivot = new Vector2(0.5f, 0);
        panelRect.sizeDelta = new Vector2(400, 60);
        panelRect.anchoredPosition = new Vector2(0, 60);
        
        // 创建文本
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(panel.transform, false);
        promptText = textObj.AddComponent<TextMeshProUGUI>();
        promptText.text = promptMessage; // 使用自定义提示文本
        promptText.color = textColor;
        promptText.fontSize = fontSize;
        promptText.alignment = TextAlignmentOptions.Center;
        
        // 设置文本大小和位置
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);
        
        // 保存引用并初始隐藏
        promptCanvas = canvas;
        promptCanvas.SetActive(false);
        
        Debug.Log("[DirectDoor] 已创建提示UI: " + promptMessage);
    }
    
    // 强制检查状态以更新UI
    private void ForceStateCheck()
    {
        if (playerTransform == null)
        {
            // 尝试再次查找玩家
            FindPlayer();
            if (playerTransform == null) return;
        }
        
        // 计算玩家与门的距离
        float distance = Vector3.Distance(playerTransform.position, doorTransform.position);
        
        // 判断是否在范围内
        bool inRange = distance <= activationDistance;
        
        // 状态变化时输出日志并更新UI
        if (inRange != playerInRange)
        {
            playerInRange = inRange;
            
            if (playerInRange)
            {
                Debug.Log("[DirectDoor] 玩家进入范围，距离: " + distance.ToString("F2") + " 米");
                if (promptCanvas != null)
                {
                    promptCanvas.SetActive(true);
                }
                
                // 提醒按键
                Debug.Log("[DirectDoor] 请按 F/E/空格 键进入");
            }
            else
            {
                Debug.Log("[DirectDoor] 玩家离开范围，距离: " + distance.ToString("F2") + " 米");
                if (promptCanvas != null)
                {
                    promptCanvas.SetActive(false);
                }
            }
        }
        
        // 每秒输出一次距离信息
        if (Time.time > lastDistanceCheck + 1f)
        {
            lastDistanceCheck = Time.time;
            Debug.Log("[DirectDoor] 玩家距离: " + distance.ToString("F2") + " 米，触发距离: " + activationDistance + " 米");
        }
    }
    
    // 由InputListener调用的输入处理方法
    public void HandleKeyPress(KeyCode keyPressed)
    {
        // 检查是否是我们关注的按键
        foreach (KeyCode key in interactionKeys)
        {
            if (keyPressed == key)
            {
                Debug.Log("[DirectDoor] 检测到按键: " + keyPressed);
                
                // 如果没有玩家引用，尝试重新查找
                if (playerTransform == null)
                {
                    FindPlayer();
                }
                
                // 检查玩家是否在范围内
                if (playerTransform != null)
                {
                    float distance = Vector3.Distance(playerTransform.position, doorTransform.position);
                    
                    if (distance <= activationDistance)
                    {
                        Debug.Log("[DirectDoor] 玩家在范围内并按下交互键，触发场景切换");
                        LoadNextScene();
                    }
                    else
                    {
                        Debug.Log("[DirectDoor] 按下交互键，但玩家不在范围内，距离: " + distance.ToString("F2") + " 米");
                    }
                }
                else
                {
                    // 如果找不到玩家但按了交互键，尝试直接加载场景（紧急情况下）
                    Debug.LogWarning("[DirectDoor] 无法找到玩家但检测到交互键，尝试直接加载场景");
                    LoadNextScene();
                }
                
                break;
            }
        }
    }
    
    // 加载下一个场景
    private void LoadNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("[DirectDoor] 场景名称未设置!");
            return;
        }
        
        Debug.Log("[DirectDoor] 开始加载场景: " + nextSceneName);
        
        // 确保UI提示隐藏
        if (promptCanvas != null)
        {
            promptCanvas.SetActive(false);
        }
        
        try
        {
            doorLoaded = true;
            SceneManager.LoadScene(nextSceneName);
            Debug.Log("[DirectDoor] 场景加载指令已发送");
        }
        catch (System.Exception e)
        {
            doorLoaded = false;
            Debug.LogError("[DirectDoor] 场景加载失败: " + e.Message);
        }
    }
    
    // 绘制门的触发范围
    private void OnDrawGizmos()
    {
        // 在场景视图中显示触发范围
        if (doorTransform != null)
        {
            Gizmos.color = playerInRange ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(doorTransform.position, activationDistance);
        }
    }
}

// 独立的输入监听器，不受角色控制器影响
public class InputListener : MonoBehaviour
{
    [System.NonSerialized]
    public DirectDoorTransition doorTransition;
    
    private void Update()
    {
        // 监听所有按键输入并转发给门组件
        if (doorTransition != null)
        {
            // 检查所有可能的按键
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    doorTransition.HandleKeyPress(key);
                }
            }
        }
    }
} 