using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class DoorDistanceTransition : MonoBehaviour
{
    [Header("场景设置")]
    public string nextSceneName; // 下一个场景的名称
    public KeyCode interactKey = KeyCode.F; // 交互按键，默认为F键
    public KeyCode alternateKey = KeyCode.E; // 备用交互按键
    public bool checkAllKeys = true; // 是否检测所有可能的交互键
    private bool inputDebugMode = true; // 是否启用输入调试模式
    private float lastInputCheck = 0f; // 上次输入检查的时间
    private KeyCode[] commonKeys = new KeyCode[] { 
        KeyCode.F, KeyCode.E, KeyCode.Space, KeyCode.Return, KeyCode.KeypadEnter 
    }; // 常用的交互键
    
    [Header("目标设置")]
    public Transform doorTransform; // 教堂大门的Transform
    public Transform playerTransform; // 玩家的Transform
    public float activationDistance = 3.0f; // 激活距离，玩家需要靠近到这个距离才能触发
    
    [Header("提示UI设置")]
    public bool showPrompt = true; // 是否显示交互提示
    public GameObject promptUI; // 提示UI对象
    public TextMeshProUGUI promptText; // 提示文本组件
    public string customPromptText = "按 F 进入教堂"; // 自定义提示文本
    
    [Header("音效设置")]
    public AudioClip transitionSound; // 场景切换音效
    public float soundVolume = 1.0f; // 音效音量
    
    [Header("淡入淡出设置")]
    public bool useFadeEffect = true; // 是否使用淡入淡出效果
    public float fadeDuration = 1.0f; // 淡入淡出持续时间
    public Color fadeColor = Color.black; // 淡入淡出颜色
    
    [Header("调试设置")]
    public bool showDebugGizmo = true; // 是否显示调试图形
    public bool logDebugInfo = true; // 是否记录调试信息
    
    private bool playerInRange = false; // 玩家是否在范围内
    private AudioSource audioSource; // 音频播放器
    private float currentDistance; // 当前玩家与门的距离
    
    private void Start()
    {
        Debug.Log("[DoorTransition] 脚本启动 - 目标场景: " + nextSceneName);
        
        // 如果未指定门Transform，使用此脚本所在的物体
        if (doorTransform == null)
        {
            doorTransform = transform;
            Debug.Log("[DoorTransition] 未指定门Transform，使用当前物体");
        }
        
        // 如果未指定玩家，尝试查找
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                Debug.Log("[DoorTransition] 已自动找到玩家: " + playerTransform.name);
            }
            else
            {
                Debug.LogError("[DoorTransition] 未找到Player标签的对象！请手动设置玩家Transform");
            }
        }
        
        // 检查场景名
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("[DoorTransition] 场景名未设置！请在Inspector中设置nextSceneName属性");
        }
        
        // 初始化提示UI
        if (showPrompt)
        {
            // 如果没有指定提示UI，尝试查找或创建
            if (promptUI == null)
            {
                // 尝试查找现有提示UI
                promptUI = GameObject.FindGameObjectWithTag("PromptUI");
                
                if (promptUI == null)
                {
                    Debug.LogWarning("[DoorTransition] 未找到提示UI，交互提示将不会显示");
                }
            }
            
            // 如果有提示UI但没有提示文本组件，尝试获取
            if (promptUI != null && promptText == null)
            {
                promptText = promptUI.GetComponentInChildren<TextMeshProUGUI>();
            }
            
            // 初始隐藏提示UI
            if (promptUI != null)
            {
                promptUI.SetActive(false);
            }
        }
        
        // 初始化音频源
        if (transitionSound != null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
    }
    
    private void Update()
    {
        // 确保有玩家和门的引用
        if (playerTransform == null || doorTransform == null)
            return;
        
        // 计算玩家与门的距离
        currentDistance = Vector3.Distance(playerTransform.position, doorTransform.position);
        
        // 判断玩家是否在范围内
        bool isInRange = currentDistance <= activationDistance;
        
        // 如果玩家状态变化（进入或离开范围）
        if (isInRange != playerInRange)
        {
            playerInRange = isInRange;
            
            if (playerInRange)
            {
                // 玩家进入范围
                Debug.Log("[DoorTransition] 玩家进入范围，距离: " + currentDistance.ToString("F2") + "米");
                Debug.Log("[DoorTransition] 按下 " + interactKey + " 或 " + alternateKey + " 键进入下一场景");
                
                // 显示提示UI
                if (showPrompt && promptUI != null)
                {
                    promptUI.SetActive(true);
                    
                    // 设置提示文本
                    if (promptText != null && !string.IsNullOrEmpty(customPromptText))
                    {
                        promptText.text = customPromptText;
                    }
                }
            }
            else
            {
                // 玩家离开范围
                Debug.Log("[DoorTransition] 玩家离开范围，距离: " + currentDistance.ToString("F2") + "米");
                
                // 隐藏提示UI
                if (showPrompt && promptUI != null)
                {
                    promptUI.SetActive(false);
                }
            }
        }
        
        // 输入调试 - 每3秒检查一次是否有输入
        if (inputDebugMode && Time.time > lastInputCheck + 3f)
        {
            lastInputCheck = Time.time;
            Debug.Log("[DoorTransition] 输入检测中... 请按任意键");
        }
        
        // 检测主要交互键
        if (Input.GetKeyDown(interactKey))
        {
            Debug.Log("[DoorTransition] 检测到按下主交互键: " + interactKey);
            if (playerInRange)
            {
                LoadNextScene();
            }
        }
        
        // 检测备用交互键
        if (Input.GetKeyDown(alternateKey))
        {
            Debug.Log("[DoorTransition] 检测到按下备用交互键: " + alternateKey);
            if (playerInRange)
            {
                LoadNextScene();
            }
        }
        
        // 检测任何按键（用于调试）
        if (inputDebugMode && Input.anyKeyDown)
        {
            Debug.Log("[DoorTransition] 检测到按键输入: Input.anyKeyDown = true");
            
            // 检测常用交互键
            if (checkAllKeys)
            {
                foreach (KeyCode key in commonKeys)
                {
                    if (Input.GetKeyDown(key))
                    {
                        Debug.Log("[DoorTransition] 检测到按下键: " + key);
                        
                        // 如果在范围内且不是主交互键或备用键（这些前面已处理）
                        if (playerInRange && key != interactKey && key != alternateKey)
                        {
                            Debug.Log("[DoorTransition] 使用备用键 " + key + " 尝试进入下一场景");
                            LoadNextScene();
                        }
                    }
                }
            }
        }
        
        // 每秒记录一次调试信息
        if (logDebugInfo && Time.frameCount % 60 == 0)
        {
            Debug.Log("[DoorTransition] 玩家距离门: " + currentDistance.ToString("F2") + 
                     "米，激活距离: " + activationDistance + "米，玩家" + 
                     (playerInRange ? "在范围内" : "不在范围内"));
        }
    }
    
    // 加载下一个场景
    private void LoadNextScene()
    {
        // 播放音效
        if (transitionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(transitionSound, soundVolume);
            Debug.Log("[DoorTransition] 播放过渡音效");
        }
        
        // 使用淡入淡出效果还是直接加载
        if (useFadeEffect)
        {
            Debug.Log("[DoorTransition] 使用淡入淡出效果加载场景");
            StartCoroutine(FadeAndLoadScene());
        }
        else
        {
            // 直接加载场景
            Debug.Log("[DoorTransition] 直接加载场景: " + nextSceneName);
            
            try
            {
                SceneManager.LoadScene(nextSceneName);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[DoorTransition] 加载场景失败: " + e.Message);
            }
        }
    }
    
    // 淡入淡出效果协程
    private System.Collections.IEnumerator FadeAndLoadScene()
    {
        // 创建一个临时的画布和图像用于淡入淡出
        GameObject fadeObj = new GameObject("FadeCanvas");
        Canvas fadeCanvas = fadeObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 999; // 确保在最上层
        
        // 添加CanvasScaler组件
        UnityEngine.UI.CanvasScaler scaler = fadeObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // 添加一个图像组件用于淡入淡出
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(fadeObj.transform, false);
        UnityEngine.UI.Image fadeImage = imageObj.AddComponent<UnityEngine.UI.Image>();
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0);
        
        // 设置图像填满屏幕
        RectTransform rectTransform = fadeImage.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        
        // 淡入
        float elapsedTime = 0;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
            yield return null;
        }
        
        // 加载新场景
        try
        {
            SceneManager.LoadScene(nextSceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[DoorTransition] 加载场景失败: " + e.Message);
        }
    }
    
    // 在编辑器中绘制激活范围
    private void OnDrawGizmos()
    {
        if (!showDebugGizmo || doorTransform == null)
            return;
        
        // 绘制激活区域
        Gizmos.color = playerInRange ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(doorTransform.position, activationDistance);
        
        // 如果有玩家引用，绘制到玩家的线
        if (playerTransform != null)
        {
            Gizmos.color = playerInRange ? Color.green : Color.red;
            Gizmos.DrawLine(doorTransform.position, playerTransform.position);
            
            // 显示距离文本
            #if UNITY_EDITOR
            if (currentDistance > 0)
            {
                Vector3 textPosition = Vector3.Lerp(doorTransform.position, playerTransform.position, 0.5f);
                UnityEditor.Handles.BeginGUI();
                Vector3 screenPos = UnityEditor.HandleUtility.WorldToGUIPoint(textPosition);
                UnityEditor.Handles.Label(textPosition, currentDistance.ToString("F2") + "m");
                UnityEditor.Handles.EndGUI();
            }
            #endif
        }
    }
} 