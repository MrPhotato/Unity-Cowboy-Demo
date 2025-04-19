using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("生成设置")]
    public GameObject robotEnemyPrefab; // 机器人敌人预制体
    public Transform player; // 玩家位置
    public int maxEnemyCount = 5; // 最大敌人数量
    public float spawnRadius = 15f; // 生成半径，增大以更容易找到生成点
    public float checkInterval = 1f; // 检查间隔（秒），减少以更快响应
    public float minDistanceFromPlayer = 5f; // 生成点与玩家的最小距离
    
    [Header("调试设置")]
    public bool showDebugInfo = true; // 开启调试信息，帮助发现问题
    public bool showSpawnPoints = false; // 是否显示生成点
    
    [Header("备份设置")]
    private GameObject backupPrefab; // 备份预制体，在所有敌人消灭后使用
    private bool hasCreatedBackup = false; // 是否已创建备份

    private float nextCheckTime; // 下次检查时间
    private int failedAttempts = 0; // 记录找不到生成点的尝试次数
    
    // 保存已生成的敌人，避免重复查找
    private List<GameObject> spawnedEnemies = new List<GameObject>();

    private void Start()
    {
        // 如果未指定玩家，尝试查找
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log("已自动找到玩家: " + player.name);
            }
            else
            {
                Debug.LogError("找不到玩家！请手动指定玩家位置");
                enabled = false;
                return;
            }
        }

        // 检查场景中已有敌人并添加到列表
        GameObject[] existingEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in existingEnemies)
        {
            if (!spawnedEnemies.Contains(enemy))
            {
                spawnedEnemies.Add(enemy);
                
                // 创建备份预制体（如果需要）
                if (!hasCreatedBackup && backupPrefab == null)
                {
                    CreateBackupFromEnemy(enemy);
                }
                
                // 监听敌人的死亡
                RobotEnemy robotEnemy = enemy.GetComponent<RobotEnemy>();
                if (robotEnemy != null)
                {
                    // 记录初始生命值，用于死亡检测
                    StartCoroutine(MonitorEnemyHealth(enemy, robotEnemy));
                }
            }
        }
        
        // 如果没有指定预制体，且没有从场景中创建备份，显示警告
        if (robotEnemyPrefab == null && backupPrefab == null)
        {
            if (existingEnemies.Length == 0)
            {
                Debug.LogError("没有指定敌人预制体，且场景中不存在敌人！无法生成新敌人");
                enabled = false;
                return;
            }
            else
            {
                Debug.LogWarning("没有指定敌人预制体，将使用场景中的敌人作为模板");
            }
        }

        // 立即检查并生成敌人，不延迟
        CheckAndSpawnEnemies();
        
        // 设置下次检查时间
        nextCheckTime = Time.time + checkInterval;
    }
    
    // 从现有敌人创建备份预制体
    private void CreateBackupFromEnemy(GameObject enemy)
    {
        if (enemy == null) return;
        
        try
        {
            // 创建一个深拷贝
            backupPrefab = Instantiate(enemy);
            
            // 将备份预制体隐藏并禁用
            backupPrefab.SetActive(false);
            backupPrefab.name = "BackupEnemyTemplate";
            
            // 保持这个对象不被销毁（即使场景重新加载）
            DontDestroyOnLoad(backupPrefab);
            
            hasCreatedBackup = true;
            
            if (showDebugInfo)
            {
                Debug.Log("已从现有敌人创建备份模板");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"创建敌人备份失败: {e.Message}");
        }
    }

    // 获取可用的敌人预制体（首选指定的，其次是备份的）
    private GameObject GetEnemyPrefab()
    {
        // 首先检查是否指定了预制体
        if (robotEnemyPrefab != null)
        {
            return robotEnemyPrefab;
        }
        
        // 其次检查是否有备份预制体
        if (backupPrefab != null)
        {
            return backupPrefab;
        }
        
        // 如果两者都没有，尝试从场景中找一个敌人
        if (spawnedEnemies.Count > 0)
        {
            // 找到第一个有效的敌人
            foreach (GameObject enemy in spawnedEnemies)
            {
                if (enemy != null)
                {
                    // 创建备份并返回
                    CreateBackupFromEnemy(enemy);
                    return backupPrefab;
                }
            }
        }
        
        // 最后，尝试在整个场景中搜索
        GameObject[] anyEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (anyEnemies.Length > 0)
        {
            CreateBackupFromEnemy(anyEnemies[0]);
            return backupPrefab;
        }
        
        // 如果真的一个都没找到，返回null
        Debug.LogError("无法找到任何可用的敌人预制体！无法生成新敌人");
        return null;
    }

    // 监控敌人生命值
    private IEnumerator MonitorEnemyHealth(GameObject enemy, RobotEnemy robotEnemy)
    {
        int lastHealth = robotEnemy.currentHealth;
        
        while (enemy != null && robotEnemy != null)
        {
            // 检测生命值变化
            if (robotEnemy.currentHealth <= 0 && lastHealth > 0)
            {
                // 如果还没有备份，并且这是最后一个敌人，创建备份
                if (!hasCreatedBackup && backupPrefab == null && spawnedEnemies.Count <= 1)
                {
                    CreateBackupFromEnemy(enemy);
                }
                
                // 敌人死亡，立即安排生成新敌人
                if (showDebugInfo)
                {
                    Debug.Log("检测到敌人死亡，准备生成新敌人");
                }
                spawnedEnemies.Remove(enemy);
                yield return new WaitForSeconds(0.5f); // 短暂延迟
                CheckAndSpawnEnemies();
                yield break;
            }
            
            lastHealth = robotEnemy.currentHealth;
            yield return new WaitForSeconds(0.2f); // 定期检查
        }
        
        // 如果敌人对象不存在了，从列表移除
        if (enemy == null || robotEnemy == null)
        {
            spawnedEnemies.Remove(enemy);
            yield return new WaitForSeconds(0.5f); // 短暂延迟
            CheckAndSpawnEnemies();
        }
    }

    private void Update()
    {
        // 更积极地清理和检测已销毁的敌人
        int removedCount = 0;
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            if (spawnedEnemies[i] == null)
            {
                spawnedEnemies.RemoveAt(i);
                removedCount++;
            }
            else
            {
                // 检查敌人是否已死亡但未被销毁
                RobotEnemy enemy = spawnedEnemies[i].GetComponent<RobotEnemy>();
                if (enemy != null && enemy.currentHealth <= 0)
                {
                    // 标记为即将移除
                    spawnedEnemies[i] = null;
                    removedCount++;
                }
            }
        }
        
        // 如果有敌人被移除，立即生成新敌人
        if (removedCount > 0 && showDebugInfo)
        {
            Debug.Log($"Update中检测到{removedCount}个敌人死亡或销毁");
            CheckAndSpawnEnemies();
        }
        
        // 定期检查敌人数量
        if (Time.time >= nextCheckTime)
        {
            CheckAndSpawnEnemies();
            nextCheckTime = Time.time + checkInterval;
        }
    }

    // 检查并生成敌人
    private void CheckAndSpawnEnemies()
    {
        // 计算需要生成的敌人数量
        int enemiesNeeded = maxEnemyCount - spawnedEnemies.Count;
        
        if (showDebugInfo)
        {
            Debug.Log($"当前敌人数量: {spawnedEnemies.Count}, 需要生成: {enemiesNeeded}");
        }
        
        if (enemiesNeeded <= 0)
        {
            return; // 不需要生成新敌人
        }
        
        // 生成缺少的敌人
        int successCount = 0;
        for (int i = 0; i < enemiesNeeded; i++)
        {
            if (SpawnEnemy())
            {
                successCount++;
            }
        }
        
        if (successCount > 0)
        {
            failedAttempts = 0; // 重置失败计数
            if (showDebugInfo)
            {
                Debug.Log($"成功生成了 {successCount} 个新敌人");
            }
        }
        else
        {
            failedAttempts++;
            if (failedAttempts >= 3 && showDebugInfo)
            {
                Debug.LogWarning($"连续 {failedAttempts} 次未能生成敌人，可能是NavMesh问题或找不到合适位置");
                
                // 如果连续多次失败，尝试直接在玩家周围生成
                if (failedAttempts >= 5)
                {
                    SpawnEnemyFallback();
                }
            }
        }
    }

    // 生成单个敌人，返回是否成功
    private bool SpawnEnemy()
    {
        // 获取可用的敌人预制体
        GameObject prefabToUse = GetEnemyPrefab();
        if (prefabToUse == null)
        {
            if (showDebugInfo) Debug.LogError("无法找到敌人预制体，生成失败");
            return false;
        }
        
        // 生成空中位置，不再尝试找NavMesh点
        Vector3 spawnPoint = GetAirDropPosition();
        
        // 生成敌人在空中位置
        GameObject enemy = Instantiate(prefabToUse, spawnPoint, Quaternion.identity);
        
        // 如果使用的是备份，确保它是激活的
        if (prefabToUse == backupPrefab)
        {
            enemy.SetActive(true);
        }
        
        // 确保敌人标签正确
        enemy.tag = "Enemy";
        
        // 获取RobotEnemy组件并配置
        RobotEnemy robotEnemy = enemy.GetComponent<RobotEnemy>();
        if (robotEnemy != null)
        {
            // 设置目标为玩家
            robotEnemy.target = player;
            
            // 禁用NavMeshAgent，等待落地后再激活
            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = false;
            }
            
            // 确保有刚体组件用于下落
            Rigidbody rb = enemy.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = enemy.AddComponent<Rigidbody>();
            }
            // 确保设置了重力和非运动学模式
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.mass = 10f; // 增加质量确保下落得更快
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            
            // 添加到已生成敌人列表
            spawnedEnemies.Add(enemy);
            
            // 开始监控敌人生命值
            StartCoroutine(MonitorEnemyHealth(enemy, robotEnemy));
            
            // 监控落地
            StartCoroutine(WaitForLanding(enemy, robotEnemy, rb));
            
            if (showDebugInfo)
            {
                Debug.Log($"已在空中坐标 {spawnPoint} 生成新敌人，等待落地");
            }
            
            return true;
        }
        else
        {
            Debug.LogError("生成的敌人预制体没有RobotEnemy组件！");
            Destroy(enemy); // 销毁无效的敌人
            return false;
        }
    }
    
    // 监控敌人落地
    private IEnumerator WaitForLanding(GameObject enemy, RobotEnemy robotEnemy, Rigidbody rb)
    {
        if (enemy == null || robotEnemy == null) yield break;
        
        // 等待敌人下落
        bool isGrounded = false;
        float waitTime = 0f;
        float maxWaitTime = 10f; // 增加最大等待时间
        float checkInterval = 0.2f; // 检查间隔
        float lastLogTime = 0f; // 上次记录日志的时间
        
        // 确保重力是启用的
        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.AddForce(Vector3.down * 10f, ForceMode.Impulse); // 给一个向下的初始力
            
            if (showDebugInfo)
            {
                Debug.Log($"敌人 {enemy.name} 开始下落，初始位置Y: {enemy.transform.position.y}");
            }
        }
        
        while (!isGrounded && waitTime < maxWaitTime && enemy != null)
        {
            waitTime += Time.deltaTime;
            
            // 每隔一段时间检查并记录日志
            if (showDebugInfo && Time.time > lastLogTime + 1.0f)
            {
                if (rb != null)
                {
                    // Debug.Log($"敌人 {enemy.name} 下落中 - 时间: {waitTime:F1}秒, 位置Y: {enemy.transform.position.y:F2}, 速度Y: {rb.velocity.y:F2}");
                }
                lastLogTime = Time.time;
            }
            
            // 检测是否在地面上
            isGrounded = CheckIfGrounded(enemy);
            
            // 如果已着地或超时
            if (isGrounded || waitTime >= maxWaitTime)
            {
                if (enemy != null)
                {
                    // 如果是超时但未检测到着地
                    if (waitTime >= maxWaitTime && !isGrounded)
                    {
                        if (showDebugInfo)
                        {
                            Debug.LogWarning($"敌人 {enemy.name} 下落超时，强制设置为已着地状态。当前位置Y: {enemy.transform.position.y}");
                        }
                    }
                    
                    if (rb != null)
                    {
                        rb.velocity = Vector3.zero; // 停止所有速度
                        rb.isKinematic = true; // 停止物理下落
                    }
                    
                    NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
                    if (agent != null)
                    {
                        // 找到最近的NavMesh位置
                        NavMeshHit hit;
                        if (NavMesh.SamplePosition(enemy.transform.position, out hit, 5f, NavMesh.AllAreas))
                        {
                            // 放置到最近的NavMesh上
                            if (showDebugInfo)
                            {
                                Debug.Log($"敌人 {enemy.name} 放置到NavMesh上，从 {enemy.transform.position.y:F2} 到 {hit.position.y:F2}");
                            }
                            enemy.transform.position = hit.position;
                        }
                        else if (showDebugInfo)
                        {
                            Debug.LogWarning($"敌人 {enemy.name} 无法找到附近的NavMesh位置");
                        }
                        
                        // 启用导航
                        agent.enabled = true;
                        
                        // 指定目标
                        if (robotEnemy != null)
                        {
                            robotEnemy.ForceActivateAgent();
                            if (showDebugInfo)
                            {
                                Debug.Log($"敌人 {enemy.name} 已激活NavMeshAgent，开始追踪玩家");
                            }
                        }
                    }
                }
                break;
            }
            
            yield return new WaitForSeconds(checkInterval); // 使用间隔检查，减少资源消耗
        }
    }
    
    // 检测敌人是否已落地
    private bool CheckIfGrounded(GameObject enemy)
    {
        if (enemy == null) return false;
        
        // 从敌人位置向下发射射线
        RaycastHit hit;
        float rayDistance = 1.0f; // 增加检测距离
        Vector3 rayOrigin = enemy.transform.position + Vector3.up * 0.5f; // 从略高的位置发射
        
        Debug.DrawRay(rayOrigin, Vector3.down * rayDistance, Color.green, 0.2f); // 可视化射线
        
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayDistance))
        {
            if (showDebugInfo)
            {
                // Debug.Log($"敌人 {enemy.name} 检测到地面: {hit.collider.name}，距离: {hit.distance}");
            }
            return true; // 敌人在地面上
        }
        
        // 检查是否速度接近零且在较低位置（已停止移动）
        Rigidbody rb = enemy.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 检查垂直速度和位置
            bool lowVelocity = Mathf.Abs(rb.velocity.y) < 0.2f;
            bool lowHeight = enemy.transform.position.y < 2.0f; // 如果高度较低
            
            if (lowVelocity && lowHeight)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"敌人 {enemy.name} 速度接近零且高度较低，视为已落地。速度: {rb.velocity.y}, 高度: {enemy.transform.position.y}");
                }
                return true;
            }
            
            if (showDebugInfo && rb.velocity.magnitude < 0.5f)
            {
                Debug.Log($"敌人 {enemy.name} 当前速度: {rb.velocity}，位置Y: {enemy.transform.position.y}");
            }
        }
        
        return false;
    }
    
    // 获取空中掉落位置
    private Vector3 GetAirDropPosition()
    {
        // 随机选择方法：环形区域或四个象限随机点
        bool useQuadrantMethod = (Random.value > 0.5f); // 随机选择方法
        
        Vector3 randomPosition;
        
        if (useQuadrantMethod)
        {
            // 方法1：在玩家周围的环形区域内随机位置
            float randomAngle = Random.Range(0f, Mathf.PI * 2);
            float randomDistance = Random.Range(minDistanceFromPlayer, spawnRadius);
            
            float x = Mathf.Cos(randomAngle) * randomDistance;
            float z = Mathf.Sin(randomAngle) * randomDistance;
            
            randomPosition = player.position + new Vector3(x, 0, z);
        }
        else
        {
            // 方法2：在玩家四周的象限位置生成
            int quadrant = Random.Range(0, 4); // 0=前方, 1=右侧, 2=后方, 3=左侧
            
            Vector3 direction = Vector3.zero;
            switch (quadrant)
            {
                case 0: direction = player.forward; break;
                case 1: direction = player.right; break;
                case 2: direction = -player.forward; break;
                case 3: direction = -player.right; break;
            }
            
            // 随机距离
            float distance = Random.Range(minDistanceFromPlayer, spawnRadius);
            
            // 添加一些随机偏移
            Vector3 offset = new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
            
            randomPosition = player.position + direction * distance + offset;
        }
        
        // 抬高位置（从空中掉落）
        float dropHeight = Random.Range(10f, 15f);
        randomPosition.y += dropHeight;
        
        if (showDebugInfo && showSpawnPoints)
        {
            // 创建一个临时标记
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.transform.position = randomPosition;
            marker.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            marker.GetComponent<Renderer>().material.color = Color.cyan; // 使用青色区分空投点
            marker.name = "AirDropMarker_TEMP";
            Destroy(marker.GetComponent<SphereCollider>()); // 移除碰撞体
            Destroy(marker, 2f); // 确保2秒后销毁
        }
        
        return randomPosition;
    }

    // 最后的备用生成方法，在连续失败后使用
    private void SpawnEnemyFallback()
    {
        if (showDebugInfo)
        {
            Debug.Log("使用空投备用方法生成敌人");
        }
        
        // 直接在玩家上方生成
        Vector3 airDropPosition = player.position + (player.forward * -5f) + new Vector3(0, 15f, 0);
        
        // 生成敌人
        GameObject enemy = Instantiate(robotEnemyPrefab, airDropPosition, Quaternion.identity);
        enemy.tag = "Enemy";
        
        // 确保有刚体组件用于下落
        Rigidbody rb = enemy.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = enemy.AddComponent<Rigidbody>();
        }
        // 确保设置了重力和非运动学模式
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.mass = 10f; // 增加质量确保下落得更快
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        
        // 获取组件
        RobotEnemy robotEnemy = enemy.GetComponent<RobotEnemy>();
        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        
        if (agent != null)
        {
            agent.enabled = false; // 先禁用导航
        }
        
        if (robotEnemy != null)
        {
            robotEnemy.target = player;
            spawnedEnemies.Add(enemy);
            
            // 开始监控敌人生命值
            StartCoroutine(MonitorEnemyHealth(enemy, robotEnemy));
            
            // 监控落地
            StartCoroutine(WaitForLanding(enemy, robotEnemy, rb));
            
            if (showDebugInfo)
            {
                Debug.Log($"使用备用方法成功空投敌人");
            }
        }
    }

    // 查找有效的生成点 - 不再使用，改为空投
    private bool FindValidSpawnPoint(out Vector3 spawnPoint)
    {
        // 直接获取空投位置
        spawnPoint = GetAirDropPosition();
        return true;
    }
    
    // 在场景视图中可视化生成区域
    private void OnDrawGizmosSelected()
    {
        if (player == null) return;
        
        // 绘制生成半径
        Gizmos.color = new Color(1, 0, 0, 0.2f); // 半透明红色
        Gizmos.DrawSphere(player.position, spawnRadius);
        
        // 绘制最小距离
        Gizmos.color = new Color(0, 0, 1, 0.2f); // 半透明蓝色
        Gizmos.DrawSphere(player.position, minDistanceFromPlayer);
    }
} 