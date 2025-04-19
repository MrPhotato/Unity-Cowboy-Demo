using UnityEngine;
using UnityEngine.AI;
using StarterAssets;
using System.Collections;
using System.Collections.Generic;

public class RobotEnemy : MonoBehaviour, IDamageable
{
    [Header("目标和移动")]
    public Transform target; // 玩家目标
    public float chaseDistance = 20f; // 开始追逐的距离
    public float attackDistance = 2f; // 攻击距离
    public float moveSpeed = 3.5f; // 移动速度

    [Header("巡逻设置")]
    public bool enablePatrol = true; // 是否启用巡逻
    public float patrolRadius = 10f; // 巡逻半径
    public float minPatrolWaitTime = 1f; // 最小巡逻点等待时间
    public float maxPatrolWaitTime = 3f; // 最大巡逻点等待时间
    public float patrolSpeed = 0.8f; // 巡逻移动速度（设置更慢）
    public float patrolAnimationSpeed = 3.0f; // 巡逻时的动画速度
    private Vector3 patrolPoint; // 当前巡逻目标点
    private bool isWaitingAtPatrolPoint = false; // 是否正在巡逻点等待
    private float patrolWaitTimer = 0f; // 巡逻点等待计时器
    private float currentPatrolWaitTime = 0f; // 当前巡逻点应等待时间
    private Vector3 initialPosition; // 初始位置，用作巡逻中心点

    [Header("生命周期")]
    public bool enableLifetime = true; // 是否启用生命周期
    public float lifetime = 15f; // 生命周期（秒）
    private float lifetimeTimer = 0f; // 生命周期计时器
    
    [Header("攻击设置")]
    public float attackCooldown = 1.5f; // 攻击冷却时间
    public int damagePerHit = 1; // 每次攻击造成的伤害
    
    [Header("动画设置")]
    public float animationSmoothTime = 0.1f; // 动画平滑过渡时间
    public float animationSpeedThreshold = 0.1f; // 动画速度阈值

    [Header("调试")]
    public bool showDebugInfo = true;

    [Header("生命值设置")]
    public int maxHealth = 3; // 最大生命值
    public int currentHealth; // 当前生命值
    public bool showHealthBar = true; // 是否显示血条
    public float damageFlashDuration = 0.2f; // 受伤闪烁时间
    private bool isFlashing = false; // 是否正在闪烁
    private Material[] originalMaterials; // 原始材质
    private Renderer[] renderers; // 渲染器组件

    // 内部引用
    private NavMeshAgent agent;
    private StarterAssetsInputs enemyInputs;
    private Animator animator;
    private bool isDead = false;
    private bool isAttacking = false;
    private float lastAttackTime = 0f;
    
    // 动画参数ID
    private int _animIDSpeed;
    private int _animIDMotionSpeed;
    private int _animIDAttack;
    private int _animIDDead;
    private int _animIDGrounded;

    // 敌人行为状态
    private enum EnemyState
    {
        Patrolling,
        Chasing,
        Attacking,
        Dead
    }
    private EnemyState currentState = EnemyState.Patrolling;

