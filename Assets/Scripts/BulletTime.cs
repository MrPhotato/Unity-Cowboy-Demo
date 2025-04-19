using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;

public class BulletTime : MonoBehaviour
{
    [Header("节拍特效设置")]
    public GameObject beatEffectPrefab; // 节拍特效预制体
    public TextAsset beatsJsonFile; // 节拍JSON文件
    public float beatEffectDuration = 0.2f; // 特效持续时间
    public float beatThreshold = 0.1f; // 节拍检测阈值（秒）
    public bool enableBeatEffects = true; // 是否启用节拍特效
    public Transform effectSpawnPoint; // 特效生成位置（不设置则使用玩家位置）
    public float beatEffectScale = 1.0f; // 特效缩放大小
    [Tooltip("是否在子弹时间模式下增强节拍特效")]
    public bool enhanceBeatEffectsInBulletTime = true; // 子弹时间下增强特效
    [Tooltip("是否在子弹时间模式下修改特效颜色")]
    public bool modifyEffectColorInBulletTime = false; // 是否修改特效颜色
    [Tooltip("子弹时间下的特效颜色")]
    public Color bulletTimeEffectColor = new Color(1f, 0.5f, 0f, 1f); // 子弹时间下的特效颜色
    [Tooltip("完美节拍判定时间（毫秒）")]
    public float perfectBeatThreshold = 100f; // 完美节拍判定时间（毫秒）
    [Tooltip("连续MISS多少次后特效变红")]
    public int missCountToRed = 10; // 连续MISS多少次后特效变红
    [Tooltip("系统延迟补偿（毫秒）")]
    public float systemDelay = 50f; // 系统延迟补偿（毫秒）

    private List<float> beats = new List<float>(); // 存储节拍时间点
    private float lastMusicTime = 0f; // 上一帧的音乐时间
    private float nextBeatIndex = 0; // 下一个要触发的节拍索引
    private bool beatDataLoaded = false; // 是否已加载节拍数据
    private AudioSource musicSource; // 当前正在播放的音乐AudioSource
    private float lastBeatTime = 0f; // 最近的节拍时间
    private int currentMissCount = 0; // 当前连续MISS次数
    private bool isEffectGreen = false; // 特效当前是否为绿色

    [Header("相机设置")]
    public Camera mainCamera; // 主相机
    public Camera topDownCamera; // 俯视相机
    public Transform player; // 玩家Transform

    [Header("俯视相机设置")]
    public float topDownHeight = 6.0f; // 俯视相机在玩家上方的高度
    public float topDownSwitchTime = 10.0f; // 俯视镜头持续时间
    public float topDownFOV = 75f; // 俯视视野角度
    [Range(60f, 120f)]
    public float additionalFOV = 90f; // 额外增加的视野范围

    [Header("俯视相机轨道设置")]
    public bool rotateTopDownCamera = true; // 是否启用俯视相机旋转
    public float rotationSpeed = 20f; // 旋转速度
    public float orbitRadius = 3.0f; // 相机绕玩家旋转的半径
    private float currentRotationAngle = 0f; // 当前旋转角度

    [Header("玩家聚光灯设置")]
    public bool enablePlayerSpotlight = true; // 是否启用玩家头顶聚光灯
    public Color spotlightColor = new Color(1f, 1f, 1f, 0.8f); // 聚光灯颜色
    public float spotlightIntensity = 2.5f; // 聚光灯强度
    public float spotlightRange = 10f; // 聚光灯范围
    public float spotlightAngle = 30f; // 聚光灯角度
    public float spotlightHeight = 10.0f; // 聚光灯在玩家上方的高度（默认高度）
    public float spotlightTransitionSpeed = 2.0f; // 聚光灯高度变化的速度
    private Light playerSpotlight; // 玩家头顶聚光灯引用

    [Header("子弹时间设置")]
    public float bulletTimeScale = 0.2f; // 子弹时间的时间缩放(1/5)
    public bool affectPlayer = false; // 是否影响玩家速度
    public LayerMask enemyLayer; // 敌人所在的层
    public string enemyTag = "Enemy"; // 敌人标签，默认为"Enemy"
    public float autoAimRadius = 20f; // 自动索敌半径
    public bool enableAutoAim = true; // 是否启用自动索敌
    public AudioClip bulletTimeActivationSound; // 进入子弹时间的提示音
    [Range(0.1f, 2.0f)]
    public float bulletTimeSoundVolume = 1.0f; // 提示音音量
    public bool enableBulletTimeGunSound = false; // 是否在子弹时间内播放枪声

    [Header("子弹时间攻击设置")]
    public float bulletTimeDamageMultiplier = 2.0f; // 子弹时间下的伤害倍率
    public float bulletTimeFireCooldown = 0.1f; // 子弹时间下的射击冷却时间
    public bool autoFireInBulletTime = false; // 是否在子弹时间内自动射击
    public float autoFireRate = 0.5f; // 自动射击的间隔时间
    private float lastBulletTimeFireTime = 0f; // 上次子弹时间射击的时间

    [Header("动画设置")]
    public float shootingAnimDuration = 0.3f; // 射击动画持续时间
    private Animator playerAnimator; // 玩家的动画控制器
    private Coroutine resetShootingCoroutine; // 重置射击动画的协程

    private bool isTopDownActive = false;
    private bool isBulletTimeActive = false;
    private Coroutine autoSwitchCoroutine;
    
    // 保存原始速度的字典
    private Dictionary<object, float> originalSpeeds = new Dictionary<object, float>();
    private MyCowboy.Demo.BasicMotionsCharacterController playerController;
    private Transform currentTarget;
    private AudioSource bulletTimeAudioSource; // 音频源组件

    private List<GameObject> activeEffects = new List<GameObject>(); // 存储当前活跃的特效
    private bool shouldGenerateEffects = false; // 控制是否生成特效

    private float lastInputTime = 0f; // 上次输入时间
    private float inputBufferTime = 0.016f; // 输入缓冲时间（约60fps）
    private bool inputBuffer = false; // 输入缓冲标志

    private float lastBeatCheckTime = 0f; // 上次检查节拍的时间
    private bool isWaitingForInput = false; // 是否正在等待玩家输入
    private float currentBeatWindow = 0f; // 当前节拍窗口时间
    private float beatWindowDuration = 0.2f; // 节拍判定窗口持续时间
    private bool hasProcessedCurrentBeat = false; // 是否已处理当前节拍

    [Header("能量条设置")]
    public bool showEnergyBar = true; // 是否显示能量条
    public float energyBarHeight = 5f; // 能量条高度
    public float energyBarWidth = 50f; // 能量条宽度
    public float energyBarVerticalOffset = 2.5f; // 能量条在玩家头顶的偏移
    public Color energyBarColor = new Color(0.5f, 0f, 0.5f, 1f); // 紫色能量条
    private int currentEnergy = 0; // 当前能量值
    private const int maxEnergy = 10; // 最大能量值

    private float originalSpotlightHeight; // 保存原始聚光灯高度
    private bool isHighlightActive = false; // 是否处于highlight状态
    private Coroutine highlightCoroutine; // highlight效果的协程

    [Header("敌人聚光灯设置")]
    public bool enableEnemySpotlight = true; // 是否启用敌人头顶聚光灯
    public Color enemySpotlightColor = new Color(1f, 1f, 1f, 0.8f); // 敌人聚光灯颜色
    public float enemySpotlightIntensity = 100f; // 敌人聚光灯强度
    public float enemySpotlightRange = 100f; // 敌人聚光灯范围
    public float enemySpotlightAngle = 60f; // 敌人聚光灯角度
    public float enemySpotlightHeight = 8f; // 敌人聚光灯高度
    public float enemySpotlightTransitionSpeed = 2.0f; // 敌人聚光灯高度变化速度
    private Dictionary<GameObject, Light> enemySpotlights = new Dictionary<GameObject, Light>(); // 存储敌人及其聚光灯的映射

    private void Start()
    {
        // 隐藏鼠标光标并锁定到游戏窗口中心
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // 禁用输入法
        Input.imeCompositionMode = IMECompositionMode.Off;

        // 初始化相机
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // 确保相机存在
        if (mainCamera == null)
        {
            Debug.LogError("未找到主相机！请手动设置主相机");
            enabled = false;
            return;
        }

        // 如果俯视相机未指定，创建一个
        if (topDownCamera == null)
        {
            GameObject topDownCameraObj = new GameObject("TopDownCamera");
            topDownCamera = topDownCameraObj.AddComponent<Camera>();
            
            // 复制主相机的设置
            topDownCamera.clearFlags = mainCamera.clearFlags;
            topDownCamera.backgroundColor = mainCamera.backgroundColor;
            topDownCamera.cullingMask = mainCamera.cullingMask;
            topDownCamera.depth = mainCamera.depth;
            topDownCamera.renderingPath = mainCamera.renderingPath;
            
            // 设置俯视相机的视野
            topDownCamera.fieldOfView = topDownFOV;
            
            // 固定俯视角度
            topDownCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
            
            Debug.Log("已创建俯视相机");
        }

        // 确保玩家被正确分配
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log("已自动找到玩家: " + player.name);
                
                // 尝试获取玩家控制器
                playerController = player.GetComponent<MyCowboy.Demo.BasicMotionsCharacterController>();
                if (playerController == null)
                {
                    Debug.LogWarning("未找到BasicMotionsCharacterController组件，自动索敌功能可能受限");
                }
                
                // 获取玩家的Animator组件
                playerAnimator = player.GetComponent<Animator>();
                if (playerAnimator == null)
                {
                    // 尝试在子物体中查找
                    playerAnimator = player.GetComponentInChildren<Animator>();
                    if (playerAnimator == null)
                    {
                        Debug.LogWarning("未找到玩家的Animator组件，射击动画将不会播放");
                    }
                    else
                    {
                        Debug.Log("在玩家子物体中找到Animator组件");
                    }
                }
            }
            else
            {
                Debug.LogError("未找到Player标签的对象！请手动设置玩家Transform");
                enabled = false;
                return;
            }
        }
        else
        {
            // 如果已经设置了player，尝试获取控制器和动画器
            playerController = player.GetComponent<MyCowboy.Demo.BasicMotionsCharacterController>();
            playerAnimator = player.GetComponent<Animator>();
            if (playerAnimator == null)
            {
                playerAnimator = player.GetComponentInChildren<Animator>();
            }
        }

        // 初始化相机状态
        mainCamera.enabled = true;
        topDownCamera.enabled = false;
        isTopDownActive = false;
        
        // 设置俯视相机的初始位置
        UpdateTopDownCameraPosition();

        // 确保有音频源播放提示音
        bulletTimeAudioSource = GetComponent<AudioSource>();
        if (bulletTimeAudioSource == null && bulletTimeActivationSound != null)
        {
            bulletTimeAudioSource = gameObject.AddComponent<AudioSource>();
            bulletTimeAudioSource.playOnAwake = false;
            bulletTimeAudioSource.spatialBlend = 0f; // 设为2D音效
            Debug.Log("已自动添加AudioSource组件用于播放子弹时间提示音");
        }
        
        // 创建玩家头顶聚光灯
        if (enablePlayerSpotlight && player != null)
        {
            CreatePlayerSpotlight();
        }

        // 加载节拍数据
        LoadBeatData();
        
        // 尝试找到PersistentAudioManager中的主音轨
        FindMusicSource();

        // 为所有敌人创建聚光灯
        if (enableEnemySpotlight)
        {
            CreateEnemySpotlights();
        }
    }
    
    // 创建玩家头顶聚光灯
    private void CreatePlayerSpotlight()
    {
        // 检查是否已经有聚光灯
        if (playerSpotlight != null)
            return;
            
        // 创建聚光灯游戏对象
        GameObject spotlightObj = new GameObject("PlayerSpotlight");
        playerSpotlight = spotlightObj.AddComponent<Light>();
        
        // 设置聚光灯属性
        playerSpotlight.type = LightType.Spot;
        playerSpotlight.color = spotlightColor;
        playerSpotlight.intensity = spotlightIntensity;
        playerSpotlight.range = spotlightRange;
        playerSpotlight.spotAngle = spotlightAngle;
        playerSpotlight.shadows = LightShadows.Soft; // 启用软阴影
        
        // 将聚光灯放置在玩家头顶
        UpdateSpotlightPosition();
        
        Debug.Log("已创建玩家头顶聚光灯");
    }
    
    // 更新聚光灯位置
    private void UpdateSpotlightPosition()
    {
        if (playerSpotlight != null && player != null)
        {
            // 获取玩家高度
            float playerHeight = 0;
            Collider playerCollider = player.GetComponent<Collider>();
            if (playerCollider != null)
            {
                playerHeight = playerCollider.bounds.size.y;
            }
            else
            {
                // 如果没有碰撞体，使用估计值
                playerHeight = 1.8f;
            }
            
            // 设置聚光灯位置在玩家上方
            playerSpotlight.transform.position = player.position + Vector3.up * (playerHeight + spotlightHeight);
            
            // 聚光灯始终朝下
            playerSpotlight.transform.rotation = Quaternion.Euler(90, 0, 0);
        }
    }

    private void FixedUpdate()
    {
        // 检测R键输入
        if (Input.GetKey(KeyCode.R))
        {
            float currentTime = Time.time;
            if (currentTime - lastInputTime >= inputBufferTime)
            {
                inputBuffer = true;
                lastInputTime = currentTime;
            }
        }
        else
        {
            inputBuffer = false;
        }
    }

    private void Update()
    {
        // 使用缓冲的输入状态
        if (inputBuffer)
        {
            if (!isBulletTimeActive)
            {
                ActivateBulletTime();
            }
            else
            {
                // 如果正在等待输入且玩家按下了按键
                if (isWaitingForInput && !hasProcessedCurrentBeat)
                {
                    float currentMusicTime = musicSource != null ? musicSource.time : 0f;
                    float timeDiff = Mathf.Abs(currentMusicTime - currentBeatWindow) * 1000f; // 转换为毫秒
                    
                    // 检查是否在判定窗口内
                    if (timeDiff <= perfectBeatThreshold)
                    {
                        // 完美节拍
                        currentMissCount = 0;
                        
                        // 增加能量值
                        if (currentEnergy < maxEnergy)
                        {
                            currentEnergy++;
                            Debug.Log($"能量增加！当前能量：{currentEnergy}/{maxEnergy}");
                        }
                        
                        if (!isEffectGreen)
                        {
                            ChangeAllEffectsToGreen();
                            isEffectGreen = true;
                        }
                        Debug.Log($"完美节拍！时间差: {timeDiff:F1}ms");
                    }
                    else
                    {
                        // MISS
                        currentMissCount++;
                        Debug.Log($"MISS！时间差: {timeDiff:F1}ms，连续MISS次数：{currentMissCount}");
                        
                        // 只有当连续MISS次数达到阈值时才变红
                        if (currentMissCount >= missCountToRed && isEffectGreen)
                        {
                            ChangeAllEffectsToRed();
                            isEffectGreen = false;
                            Debug.Log($"连续MISS {missCountToRed} 次，特效变为红色！");
                        }
                    }
                    
                    hasProcessedCurrentBeat = true;
                    
                    // 播放射击效果
                    if (enableBulletTimeGunSound && playerController != null && playerController.shootAudioSource != null && playerController.shootSound != null)
                    {
                        playerController.shootAudioSource.PlayOneShot(playerController.shootSound);
                    }
                    
                    if (playerController != null && playerController.muzzleFlashPrefab != null && playerController.firePoint != null)
                    {
                        GameObject muzzleFlash = Instantiate(playerController.muzzleFlashPrefab, 
                                                            playerController.firePoint.position, 
                                                            playerController.firePoint.rotation);
                        Destroy(muzzleFlash, 0.1f);
                    }
                    
                    TriggerShootingAnimation();
                    
                    // 处理伤害
                    if (currentTarget == null)
                    {
                        currentTarget = FindNearestEnemy();
                    }
                    
                    if (currentTarget != null)
                    {
                        RobotEnemy enemy = currentTarget.GetComponent<RobotEnemy>();
                        if (enemy != null)
                        {
                            int damage = Mathf.RoundToInt(bulletTimeDamageMultiplier);
                            enemy.TakeDamage(damage);
                            Debug.Log($"子弹时间攻击直接对 {currentTarget.name} 造成 {damage} 点伤害!");
                            
                            CreateBulletEffect(playerController.firePoint.position, currentTarget.position);
                        }
                    }
                }
            }
        }
        
        // 检查节拍
        if (isBulletTimeActive && musicSource != null && musicSource.isPlaying)
        {
            float currentMusicTime = musicSource.time;
            
            // 如果当前不在等待输入状态，检查是否到达新的节拍点
            if (!isWaitingForInput)
            {
                float nearestBeatTime = FindNearestBeatTime(currentMusicTime);
                float nextUpcomingBeat = GetNextUpcomingBeat(currentMusicTime);
                
                // 检查是否接近节拍点
                if (nearestBeatTime >= 0)
                {
                    float timeToBeat = Mathf.Abs(currentMusicTime - nearestBeatTime);
                    if (timeToBeat <= beatWindowDuration)
                    {
                        // 进入节拍判定窗口
                        isWaitingForInput = true;
                        currentBeatWindow = nearestBeatTime;
                        hasProcessedCurrentBeat = false;
                        Debug.Log($"进入节拍判定窗口，等待玩家输入");
                    }
                }
                else if (nextUpcomingBeat >= 0)
                {
                    float timeToBeat = nextUpcomingBeat - currentMusicTime;
                    if (timeToBeat <= beatWindowDuration)
                    {
                        // 进入节拍判定窗口
                        isWaitingForInput = true;
                        currentBeatWindow = nextUpcomingBeat;
                        hasProcessedCurrentBeat = false;
                        Debug.Log($"进入节拍判定窗口，等待玩家输入");
                    }
                }
            }
            else
            {
                // 如果正在等待输入，检查是否超出判定窗口
                float timeDiff = Mathf.Abs(currentMusicTime - currentBeatWindow);
                if (timeDiff > beatWindowDuration)
                {
                    // 只有当玩家没有处理这个节拍时才记录MISS
                    if (!hasProcessedCurrentBeat)
                    {
                        // 超出判定窗口，记录MISS
                        currentMissCount++;
                        Debug.Log($"超出判定窗口，MISS！连续MISS次数：{currentMissCount}");
                        
                        // 只有当连续MISS次数达到阈值时才变红
                        if (currentMissCount >= missCountToRed && isEffectGreen)
                        {
                            ChangeAllEffectsToRed();
                            isEffectGreen = false;
                            Debug.Log($"连续MISS {missCountToRed} 次，特效变为红色！");
                        }
                    }
                    
                    isWaitingForInput = false;
                    hasProcessedCurrentBeat = false;
                }
            }
        }
        
        // 按ESC键退出子弹时间
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 如果正在子弹时间，先退出子弹时间
            if (isBulletTimeActive)
            {
                DeactivateBulletTime();
            }
            
            // 加载Demo场景
            SceneManager.LoadScene("Demo");
        }

        // 如果俯视相机处于激活状态，更新其位置和旋转
        if (isTopDownActive && topDownCamera != null && player != null)
        {
            UpdateTopDownCameraPosition();
            
            // 如果启用了旋转，旋转俯视相机
            if (rotateTopDownCamera)
            {
                RotateTopDownCamera();
            }
        }
        
        // 如果子弹时间活跃且启用了自动索敌，更新自动索敌
        if (isBulletTimeActive && enableAutoAim && player != null)
        {
            UpdateAutoAim();
        }
        
        // 更新玩家聚光灯位置
        if (enablePlayerSpotlight && playerSpotlight != null && player != null)
        {
            UpdateSpotlightPosition();
        }

        // 检测节拍并播放特效
        if (enableBeatEffects && beatDataLoaded && musicSource != null && musicSource.isPlaying)
        {
            CheckAndTriggerBeatEffects();
        }

        // 更新所有敌人的聚光灯位置
        if (enableEnemySpotlight)
        {
            UpdateAllEnemySpotlights();
        }
    }

    // 更新俯视相机位置
    private void UpdateTopDownCameraPosition()
    {
        if (player != null && topDownCamera != null)
        {
            if (!rotateTopDownCamera)
            {
                // 计算俯视位置 - 直接在玩家上方
                Vector3 topDownPosition = player.position + Vector3.up * topDownHeight;
                
                // 设置相机位置
                topDownCamera.transform.position = topDownPosition;
                
                // 让相机朝向玩家
                topDownCamera.transform.LookAt(player);
            }
            else
            {
                // 如果启用了旋转，位置将在RotateTopDownCamera中更新
                // 这里只更新FOV
            }
            
            // 实时更新相机FOV
            topDownCamera.fieldOfView = additionalFOV;
        }
    }
    
    // 旋转俯视相机
    private void RotateTopDownCamera()
    {
        // 更新旋转角度
        currentRotationAngle += rotationSpeed * Time.deltaTime;
        if (currentRotationAngle >= 360f)
            currentRotationAngle -= 360f;
        
        // 计算相机在圆形轨道上的位置
        float x = player.position.x + Mathf.Cos(currentRotationAngle * Mathf.Deg2Rad) * orbitRadius;
        float z = player.position.z + Mathf.Sin(currentRotationAngle * Mathf.Deg2Rad) * orbitRadius;
        float y = player.position.y + topDownHeight;
        
        // 设置相机位置
        topDownCamera.transform.position = new Vector3(x, y, z);
        
        // 让相机始终看向玩家
        topDownCamera.transform.LookAt(player);
    }

    // 更新自动索敌 - 找到最近的敌人并面向它
    private void UpdateAutoAim()
    {
        // 查找最近的敌人
        Transform nearestEnemy = FindNearestEnemy();
        
        // 如果找到敌人且有玩家控制器
        if (nearestEnemy != null && playerController != null)
        {
            currentTarget = nearestEnemy;
            
            // 计算朝向敌人的方向（只考虑水平方向）
            Vector3 targetDirection = nearestEnemy.position - player.position;
            targetDirection.y = 0;
            
            // 如果方向向量有效
            if (targetDirection.magnitude > 0.1f)
            {
                // 计算目标旋转
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                
                // 平滑旋转玩家朝向敌人
                player.rotation = Quaternion.Slerp(player.rotation, targetRotation, Time.deltaTime * 5f);
                
                Debug.DrawLine(player.position, nearestEnemy.position, Color.red);
            }
        }
    }

    // 查找最近的敌人
    private Transform FindNearestEnemy()
    {
        Transform nearest = null;
        float minDistance = autoAimRadius;
        
        // 使用标签查找所有敌人
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        
        foreach (GameObject enemy in enemies)
        {
            // 计算距离
            float distance = Vector3.Distance(player.position, enemy.transform.position);
            
            // 如果这个敌人更近
            if (distance < minDistance)
            {
                // 检查敌人是否在视线内 - 射线检测
                RaycastHit hit;
                Vector3 direction = enemy.transform.position - player.position;
                if (Physics.Raycast(player.position + Vector3.up, direction.normalized, out hit, autoAimRadius))
                {
                    // 如果射线直接击中敌人
                    if (hit.transform.gameObject == enemy || hit.transform.IsChildOf(enemy.transform))
                    {
                        minDistance = distance;
                        nearest = enemy.transform;
                    }
                }
            }
        }
        
        return nearest;
    }

    // 切换子弹时间和相机视角
    public void ToggleBulletTime()
    {
        // 仅允许激活，不允许通过此方法停用
        if (!isBulletTimeActive)
        {
            ActivateBulletTime();
        }
    }
    
    // 激活子弹时间
    public void ActivateBulletTime()
    {
        // 已经处于激活状态则返回
        if (isTopDownActive || isBulletTimeActive)
            return;
            
        // 播放提示音
        PlayBulletTimeActivationSound();
            
        // 启用俯视相机
        SwitchToTopDownView();
        
        // 减慢敌人速度
        SlowDownEnemies();
        
        // 设置唯一的音频状态为"情绪高涨"
        AudioStateManager.SetSingleState("excited");
        
        // 开始生成特效
        shouldGenerateEffects = true;
        
        isBulletTimeActive = true;
        
        Debug.Log("子弹时间已激活！敌人速度降为1/5，音乐切换为'情绪高涨'状态");
    }
    
    // 播放子弹时间提示音
    private void PlayBulletTimeActivationSound()
    {
        if (bulletTimeActivationSound != null)
        {
            // 优先使用AudioSource组件播放
            if (bulletTimeAudioSource != null)
            {
                bulletTimeAudioSource.clip = bulletTimeActivationSound;
                bulletTimeAudioSource.volume = bulletTimeSoundVolume;
                bulletTimeAudioSource.Play();
            }
            else
            {
                // 如果没有AudioSource，使用静态方法在空间中播放
                AudioSource.PlayClipAtPoint(bulletTimeActivationSound, Camera.main.transform.position, bulletTimeSoundVolume);
            }
            
            Debug.Log("播放了子弹时间激活提示音");
        }
    }
    
    // 减慢敌人速度
    private void SlowDownEnemies()
    {
        // 清除之前的字典
        originalSpeeds.Clear();
        
        // 查找所有敌人
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        
        foreach (GameObject enemy in enemies)
        {
            // 尝试获取NavMeshAgent组件（通常控制敌人移动）
            UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                // 保存原始速度
                originalSpeeds[agent] = agent.speed;
                
                // 降低速度
                agent.speed *= bulletTimeScale;
                
                Debug.Log($"减慢敌人 {enemy.name} 的速度，从 {originalSpeeds[agent]} 到 {agent.speed}");
            }
            
            // 尝试获取Animator组件
            Animator animator = enemy.GetComponent<Animator>();
            if (animator != null)
            {
                // 减慢动画速度
                animator.speed = bulletTimeScale;
            }
            
            // 尝试获取RobotEnemy组件（之前我们创建的敌人脚本）
            var robotEnemy = enemy.GetComponent<RobotEnemy>();
            if (robotEnemy != null)
            {
                // 保存原始速度
                originalSpeeds[robotEnemy] = robotEnemy.moveSpeed;
                
                // 降低速度
                robotEnemy.moveSpeed *= bulletTimeScale;
            }
        }
    }
    
    // 恢复敌人速度
    private void RestoreEnemiesSpeeds()
    {
        // 恢复所有保存的速度
        foreach (var pair in originalSpeeds)
        {
            if (pair.Key is UnityEngine.AI.NavMeshAgent)
            {
                UnityEngine.AI.NavMeshAgent agent = pair.Key as UnityEngine.AI.NavMeshAgent;
                if (agent != null)
                {
                    agent.speed = pair.Value;
                }
            }
            else if (pair.Key is RobotEnemy)
            {
                RobotEnemy robotEnemy = pair.Key as RobotEnemy;
                if (robotEnemy != null)
                {
                    robotEnemy.moveSpeed = pair.Value;
                }
            }
        }
        
        // 恢复所有敌人的动画速度
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        foreach (GameObject enemy in enemies)
        {
            Animator animator = enemy.GetComponent<Animator>();
            if (animator != null)
            {
                animator.speed = 1.0f;
            }
        }
        
        // 清除字典
        originalSpeeds.Clear();
        
        Debug.Log("已恢复所有敌人的速度");
    }
    
    // 停用子弹时间
    public void DeactivateBulletTime()
    {
        if (!isTopDownActive && !isBulletTimeActive)
            return;

        // 检查是否达到满能量
        bool isFullEnergy = currentEnergy >= maxEnergy;
            
        // 重置能量值
        currentEnergy = 0;
        
        // 关闭俯视相机
        SwitchToMainView();
        
        // 恢复敌人速度
        RestoreEnemiesSpeeds();
        
        // 如果能量满了，触发特殊效果
        if (isFullEnergy && !isHighlightActive)
        {
            StartHighlightEffect();
        }
        else if (!isHighlightActive)
        {
            // 如果不是满能量且不在highlight状态，正常切换到discovered
            try
            {
                AudioStateManager.SetSingleState("discovered");
                Debug.Log("子弹时间退出，已切换到'被发现'音频状态");
            }
            catch (System.Exception e)
            {
                Debug.LogError("切换音频状态失败: " + e.Message);
            }
        }
        
        // 停止生成特效
        shouldGenerateEffects = false;
        
        // 清理所有现有特效
        ClearAllEffects();
        
        isBulletTimeActive = false;
        
        Debug.Log("子弹时间已停用！敌人速度恢复正常");
    }

    // 切换相机视角
    public void ToggleCameraView()
    {
        if (isTopDownActive)
        {
            SwitchToMainView();
        }
        else
        {
            SwitchToTopDownView();
        }
    }

    // 切换到俯视图
    public void SwitchToTopDownView()
    {
        if (isTopDownActive || mainCamera == null || topDownCamera == null || player == null)
            return;

        // 如果启用旋转，重置旋转角度和位置
        if (rotateTopDownCamera)
        {
            currentRotationAngle = 0f;
            // 计算初始位置
            float x = player.position.x + orbitRadius; // 从0度角开始
            float z = player.position.z;
            float y = player.position.y + topDownHeight;
            topDownCamera.transform.position = new Vector3(x, y, z);
            topDownCamera.transform.LookAt(player);
        }
        else
        {
            // 更新俯视相机位置
            UpdateTopDownCameraPosition();
        }
        
        // 设置俯视相机的FOV
        topDownCamera.fieldOfView = additionalFOV;

        // 停止之前可能存在的协程
        if (autoSwitchCoroutine != null)
        {
            StopCoroutine(autoSwitchCoroutine);
            autoSwitchCoroutine = null;
        }

        // 切换相机
        mainCamera.enabled = false;
        topDownCamera.enabled = true;
        isTopDownActive = true;

        // 启动自动切换回主视角的协程
        autoSwitchCoroutine = StartCoroutine(AutoSwitchBackToMain());

        Debug.Log("已切换到俯视相机，将在 " + topDownSwitchTime + " 秒后自动切回主相机");
    }

    // 切换到主视角
    public void SwitchToMainView()
    {
        if (!isTopDownActive || mainCamera == null || topDownCamera == null)
            return;

        // 切换相机
        topDownCamera.enabled = false;
        mainCamera.enabled = true;
        isTopDownActive = false;

        // 停止自动切换协程
        if (autoSwitchCoroutine != null)
        {
            StopCoroutine(autoSwitchCoroutine);
            autoSwitchCoroutine = null;
        }

        Debug.Log("已切换回主相机");
    }

    // 自动切换回主相机的协程
    private IEnumerator AutoSwitchBackToMain()
    {
        yield return new WaitForSeconds(topDownSwitchTime);
        
        if (isTopDownActive)
        {
            // 同时停用子弹时间和相机
            DeactivateBulletTime();
            
            // 额外确保音频状态正确切换
            // 直接在协程中调用音频状态切换，以防DeactivateBulletTime中的调用失败
            AudioStateManager.SetSingleState("discovered");
            
            Debug.Log("子弹时间自动结束，音频切换到'被发现'状态");
        }
        
        autoSwitchCoroutine = null;
    }
    
    // 子弹时间模式下的射击方法
    private void FireInBulletTime()
    {
        // 检查玩家控制器是否可用
        if (playerController == null)
            return;
            
        // 记录时间
        lastBulletTimeFireTime = Time.time;
        
        // 提前播放射击音效，减少延迟感
        if (playerController.shootAudioSource != null && playerController.shootSound != null)
        {
            playerController.shootAudioSource.PlayOneShot(playerController.shootSound);
        }
        
        // 提前显示枪口火花
        if (playerController.muzzleFlashPrefab != null && playerController.firePoint != null)
        {
            GameObject muzzleFlash = Instantiate(playerController.muzzleFlashPrefab, 
                                                playerController.firePoint.position, 
                                                playerController.firePoint.rotation);
            Destroy(muzzleFlash, 0.1f);
        }
        
        // 触发玩家的射击动画
        TriggerShootingAnimation();
        
        // 查找当前目标（如果没有当前目标，尝试找一个）
        if (currentTarget == null)
        {
            currentTarget = FindNearestEnemy();
            if (currentTarget == null)
                return; // 没有可射击的目标
        }
        
        // 直接获取敌人组件并造成伤害
        RobotEnemy enemy = currentTarget.GetComponent<RobotEnemy>();
        if (enemy != null)
        {
            // 造成伤害（默认2点，可通过bulletTimeDamageMultiplier调整）
            int damage = Mathf.RoundToInt(bulletTimeDamageMultiplier);
            enemy.TakeDamage(damage);
            Debug.Log($"子弹时间攻击直接对 {currentTarget.name} 造成 {damage} 点伤害!");
            
            // 创建视觉子弹效果
            CreateBulletEffect(playerController.firePoint.position, currentTarget.position);
        }
        else
        {
            Debug.LogWarning($"找到了目标 {currentTarget.name}，但它没有RobotEnemy组件!");
        }
    }
    
    // 触发射击动画
    private void TriggerShootingAnimation()
    {
        if (playerAnimator == null)
            return;
            
        // 停止之前可能存在的重置协程
        if (resetShootingCoroutine != null)
        {
            StopCoroutine(resetShootingCoroutine);
        }
        
        // 设置Shooting为true触发动画
        playerAnimator.SetBool("Shooting", true);
        
        // 启动新协程，延迟重置Shooting状态
        resetShootingCoroutine = StartCoroutine(ResetShootingAnimation());
        
        Debug.Log("触发了玩家射击动画");
    }
    
    // 重置射击动画状态的协程
    private IEnumerator ResetShootingAnimation()
    {
        // 等待指定的动画持续时间
        yield return new WaitForSeconds(shootingAnimDuration);
        
        // 重置Shooting状态
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("Shooting", false);
        }
        
        resetShootingCoroutine = null;
    }
    
    // 创建子弹视觉效果
    private void CreateBulletEffect(Vector3 startPos, Vector3 targetPos)
    {
        // 确保有firePoint和子弹预制体
        if (playerController == null || playerController.firePoint == null || playerController.bulletPrefab == null)
            return;
            
        // 创建子弹并设置属性
        GameObject bullet = Instantiate(playerController.bulletPrefab, startPos, Quaternion.identity);
        
        // 计算射击方向
        Vector3 direction = (targetPos - startPos).normalized;
        
        // 绘制调试线
        Debug.DrawRay(startPos, direction * 100f, Color.green, 2f);
            
        // 禁用子弹上的所有碰撞体，避免自碰撞
        Collider[] bulletColliders = bullet.GetComponentsInChildren<Collider>();
        foreach (Collider col in bulletColliders)
        {
            col.enabled = false;
        }
        
        // 设置子弹物理
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 禁用重力
            rb.useGravity = false;
            
            // 计算射击距离
            float distance = Vector3.Distance(targetPos, startPos);
            
            // 根据距离计算速度，确保子弹快速到达
            float speed = Mathf.Max(playerController.bulletForce, distance * 2f);
            rb.velocity = direction * speed;
            
            // 设置子弹朝向
            bullet.transform.forward = direction;
        }
        
        // 1秒后销毁子弹
        Destroy(bullet, 1f);
        
        // 在目标位置创建击中效果
        CreateHitEffect(targetPos);
    }
    
    // 创建击中效果
    private void CreateHitEffect(Vector3 position)
    {
        // 创建特效父物体
        GameObject hitEffect = new GameObject("HitEffect");
        hitEffect.transform.position = position;
        
        // 添加一个明显的视觉指示器
        GameObject visualIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visualIndicator.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
        visualIndicator.transform.position = position;
        visualIndicator.transform.parent = hitEffect.transform;
        
        // 创建一个材质并设置绿色
        Renderer renderer = visualIndicator.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.green; // 子弹时间用绿色区分
        }
        
        // 2秒后销毁击中特效
        Destroy(hitEffect, 2f);
    }
    
    // 在Unity编辑器中显示Gizmos
    private void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            // 绘制俯视相机位置
            Gizmos.color = Color.blue;
            Vector3 topDownPos = player.position + Vector3.up * topDownHeight;
            Gizmos.DrawWireSphere(topDownPos, 0.5f);
            
            // 绘制连接线
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(player.position, topDownPos);
            
            // 绘制自动索敌范围
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(player.position, autoAimRadius);
            
            // 绘制相机轨道
            Gizmos.color = Color.cyan;
            DrawCircle(player.position + Vector3.up * topDownHeight, orbitRadius, 32);
        }
    }
    
    // 辅助方法：在Gizmos中绘制圆形
    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        // 防止除以零错误
        if (segments < 3) segments = 3;
        
        float angleStep = 360f / segments;
        Vector3 previousPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }

    // 加载节拍数据
    private void LoadBeatData()
    {
        if (beatsJsonFile == null)
        {
            Debug.LogWarning("未设置节拍JSON文件，节拍特效将不会工作");
            return;
        }
        
        try
        {
            string jsonContent = beatsJsonFile.text;
            
            // 因为JsonUtility不支持顶级数组，我们需要手动解析或使用其他方法
            // 这里我们封装成一个简单的辅助类进行解析
            string wrappedJson = "{ \"beatDataWrapper\":" + jsonContent + "}";
            BeatDataWrapper wrapper = JsonUtility.FromJson<BeatDataWrapper>(wrappedJson);
            
            if (wrapper != null && wrapper.beatDataWrapper.beats != null && wrapper.beatDataWrapper.beats.Length > 0)
            {
                beats = new List<float>(wrapper.beatDataWrapper.beats);
                beatDataLoaded = true;
                Debug.Log($"成功加载节拍数据，共 {beats.Count} 个节拍点");
            }
            else
            {
                Debug.LogError("节拍数据解析失败，请检查JSON格式");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"节拍数据解析错误: {e.Message}");
            
            // 尝试替代解析方法
            try
            {
                string jsonContent = beatsJsonFile.text;
                
                // 手动解析beats数组
                int beatsStart = jsonContent.IndexOf("\"beats\": [") + 10;
                int beatsEnd = jsonContent.IndexOf("]", beatsStart);
                string beatsData = jsonContent.Substring(beatsStart, beatsEnd - beatsStart);
                
                string[] beatValues = beatsData.Split(',');
                beats.Clear();
                
                foreach (string beatValue in beatValues)
                {
                    if (float.TryParse(beatValue.Trim(), out float beat))
                    {
                        beats.Add(beat);
                    }
                }
                
                if (beats.Count > 0)
                {
                    beatDataLoaded = true;
                    Debug.Log($"使用替代方法成功加载节拍数据，共 {beats.Count} 个节拍点");
                }
                else
                {
                    Debug.LogError("替代解析方法失败，无法解析节拍数据");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"替代解析方法错误: {ex.Message}");
            }
        }
    }
    
    // 查找正在播放的音乐AudioSource
    private void FindMusicSource()
    {
        // 尝试获取PersistentAudioManager的第一个AudioSource
        var persistentAudioManager = FindObjectOfType<PersistentAudioManager>();
        if (persistentAudioManager != null)
        {
            AudioSource[] sources = persistentAudioManager.GetAudioSources();
            if (sources != null && sources.Length > 0)
            {
                musicSource = sources[0]; // 使用第一个音轨作为主音轨
                Debug.Log("已找到PersistentAudioManager的音频源");
            }
        }
        
        // 如果没找到，尝试查找场景中有"Music"标签的AudioSource
        if (musicSource == null)
        {
            GameObject musicObj = GameObject.FindWithTag("Music");
            if (musicObj != null)
            {
                musicSource = musicObj.GetComponent<AudioSource>();
                Debug.Log("已找到带Music标签的音频源");
            }
        }
        
        // 如果还是没找到，尝试找所有AudioSource
        if (musicSource == null)
        {
            AudioSource[] allSources = FindObjectsOfType<AudioSource>();
            foreach (AudioSource source in allSources)
            {
                if (source.isPlaying && source.clip != null && source.time > 0)
                {
                    musicSource = source;
                    Debug.Log($"已找到正在播放的音频源: {source.gameObject.name}");
                    break;
                }
            }
        }
        
        if (musicSource == null)
        {
            Debug.LogWarning("无法找到音乐AudioSource，节拍检测将不会工作");
        }
    }

    // 检测节拍并触发特效
    private void CheckAndTriggerBeatEffects()
    {
        if (!shouldGenerateEffects || musicSource == null || beats.Count == 0)
            return;

        float currentMusicTime = musicSource.time;
        
        // 处理音乐循环的情况
        if (currentMusicTime < lastMusicTime - 0.5f) // 允许小幅度回退，大幅度回退认为是循环
        {
            nextBeatIndex = 0; // 音乐重新开始，重置节拍索引
            Debug.Log("音乐已循环，重置节拍检测");
        }
        
        // 如果nextBeatIndex越界，重置
        if (nextBeatIndex >= beats.Count)
        {
            nextBeatIndex = 0;
        }
        
        // 查找正确的起始节拍索引（如果当前音乐时间已经超过了某些节拍）
        while (nextBeatIndex < beats.Count && currentMusicTime > beats[(int)nextBeatIndex] + beatThreshold)
        {
            nextBeatIndex++;
        }
        
        // 查找下一个要触发的节拍
        int triggeredCount = 0; // 防止一次触发过多特效
        int startIndex = (int)nextBeatIndex;
        
        for (int i = startIndex; i < beats.Count && triggeredCount < 3 && beats[i] <= currentMusicTime + beatThreshold; i++)
        {
            // 检查是否在阈值范围内
            float beatTime = beats[i];
            if (Mathf.Abs(currentMusicTime - beatTime) <= beatThreshold)
            {
                // 更新最近的节拍时间
                lastBeatTime = beatTime;
                
                // 触发特效
                SpawnBeatEffect();
                
                // 可以在这里添加其他节拍事件的触发
                
                // 标记这个节拍已触发
                Debug.Log($"触发节拍特效：第 {i} 个节拍，时间={beatTime:F2}s，当前音乐时间={currentMusicTime:F2}s");
                
                // 更新下一个要检查的节拍索引
                nextBeatIndex = i + 1;
                
                // 限制每帧触发的特效数量
                triggeredCount++;
                
                // 如果特效触发了，就不再检查这附近的其他节拍点（防止重复触发）
                break;
            }
        }
        
        lastMusicTime = currentMusicTime;
    }
    
    // 清理所有特效
    private void ClearAllEffects()
    {
        foreach (GameObject effect in activeEffects)
        {
            if (effect != null)
            {
                Destroy(effect);
            }
        }
        activeEffects.Clear();
    }

    // 生成节拍特效
    private void SpawnBeatEffect()
    {
        if (!shouldGenerateEffects || beatEffectPrefab == null)
        {
            return;
        }
        
        // 确定特效生成位置
        Vector3 effectPosition;
        if (effectSpawnPoint != null)
        {
            effectPosition = effectSpawnPoint.position;
        }
        else if (player != null)
        {
            effectPosition = player.position + Vector3.up * 1.0f;
        }
        else
        {
            Debug.LogWarning("无法确定特效生成位置，使用(0,0,0)");
            effectPosition = Vector3.zero;
        }
        
        // 创建特效实例
        GameObject effectInstance = Instantiate(beatEffectPrefab, effectPosition, Quaternion.identity);
        
        // 确保特效是激活的
        effectInstance.SetActive(true);
        
        // 根据当前状态设置特效颜色
        Color currentColor = isEffectGreen ? Color.green : Color.red;
        
        // 获取并设置所有粒子系统的颜色
        ParticleSystem[] particleSystems = effectInstance.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem ps in particleSystems)
        {
            ps.gameObject.SetActive(true);
            
            // 设置主颜色
            var main = ps.main;
            main.startColor = currentColor;
            
            // 设置颜色渐变
            var colorOverLifetime = ps.colorOverLifetime;
            if (colorOverLifetime.enabled)
            {
                var gradient = new Gradient();
                var colorKey = new GradientColorKey[2];
                var alphaKey = new GradientAlphaKey[2];
                
                colorKey[0].color = currentColor;
                colorKey[0].time = 0.0f;
                colorKey[1].color = currentColor;
                colorKey[1].time = 1.0f;
                
                alphaKey[0].alpha = 1.0f;
                alphaKey[0].time = 0.0f;
                alphaKey[1].alpha = 0.0f;
                alphaKey[1].time = 1.0f;
                
                gradient.SetKeys(colorKey, alphaKey);
                colorOverLifetime.color = gradient;
            }
            
            // 设置速度颜色
            var colorBySpeed = ps.colorBySpeed;
            if (colorBySpeed.enabled)
            {
                var gradient = new Gradient();
                var colorKey = new GradientColorKey[2];
                var alphaKey = new GradientAlphaKey[2];
                
                colorKey[0].color = currentColor;
                colorKey[0].time = 0.0f;
                colorKey[1].color = currentColor;
                colorKey[1].time = 1.0f;
                
                alphaKey[0].alpha = 1.0f;
                alphaKey[0].time = 0.0f;
                alphaKey[1].alpha = 0.0f;
                alphaKey[1].time = 1.0f;
                
                gradient.SetKeys(colorKey, alphaKey);
                colorBySpeed.color = gradient;
            }
            
            // 启动粒子系统
            if (!ps.isPlaying)
            {
                ps.Play(true);
            }
        }
        
        // 设置渲染器颜色
        Renderer[] renderers = effectInstance.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.materials;
            foreach (Material material in materials)
            {
                material.color = currentColor;
            }
        }
        
        // 是否在子弹时间模式下增强特效
        if (isBulletTimeActive && enhanceBeatEffectsInBulletTime)
        {
            effectInstance.transform.localScale *= 1.5f;
        }
        
        // 设置特效大小
        effectInstance.transform.localScale *= beatEffectScale;
        
        // 添加到活跃特效列表
        activeEffects.Add(effectInstance);
        
        // 记录当前特效颜色状态
        string colorStatus = isEffectGreen ? "<color=green>绿色</color>" : "<color=red>红色</color>";
        Debug.Log($"生成新节拍特效 - 当前颜色：{colorStatus}，连续MISS次数：{currentMissCount}");
        
        // 设置特效的存活时间并在销毁时从列表中移除
        StartCoroutine(DestroyEffectAfterDelay(effectInstance, beatEffectDuration));
    }

    // 延迟销毁特效的协程
    private IEnumerator DestroyEffectAfterDelay(GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (effect != null)
        {
            activeEffects.Remove(effect);
            Destroy(effect);
        }
    }

    // 节拍数据结构
    [System.Serializable]
    private class BeatData
    {
        public string file_name;
        public float tempo;
        public float[] beats;
        public int total_beats;
        public float sensitivity_setting;
    }
    
    // 包装类，用于JsonUtility解析
    [System.Serializable]
    private class BeatDataWrapper
    {
        public BeatData beatDataWrapper;
    }

    // 用于测试和调试的方法