    private void Start()
    {
        // 获取组件引用
        agent = GetComponent<NavMeshAgent>();
        enemyInputs = GetComponent<StarterAssetsInputs>();
        animator = GetComponent<Animator>();
        
        // 保存初始位置作为巡逻中心
        initialPosition = transform.position;
        
        // 生成第一个巡逻点
        if (enablePatrol)
        {
            GenerateNewPatrolPoint();
        }
        
        // 检查碰撞体
        CheckAndSetupColliders();
        
        // 检查场景中是否有MainCamera
        if (Camera.main == null)
        {
            Debug.LogWarning("场景中没有标记为MainCamera的相机，血条将不会显示");
            
            // 尝试查找场景中的所有相机
            Camera[] allCameras = GameObject.FindObjectsOfType<Camera>();
            if (allCameras.Length > 0)
            {
                // 将第一个找到的相机标记为MainCamera
                allCameras[0].tag = "MainCamera";
                Debug.Log($"已将相机 {allCameras[0].name} 标记为MainCamera");
            }
        }
        
        // 获取所有渲染器组件和原始材质
        renderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].material;
        }

        // 初始化生命值
        currentHealth = maxHealth;
        
        // 移除ThirdPersonController，避免冲突
        ThirdPersonController thirdPersonCtrl = GetComponent<ThirdPersonController>();
        if (thirdPersonCtrl != null)
        {
            Debug.LogWarning("移除不必要的ThirdPersonController组件");
            Destroy(thirdPersonCtrl);
        }

        // 检查是否缺少任何重要组件
        if (agent == null || enemyInputs == null || animator == null)
        {
            Debug.LogError($"机器人缺少必要组件: {(agent == null ? "NavMeshAgent " : "")}{(enemyInputs == null ? "StarterAssetsInputs " : "")}{(animator == null ? "Animator " : "")}");
            Debug.LogError("请添加SetupRobot组件并点击'自动设置机器人'按钮");
            enabled = false; // 禁用此脚本
            return;
        }

        // 初始化动画参数ID
        AssignAnimationIDs();

        // 设置NavMeshAgent
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = attackDistance * 0.8f;
            
            // 检查是否在NavMesh上
            if (!agent.isOnNavMesh)
            {
                Debug.LogWarning("机器人不在NavMesh上! 尝试放置到最近的NavMesh位置");
                PlaceOnNavMesh();
            }
            else
            {
                Debug.Log("机器人已在NavMesh上，位置正常");
            }
        }

        // 查找玩家目标
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                // Debug.Log($"已找到玩家: {player.name}，位置: {player.transform.position}");
            }
            else
            {
                Debug.LogError("找不到Player标签的对象! 确保您的玩家对象已设置Player标签");
                // 尝试查找名称包含"Player"的对象作为备选
                GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.Contains("Player") || obj.name.Contains("Character") || obj.name.Contains("Cowboy"))
                    {
                        target = obj.transform;
                        Debug.Log($"根据名称找到可能的玩家: {obj.name}");
                        break;
                    }
                }
                
                if (target == null)
                {
                    Debug.LogError("无法找到任何可能的玩家目标，AI将无法工作");
                    enabled = false; // 禁用此脚本
                    return;
                }
            }
        }
        else
        {
            Debug.Log($"已直接指定目标: {target.name}");
        }
        
        // 确保机器人在地面上
        if (transform.position.y < -10 || float.IsNaN(transform.position.y))
        {
            Debug.LogError("机器人位置异常，尝试重置位置");
            if (target != null)
            {
                transform.position = target.position + new Vector3(0, 0, 5);
                PlaceOnNavMesh();
            }
        }
        
        // 确保Animator有适当的控制器
        if (animator != null && animator.runtimeAnimatorController == null)
        {
            Debug.LogError("机器人Animator没有控制器! 请设置AnimatorController");
        }

        // 初始化生命周期计时器
        lifetimeTimer = 0f;
    }
    
    // 初始化动画参数ID
    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        _animIDAttack = Animator.StringToHash("Attack");
        _animIDDead = Animator.StringToHash("Dead");
        _animIDGrounded = Animator.StringToHash("Grounded");
    }
    
    // 更新动画状态
    private void UpdateAnimator()
    {
        if (animator == null || agent == null) return;
        
        // 检查死亡状态
        if (isDead)
        {
            animator.SetBool(_animIDDead, true);
            animator.SetFloat(_animIDSpeed, 0);
            return;
        }
        
        // 根据NavMeshAgent速度设置移动动画
        float currentSpeed = agent.velocity.magnitude;
        
        // 定义速度阈值
        float idleThreshold = 0.1f;
        float walkThreshold;
        
        // 根据当前状态调整阈值和动画速度映射
        if (currentState == EnemyState.Patrolling)
        {
            // 巡逻状态下使用专门的速度映射，确保使用走路动画
            walkThreshold = patrolSpeed * 0.9f; // 几乎所有巡逻速度都映射到走路动画
        }
        else
        {
            // 正常追踪和攻击状态
            walkThreshold = agent.speed * 0.6f; // 60%的最大速度以下认为是走路
        }
        
        // 映射速度值到动画混合树中的阈值
        // Idle: 0, Walk: 2, Run: 6
        float mappedSpeed;
        
        if (currentSpeed < idleThreshold) 
        {
            // 静止状态 - Idle (0)
            mappedSpeed = 0f;
        }
        else if (currentState == EnemyState.Patrolling || currentSpeed < walkThreshold) 
        {
            // 巡逻状态或走路状态 - 映射到0-2之间
            // 巡逻状态强制使用走路动画，即使速度可能高于阈值
            if (currentState == EnemyState.Patrolling)
            {
                // 巡逻时使用设定的动画速度值
                mappedSpeed = patrolAnimationSpeed; // 使用可配置的动画速度
            }
            else
            {
                // 正常走路
                float walkRatio = (currentSpeed - idleThreshold) / (walkThreshold - idleThreshold);
                mappedSpeed = Mathf.Lerp(0.5f, 2f, walkRatio);
            }
        }
        else 
        {
            // 跑步状态 - 映射到2-6之间
            float runRatio = (currentSpeed - walkThreshold) / (agent.speed - walkThreshold);
            mappedSpeed = Mathf.Lerp(2.2f, 6f, Mathf.Clamp01(runRatio)); // 确保正好大于Walk阈值
        }
        
        // 平滑过渡动画参数
        float currentAnimSpeed = animator.GetFloat(_animIDSpeed);
        float newSpeed = Mathf.Lerp(currentAnimSpeed, mappedSpeed, Time.deltaTime / animationSmoothTime);
        
        // 应用动画参数
        animator.SetFloat(_animIDSpeed, newSpeed);
        
        // 当巡逻时使用正常动作速度，不再刻意减慢
        float motionSpeedValue = mappedSpeed / 6f; // 归一化的动作速度
        animator.SetFloat(_animIDMotionSpeed, motionSpeedValue);
        animator.SetBool(_animIDGrounded, true); // 机器人始终在地面上
        
        // 处理攻击动画
        animator.SetBool(_animIDAttack, isAttacking);
        
        if (showDebugInfo && (newSpeed > 0.1f || currentSpeed > 0.1f))
        {
            // Debug.Log($"机器人动画: 状态={currentState}, Speed={newSpeed:F2}, 原始速度={currentSpeed:F2}, 映射速度={mappedSpeed:F2}, 动作速度={motionSpeedValue:F2}");
        }
    }

    // 尝试将机器人放置到最近的NavMesh位置
    private void PlaceOnNavMesh()
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            Debug.Log("已将机器人放置到最近的NavMesh位置");
        }
        else
        {
            Debug.LogError("无法找到附近的NavMesh位置，请手动移动机器人");
        }
    }

    private void Update()
    {
        // 检查生命周期 - 只有非巡逻状态的机器人才会自然死亡
        if (enableLifetime && !isDead && currentState != EnemyState.Patrolling)
        {
            lifetimeTimer += Time.deltaTime;
            if (lifetimeTimer >= lifetime)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"机器人 {gameObject.name} 生命周期结束，自然死亡");
                }
                Die();
                return;
            }
        }
        
        // 检查目标是否存在
        if (isDead)
        {
            currentState = EnemyState.Dead;
            return;
        }
            
        // 确保NavMeshAgent组件有效且在NavMesh上
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
        {
            return;
        }

        // 确定当前状态
        UpdateEnemyState();

        // 根据状态执行相应行为
        switch (currentState)
        {
            case EnemyState.Patrolling:
                // 处于巡逻状态时重置生命计时器
                lifetimeTimer = 0f;
                UpdatePatrolBehavior();
                break;

            case EnemyState.Chasing:
                ChasePlayer();
                break;

            case EnemyState.Attacking:
                StopAndAttack();
                break;

            case EnemyState.Dead:
                // 已死亡，无需行动
                break;
        }
        
        // 更新动画
        UpdateAnimator();
    }

    // 更新敌人状态
    private void UpdateEnemyState()
    {
        // 记录之前的状态，用于检测状态变化
        EnemyState previousState = currentState;
        
        // 检查死亡状态
        if (isDead)
        {
            currentState = EnemyState.Dead;
            return;
        }

        // 检查目标是否存在
        if (target == null)
        {
            currentState = EnemyState.Patrolling;
            return;
        }

        // 计算与目标的距离
        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // 根据距离决定状态
        if (distanceToTarget <= attackDistance)
        {
            currentState = EnemyState.Attacking;
        }
        else if (distanceToTarget <= chaseDistance)
        {
            currentState = EnemyState.Chasing;
        }
        else
        {
            currentState = EnemyState.Patrolling;
        }
        
        // 检测从巡逻状态到追击状态的变化，触发"被发现"音乐状态
        if (previousState == EnemyState.Patrolling && currentState == EnemyState.Chasing)
        {
            // 检查是否已经处于"discovered"或"excited"状态，避免重复激活
            if (!AudioStateManager.IsStateActive("discovered") && 
                !AudioStateManager.IsStateActive("excited"))
            {
                // 激活"被发现"音乐状态
                AudioStateManager.ActivateState("discovered");
                
                if (showDebugInfo)
                {
                    Debug.Log("敌人发现了玩家，激活'被发现'音乐状态");
                }
            }
        }
    }

    // 巡逻行为更新
    private void UpdatePatrolBehavior()
    {
        if (!enablePatrol) return;

        // 如果正在等待
        if (isWaitingAtPatrolPoint)
        {
            patrolWaitTimer += Time.deltaTime;
            
            // 等待时间结束
            if (patrolWaitTimer >= currentPatrolWaitTime)
            {
                isWaitingAtPatrolPoint = false;
                GenerateNewPatrolPoint();
            }
            return;
        }

        // 如果已设置巡逻点，检查是否到达
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            // 设置移动速度为巡逻速度
            agent.speed = patrolSpeed;
            
            // 移动到巡逻点
            agent.SetDestination(patrolPoint);
            agent.isStopped = false;
            
            // 检查是否已到达巡逻点
            float distanceToPatrolPoint = Vector3.Distance(transform.position, patrolPoint);
            if (distanceToPatrolPoint < 1.0f || !agent.pathPending && agent.remainingDistance < 1.0f)
            {
                // 到达巡逻点，开始等待
                StartWaitingAtPatrolPoint();
            }
            
            // 调试显示
            if (showDebugInfo)
            {
                Debug.DrawLine(transform.position, patrolPoint, Color.blue);
            }
        }
    }

    // 开始在巡逻点等待
    private void StartWaitingAtPatrolPoint()
    {
        isWaitingAtPatrolPoint = true;
        patrolWaitTimer = 0f;
        currentPatrolWaitTime = Random.Range(minPatrolWaitTime, maxPatrolWaitTime);
        
        // 停止移动
        if (agent != null)
        {
            agent.isStopped = true;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"巡逻: 到达巡逻点，等待 {currentPatrolWaitTime:F1} 秒");
        }
    }

    // 生成新的巡逻点
    private void GenerateNewPatrolPoint()
    {
        // 在以初始位置为中心的圆内随机选择点
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
        Vector3 randomDirection = new Vector3(randomCircle.x, 0, randomCircle.y);
        Vector3 newPoint = initialPosition + randomDirection;
        
        // 尝试在NavMesh上找到有效点
        NavMeshHit hit;
        if (NavMesh.SamplePosition(newPoint, out hit, patrolRadius, NavMesh.AllAreas))
        {
            patrolPoint = hit.position;
            
            if (showDebugInfo)
            {
                // Debug.Log($"巡逻: 生成新巡逻点 {patrolPoint}");
            }
        }
        else
        {
            // 如果找不到有效点，使用当前位置附近的点
            NavMesh.SamplePosition(transform.position + Random.insideUnitSphere * 5f, out hit, 5f, NavMesh.AllAreas);
            patrolPoint = hit.position;
            
            if (showDebugInfo)
            {
                Debug.LogWarning($"巡逻: 无法找到有效巡逻点，使用附近点 {patrolPoint}");
            }
        }
        
        // 确保代理未停止
        if (agent != null)
        {
            agent.isStopped = false;
            agent.speed = patrolSpeed;
        }
    }

    private void ChasePlayer()
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
        {
            return;
        }

        // 计算到目标的距离
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        // 确保代理没有停止并设置目标
        agent.isStopped = false;
        
        // 当接近攻击范围时，减慢速度实现平滑过渡
        float slowDownStartDistance = attackDistance * 1.5f; // 开始减速的距离
        if (distanceToTarget < slowDownStartDistance && distanceToTarget > attackDistance)
        {
            // 根据接近攻击范围的程度逐渐减速
            float speedRatio = (distanceToTarget - attackDistance) / (slowDownStartDistance - attackDistance);
            float adjustedSpeed = Mathf.Lerp(moveSpeed * 0.5f, moveSpeed, speedRatio);
            agent.speed = adjustedSpeed;
            
            if (showDebugInfo)
            {
                // Debug.Log($"接近目标，减速至: {adjustedSpeed:F2}");
            }
        }
        else
        {
            // 恢复正常速度
            agent.speed = moveSpeed;
        }
        
        bool success = agent.SetDestination(target.position);
        
        // 调试是否成功设置目标
        if (showDebugInfo)
        {
            // Debug.Log($"设置追踪目标 {target.position}，结果: {(success ? "成功" : "失败")}");
            Debug.DrawLine(transform.position, agent.destination, Color.green);
        }
    }

    private void StopAndAttack()
    {
        // 停止移动
        if (agent != null)
        {
            agent.isStopped = true;
        }

        // 面向玩家
        if (target != null)
        {
            Vector3 lookDirection = target.position - transform.position;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero) // 避免零向量
            {
                // 平滑旋转
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                float rotationSpeed = isAttacking ? 15f : 8f; // 攻击时旋转更快
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                
                // 计算指向角度与当前角度的差值
                float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);
                if (showDebugInfo && angleDifference > 5f)
                {
                    // Debug.Log($"旋转中: 差值={angleDifference:F1}度");
                }
            }
        }

        // 检查攻击冷却
        if (Time.time >= lastAttackTime + attackCooldown && !isAttacking)
        {
            Attack();
        }
    }

    private void Attack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        // 设置动画状态
        if (animator != null)
        {
            animator.SetBool(_animIDAttack, true);
        }

        // 检查是否击中玩家
        StartCoroutine(DealDamageWithDelay(0.5f)); // 伤害延迟半秒，与动画同步
    }

    private IEnumerator DealDamageWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 判断玩家是否在攻击范围内且在前方
        if (target != null && Vector3.Distance(transform.position, target.position) <= attackDistance * 1.2f)
        {
            // 射线检测确保有视线
            Vector3 directionToPlayer = (target.position - transform.position).normalized;
            if (Vector3.Dot(transform.forward, directionToPlayer) > 0.5f) // 确保玩家在前方
            {
                // 对玩家造成伤害
                PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damagePerHit);
                    Debug.Log("对玩家造成伤害！剩余生命: " + playerHealth.currentHealth);
                }
                else
                {
                    Debug.LogWarning("玩家没有PlayerHealth组件!");
                }
            }
        }

        // 重置攻击状态
        isAttacking = false;
        if (animator != null)
        {
            animator.SetBool(_animIDAttack, false);
        }
    }

    // 被调用使敌人死亡
    public void Die()
    {
        if (isDead) return;

        isDead = true;
        
        // 设置死亡动画
        if (animator != null)
        {
            animator.SetBool(_animIDDead, true);
        }
        
        // 禁用NavMeshAgent和碰撞体
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }
        
        // 禁用碰撞体
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        
        // 1秒后销毁对象
        StartCoroutine(DestroyAfterDelay(3f));
    }
    
    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
    
    // 使敌人受到伤害的公共方法
    public void TakeDamage(int damage)
    {
        // 如果已经死亡，不再处理伤害
        if (isDead) return;
        
        // 减少生命值
        currentHealth -= damage;
        
        // 显示调试信息
        Debug.Log($"机器人受到{damage}点伤害! 剩余生命: {currentHealth}/{maxHealth}");
        
        // 生命值为0或更低时死亡
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        else
        {
            // 受伤反馈 - 闪烁效果
            if (!isFlashing)
            {
                StartCoroutine(DamageFlash());
            }
            
            // 播放受伤动画或声音
            // 可以添加Animator.SetTrigger("Hit")等
        }
    }
    
    // 受伤闪烁效果
    private IEnumerator DamageFlash()
    {
        isFlashing = true;
        
        // 创建红色材质
        Material flashMaterial = new Material(Shader.Find("Standard"));
        flashMaterial.color = Color.red;
        
        // 应用红色材质到所有渲染器
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.material = flashMaterial;
            }
        }
        
        // 等待短暂时间
        yield return new WaitForSeconds(damageFlashDuration);
        
        // 恢复原始材质
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && i < originalMaterials.Length && originalMaterials[i] != null)
            {
                renderers[i].material = originalMaterials[i];
            }
        }
        
        isFlashing = false;
    }
    
    // 在OnGUI中绘制血条
    private void OnGUI()
    {
        if (!showHealthBar || isDead) return;
        
        // 检查主相机是否存在
        if (Camera.main == null)
        {
            // 如果没有主相机，不显示血条
            return;
        }
        
        // 计算敌人在屏幕上的位置
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        
        // 当敌人在相机前方时显示血条
        if (screenPos.z > 0)
        {
            // 调整Y坐标（GUI坐标系与屏幕坐标系Y轴相反）
            screenPos.y = Screen.height - screenPos.y;
            
            // 血条尺寸
            float healthBarWidth = 50f;
            float healthBarHeight = 5f;
            
            // 绘制血条背景
            GUI.color = Color.gray;
            GUI.DrawTexture(new Rect(screenPos.x - healthBarWidth/2, screenPos.y - 30, healthBarWidth, healthBarHeight), Texture2D.whiteTexture);
            
            // 绘制当前血量
            GUI.color = Color.red;
            float healthRatio = (float)currentHealth / maxHealth;
            GUI.DrawTexture(new Rect(screenPos.x - healthBarWidth/2, screenPos.y - 30, healthBarWidth * healthRatio, healthBarHeight), Texture2D.whiteTexture);
        }
    }

    // 显示调试用的Gizmos
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
        
        // 显示巡逻区域
        if (Application.isPlaying && enablePatrol)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(initialPosition, patrolRadius);
            
            // 显示当前巡逻点
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(patrolPoint, 0.5f);
            Gizmos.DrawLine(transform.position, patrolPoint);
        }
    }

    // 强制激活NavMeshAgent并重新设置目标
    public void ForceActivateAgent()
    {
        if (agent != null && target != null)
        {
            agent.enabled = false;
            agent.enabled = true;
            
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(target.position);
                Debug.Log("已强制重置NavMeshAgent");
            }
            else
            {
                PlaceOnNavMesh();
            }
        }
    }

    // 用于手动测试的公共方法
    public void TestChase()
    {
        if (target != null)
        {
            ForceActivateAgent();
            Debug.Log($"手动启动追踪，目标={target.name}，位置={target.position}");
        }
        else
        {
            Debug.LogError("无法启动追踪，目标为空");
        }
    }

    // 检测碰撞
    private void OnCollisionEnter(Collision collision)
    {
        // 检查是否是子弹
        if (collision.gameObject.CompareTag("Bullet") || collision.gameObject.CompareTag("Projectile"))
        {
            // 尝试获取子弹的伤害值（如果有脚本包含damage属性）
            MonoBehaviour[] components = collision.gameObject.GetComponents<MonoBehaviour>();
            bool damageFound = false;
            
            foreach (MonoBehaviour component in components)
            {
                // 通过反射尝试获取damage字段或属性
                System.Reflection.FieldInfo damageField = component.GetType().GetField("damage");
                if (damageField != null)
                {
                    int bulletDamage = 1; // 默认值
                    try
                    {
                        object value = damageField.GetValue(component);
                        if (value is int)
                        {
                            bulletDamage = (int)value;
                            TakeDamage(bulletDamage);
                            damageFound = true;
                            break;
                        }
                    }
                    catch (System.Exception)
                    {
                        // 反射失败，使用默认值
                    }
                }
            }
            
            // 如果没有找到伤害值，使用默认值
            if (!damageFound)
            {
                TakeDamage(1);
            }
            
            // 记录碰撞并销毁子弹
            Debug.Log("子弹碰撞检测成功：" + collision.gameObject.name);
            Destroy(collision.gameObject);
        }
    }
    
    // 检测触发器碰撞
    private void OnTriggerEnter(Collider other)
    {
        // 检查是否是子弹
        if (other.CompareTag("Bullet") || other.CompareTag("Projectile"))
        {
            // 默认使用伤害值1
            TakeDamage(1);
            
            // 记录碰撞并销毁子弹
            Debug.Log("子弹触发器检测成功：" + other.name);
            Destroy(other.gameObject);
        }
    }

    // 检查并设置碰撞体
    private void CheckAndSetupColliders()
    {
        // 获取所有碰撞体
        Collider[] colliders = GetComponentsInChildren<Collider>();
        bool hasValidCollider = false;
        
        // 检查是否有有效的碰撞体
        foreach (Collider col in colliders)
        {
            // 忽略触发器
            if (!col.isTrigger)
            {
                hasValidCollider = true;
                break;
            }
        }
        
        // 如果没有有效的碰撞体，则添加一个
        if (!hasValidCollider)
        {
            Debug.LogWarning("敌人没有有效的碰撞体，添加CapsuleCollider");
            
            // 添加胶囊碰撞体
            CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.5f;
            capsule.center = new Vector3(0, 1f, 0);
            
            // 确保NavMeshAgent的高度和半径与碰撞体匹配
            if (agent != null)
            {
                agent.height = capsule.height;
                agent.radius = capsule.radius;
            }
        }
        
        // 验证碰撞体大小是否合理
        foreach (Collider col in colliders)
        {
            if (!col.isTrigger)
            {
                // 获取碰撞体的边界框
                Bounds bounds = col.bounds;
                float volume = bounds.size.x * bounds.size.y * bounds.size.z;
                
                // 如果碰撞体太小，输出警告
                if (volume < 0.1f)
                {
                    Debug.LogWarning($"碰撞体 {col.name} 可能太小，子弹可能难以击中。体积: {volume}");
                }
                // 如果碰撞体太大，输出警告
                else if (volume > 10f)
                {
                    Debug.LogWarning($"碰撞体 {col.name} 可能太大，子弹容易击中错误的部位。体积: {volume}");
                }
            }
        }
    }
} 