#if UNITY_EDITOR
    // 在编辑器中手动触发特效（用于测试）
    public void TestBeatEffect()
    {
        if (beatEffectPrefab == null)
        {
            Debug.LogError("未设置beatEffectPrefab，无法测试");
            return;
        }
        
        SpawnBeatEffect();
        Debug.Log("手动触发节拍特效");
    }
    
    // 在编辑器中重新加载节拍数据
    public void ReloadBeatData()
    {
        beats.Clear();
        beatDataLoaded = false;
        nextBeatIndex = 0;
        LoadBeatData();
        
        // 尝试重新查找音频源
        FindMusicSource();
    }
    
    // 在编辑器中打印当前音乐时间和下一个节拍点
    public void PrintMusicTimeInfo()
    {
        if (musicSource == null)
        {
            Debug.LogError("未找到音乐源，无法获取时间信息");
            return;
        }
        
        float currentTime = musicSource.time;
        string beatInfo = "无下一节拍";
        
        if (beats.Count > 0 && nextBeatIndex < beats.Count)
        {
            float nextBeat = beats[(int)nextBeatIndex];
            float timeDiff = nextBeat - currentTime;
            beatInfo = $"下一节拍: {nextBeatIndex}/{beats.Count}, 时间: {nextBeat:F2}s, 还有: {timeDiff:F2}s";
        }
        
        Debug.Log($"当前音乐时间: {currentTime:F2}s, {beatInfo}");
    }
#endif

    // 将所有特效改为绿色
    private void ChangeAllEffectsToGreen()
    {
        // 确保当前不是红色状态
        if (!isEffectGreen)
        {
            foreach (GameObject effect in activeEffects)
            {
                if (effect != null)
                {
                    // 获取所有粒子系统组件
                    ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>(true);
                    foreach (ParticleSystem ps in particleSystems)
                    {
                        // 修改主模块的颜色
                        var main = ps.main;
                        main.startColor = Color.green;
                        
                        // 修改颜色渐变模块
                        var colorOverLifetime = ps.colorOverLifetime;
                        if (colorOverLifetime.enabled)
                        {
                            var gradient = new Gradient();
                            var colorKey = new GradientColorKey[2];
                            var alphaKey = new GradientAlphaKey[2];
                            
                            // 设置渐变的颜色
                            colorKey[0].color = Color.green;
                            colorKey[0].time = 0.0f;
                            colorKey[1].color = Color.green;
                            colorKey[1].time = 1.0f;
                            
                            // 保持原有的透明度
                            alphaKey[0].alpha = 1.0f;
                            alphaKey[0].time = 0.0f;
                            alphaKey[1].alpha = 0.0f;
                            alphaKey[1].time = 1.0f;
                            
                            gradient.SetKeys(colorKey, alphaKey);
                            colorOverLifetime.color = gradient;
                        }
                        
                        // 修改颜色随速度变化模块
                        var colorBySpeed = ps.colorBySpeed;
                        if (colorBySpeed.enabled)
                        {
                            var gradient = new Gradient();
                            var colorKey = new GradientColorKey[2];
                            var alphaKey = new GradientAlphaKey[2];
                            
                            colorKey[0].color = Color.green;
                            colorKey[0].time = 0.0f;
                            colorKey[1].color = Color.green;
                            colorKey[1].time = 1.0f;
                            
                            alphaKey[0].alpha = 1.0f;
                            alphaKey[0].time = 0.0f;
                            alphaKey[1].alpha = 0.0f;
                            alphaKey[1].time = 1.0f;
                            
                            gradient.SetKeys(colorKey, alphaKey);
                            colorBySpeed.color = gradient;
                        }
                        
                        // 重新播放粒子系统以确保颜色更新
                        ps.Clear();
                        ps.Play();
                    }
                    
                    // 如果有渲染器组件，也修改其颜色
                    Renderer[] renderers = effect.GetComponentsInChildren<Renderer>(true);
                    foreach (Renderer renderer in renderers)
                    {
                        Material[] materials = renderer.materials;
                        foreach (Material material in materials)
                        {
                            material.color = Color.green;
                        }
                    }
                }
            }
            isEffectGreen = true;
            Debug.Log($"<color=green>节拍特效颜色：绿色</color> - 连续MISS次数：{currentMissCount}");
        }
    }

    // 将所有特效改为红色
    private void ChangeAllEffectsToRed()
    {
        // 确保当前是绿色状态
        if (isEffectGreen)
        {
            foreach (GameObject effect in activeEffects)
            {
                if (effect != null)
                {
                    // 获取所有粒子系统组件
                    ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>(true);
                    foreach (ParticleSystem ps in particleSystems)
                    {
                        // 修改主模块的颜色
                        var main = ps.main;
                        main.startColor = Color.red;
                        
                        // 修改颜色渐变模块
                        var colorOverLifetime = ps.colorOverLifetime;
                        if (colorOverLifetime.enabled)
                        {
                            var gradient = new Gradient();
                            var colorKey = new GradientColorKey[2];
                            var alphaKey = new GradientAlphaKey[2];
                            
                            // 设置渐变的颜色
                            colorKey[0].color = Color.red;
                            colorKey[0].time = 0.0f;
                            colorKey[1].color = Color.red;
                            colorKey[1].time = 1.0f;
                            
                            // 保持原有的透明度
                            alphaKey[0].alpha = 1.0f;
                            alphaKey[0].time = 0.0f;
                            alphaKey[1].alpha = 0.0f;
                            alphaKey[1].time = 1.0f;
                            
                            gradient.SetKeys(colorKey, alphaKey);
                            colorOverLifetime.color = gradient;
                        }
                        
                        // 修改颜色随速度变化模块
                        var colorBySpeed = ps.colorBySpeed;
                        if (colorBySpeed.enabled)
                        {
                            var gradient = new Gradient();
                            var colorKey = new GradientColorKey[2];
                            var alphaKey = new GradientAlphaKey[2];
                            
                            colorKey[0].color = Color.red;
                            colorKey[0].time = 0.0f;
                            colorKey[1].color = Color.red;
                            colorKey[1].time = 1.0f;
                            
                            alphaKey[0].alpha = 1.0f;
                            alphaKey[0].time = 0.0f;
                            alphaKey[1].alpha = 0.0f;
                            alphaKey[1].time = 1.0f;
                            
                            gradient.SetKeys(colorKey, alphaKey);
                            colorBySpeed.color = gradient;
                        }
                        
                        // 重新播放粒子系统以确保颜色更新
                        ps.Clear();
                        ps.Play();
                    }
                    
                    // 如果有渲染器组件，也修改其颜色
                    Renderer[] renderers = effect.GetComponentsInChildren<Renderer>(true);
                    foreach (Renderer renderer in renderers)
                    {
                        Material[] materials = renderer.materials;
                        foreach (Material material in materials)
                        {
                            material.color = Color.red;
                        }
                    }
                }
            }
            isEffectGreen = false;
            Debug.Log($"<color=red>节拍特效颜色：红色</color> - 连续MISS次数：{currentMissCount}");
        }
    }

    // 查找最近的节拍点
    private float FindNearestBeatTime(float currentTime)
    {
        if (beats == null || beats.Count == 0)
            return -1f;
        
        float nearestTime = beats[0];
        float minDiff = Mathf.Abs(currentTime - beats[0]);
        
        foreach (float beatTime in beats)
        {
            float diff = Mathf.Abs(currentTime - beatTime);
            if (diff < minDiff)
            {
                minDiff = diff;
                nearestTime = beatTime;
            }
        }
        
        return nearestTime;
    }

    // 检查是否在完美节拍点上
    private bool IsPerfectBeat(float currentTime, float beatTime)
    {
        if (beatTime < 0) return false;
        
        // 考虑系统延迟，将当前时间向前调整
        float adjustedTime = currentTime + (systemDelay / 1000f); // 将毫秒转换为秒
        float timeDiff = Mathf.Abs(adjustedTime - beatTime) * 1000f; // 转换为毫秒
        
        // 添加调试日志
        Debug.Log($"节拍判定 - 当前时间: {currentTime:F3}s, 调整后时间: {adjustedTime:F3}s, 节拍时间: {beatTime:F3}s, 时间差: {timeDiff:F1}ms");
        
        return timeDiff <= perfectBeatThreshold;
    }

    // 获取下一个即将到来的节拍点
    private float GetNextUpcomingBeat(float currentTime)
    {
        if (beats == null || beats.Count == 0)
            return -1f;
        
        float nextBeat = -1f;
        float minDiff = float.MaxValue;
        
        foreach (float beatTime in beats)
        {
            if (beatTime > currentTime)
            {
                float diff = beatTime - currentTime;
                if (diff < minDiff)
                {
                    minDiff = diff;
                    nextBeat = beatTime;
                }
            }
        }
        
        return nextBeat;
    }

    private void OnGUI()
    {
        if (!showEnergyBar || !isBulletTimeActive || player == null) return;
        
        // 根据当前激活的相机来显示能量条
        Camera activeCamera = isTopDownActive ? topDownCamera : Camera.main;
        if (activeCamera == null) return;
        
        // 计算玩家在屏幕上的位置（使用固定的2.0f高度，与机器人一致）
        Vector3 screenPos = activeCamera.WorldToScreenPoint(player.position + Vector3.up * 2.0f);
        
        // 当玩家在相机前方时显示能量条
        if (screenPos.z > 0)
        {
            // 调整Y坐标（GUI坐标系与屏幕坐标系Y轴相反）
            screenPos.y = Screen.height - screenPos.y;
            
            // 使用固定的尺寸（与机器人一致）
            float barWidth = 50f;
            float barHeight = 5f;
            
            // 绘制能量条背景
            GUI.color = Color.gray;
            GUI.DrawTexture(new Rect(screenPos.x - barWidth/2, screenPos.y - 30, barWidth, barHeight), Texture2D.whiteTexture);
            
            // 绘制当前能量
            GUI.color = energyBarColor;
            float energyRatio = (float)currentEnergy / maxEnergy;
            GUI.DrawTexture(new Rect(screenPos.x - barWidth/2, screenPos.y - 30, barWidth * energyRatio, barHeight), Texture2D.whiteTexture);
            
            // 显示调试信息
            if (Debug.isDebugBuild)
            {
                GUI.color = Color.white;
                string energyText = $"{currentEnergy}/{maxEnergy}";
                GUI.Label(new Rect(screenPos.x - 20, screenPos.y - 45, 40, 20), energyText);
            }
        }
    }

    // 启动highlight效果
    private void StartHighlightEffect()
    {
        if (highlightCoroutine != null)
        {
            StopCoroutine(highlightCoroutine);
        }
        
        highlightCoroutine = StartCoroutine(HandleHighlightEffect());
    }

    // 处理highlight效果的协程
    private IEnumerator HandleHighlightEffect()
    {
        isHighlightActive = true;
        
        // 保存原始聚光灯高度
        if (playerSpotlight != null)
        {
            originalSpotlightHeight = spotlightHeight;
            float targetHeight = 6f;
            
            // 平滑过渡到目标高度
            while (Mathf.Abs(spotlightHeight - targetHeight) > 0.01f)
            {
                spotlightHeight = Mathf.Lerp(spotlightHeight, targetHeight, Time.deltaTime * spotlightTransitionSpeed);
                UpdateSpotlightPosition();
                yield return null;
            }
            
            Debug.Log("聚光灯高度已平滑调整为6");
        }
        
        // 切换到highlight音乐状态
        try
        {
            AudioStateManager.SetSingleState("highlight");
            Debug.Log("音乐已切换到'highlight'状态");
        }
        catch (System.Exception e)
        {
            Debug.LogError("切换到highlight音乐状态失败: " + e.Message);
        }
        
        // 等待10秒
        yield return new WaitForSeconds(10f);
        
        // 恢复原始设置
        if (playerSpotlight != null)
        {
            // 平滑过渡回原始高度
            while (Mathf.Abs(spotlightHeight - originalSpotlightHeight) > 0.01f)
            {
                spotlightHeight = Mathf.Lerp(spotlightHeight, originalSpotlightHeight, Time.deltaTime * spotlightTransitionSpeed);
                UpdateSpotlightPosition();
                yield return null;
            }
            
            spotlightHeight = originalSpotlightHeight; // 确保完全恢复到原始高度
            UpdateSpotlightPosition();
            Debug.Log("聚光灯高度已平滑恢复到原始高度");
        }
        
        // 切换回discovered音乐状态
        try
        {
            AudioStateManager.SetSingleState("discovered");
            Debug.Log("音乐已恢复到'discovered'状态");
        }
        catch (System.Exception e)
        {
            Debug.LogError("切换回discovered音乐状态失败: " + e.Message);
        }
        
        isHighlightActive = false;
        highlightCoroutine = null;
    }

    // 创建敌人聚光灯
    private void CreateEnemySpotlights()
    {
        // 清除现有的聚光灯
        foreach (var spotlight in enemySpotlights.Values)
        {
            if (spotlight != null)
            {
                Destroy(spotlight.gameObject);
            }
        }
        enemySpotlights.Clear();

        // 查找所有敌人
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        foreach (GameObject enemy in enemies)
        {
            CreateEnemySpotlight(enemy);
        }
        
        Debug.Log($"已为 {enemies.Length} 个敌人创建头顶聚光灯");
    }

    // 为单个敌人创建聚光灯
    private void CreateEnemySpotlight(GameObject enemy)
    {
        if (enemy == null) return;

        // 创建聚光灯游戏对象
        GameObject spotlightObj = new GameObject($"EnemySpotlight_{enemy.name}");
        Light spotlight = spotlightObj.AddComponent<Light>();
        
        // 设置聚光灯属性
        spotlight.type = LightType.Spot;
        spotlight.color = enemySpotlightColor;
        spotlight.intensity = enemySpotlightIntensity;
        spotlight.range = enemySpotlightRange;
        spotlight.spotAngle = enemySpotlightAngle;
        spotlight.shadows = LightShadows.Soft; // 启用软阴影
        
        // 更新聚光灯位置
        UpdateEnemySpotlightPosition(enemy, spotlight);
        
        // 将聚光灯添加到字典中
        enemySpotlights[enemy] = spotlight;
    }

    // 更新敌人聚光灯位置
    private void UpdateEnemySpotlightPosition(GameObject enemy, Light spotlight)
    {
        if (enemy != null && spotlight != null)
        {
            // 获取敌人高度
            float enemyHeight = 0;
            Collider enemyCollider = enemy.GetComponent<Collider>();
            if (enemyCollider != null)
            {
                enemyHeight = enemyCollider.bounds.size.y;
            }
            else
            {
                // 如果没有碰撞体，使用估计值
                enemyHeight = 2f;
            }
            
            // 设置聚光灯位置在敌人上方
            spotlight.transform.position = enemy.transform.position + Vector3.up * (enemyHeight + enemySpotlightHeight);
            
            // 聚光灯始终朝下
            spotlight.transform.rotation = Quaternion.Euler(90, 0, 0);
        }
    }

    // 更新所有敌人的聚光灯
    private void UpdateAllEnemySpotlights()
    {
        // 获取当前所有敌人
        GameObject[] currentEnemies = GameObject.FindGameObjectsWithTag(enemyTag);
        HashSet<GameObject> currentEnemySet = new HashSet<GameObject>(currentEnemies);
        
        // 移除已经不存在的敌人的聚光灯
        List<GameObject> enemyToRemove = new List<GameObject>();
        foreach (var pair in enemySpotlights)
        {
            if (!currentEnemySet.Contains(pair.Key) || pair.Key == null)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value.gameObject);
                }
                enemyToRemove.Add(pair.Key);
            }
        }
        foreach (var enemy in enemyToRemove)
        {
            enemySpotlights.Remove(enemy);
        }
        
        // 为新出现的敌人创建聚光灯
        foreach (GameObject enemy in currentEnemies)
        {
            if (!enemySpotlights.ContainsKey(enemy))
            {
                CreateEnemySpotlight(enemy);
            }
            else
            {
                // 更新现有敌人的聚光灯位置
                UpdateEnemySpotlightPosition(enemy, enemySpotlights[enemy]);
            }
        }
    }
} 