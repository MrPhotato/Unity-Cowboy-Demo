using UnityEngine;
using UnityEngine.AI;
using StarterAssets;

// 这个脚本用于自动设置机器人敌人的所有必需组件
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(StarterAssetsInputs))]
public class SetupRobot : MonoBehaviour
{
    // 指定动画控制器资源
    public RuntimeAnimatorController robotAnimatorController;
    
    private void Awake()
    {
        // 确保有Animator组件
        Animator animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("机器人缺少Animator组件，自动添加...");
            animator = gameObject.AddComponent<Animator>();
        }
        
        // 设置动画控制器
        if (animator.runtimeAnimatorController == null)
        {
            if (robotAnimatorController != null)
            {
                animator.runtimeAnimatorController = robotAnimatorController;
                Debug.Log("已设置机器人动画控制器");
            }
            else
            {
                Debug.LogError("请在Inspector中设置robotAnimatorController!");
            }
        }
        
        // 确保有CharacterController组件
        CharacterController characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogWarning("机器人缺少CharacterController组件，自动添加...");
            characterController = gameObject.AddComponent<CharacterController>();
            
            // 设置合理的默认值
            characterController.radius = 0.5f;
            characterController.height = 2f;
            characterController.center = new Vector3(0, 1f, 0);
        }
        
        // 确保有StarterAssetsInputs组件
        StarterAssetsInputs inputsController = GetComponent<StarterAssetsInputs>();
        if (inputsController == null)
        {
            Debug.LogWarning("机器人缺少StarterAssetsInputs组件，自动添加...");
            inputsController = gameObject.AddComponent<StarterAssetsInputs>();
        }
        
        // 确保有NavMeshAgent组件
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogWarning("机器人缺少NavMeshAgent组件，自动添加...");
            agent = gameObject.AddComponent<NavMeshAgent>();
            
            // 设置合理的默认值
            agent.radius = 0.5f;
            agent.height = 2f;
            agent.speed = 3.5f;
            agent.angularSpeed = 120f;
            agent.acceleration = 8f;
        }
        
        // 移除ThirdPersonController
        ThirdPersonController thirdPersonCtrl = GetComponent<ThirdPersonController>();
        if (thirdPersonCtrl != null)
        {
            Debug.LogWarning("移除不必要的ThirdPersonController组件");
            Destroy(thirdPersonCtrl);
        }
        
        // 确保有RobotEnemy组件
        RobotEnemy robotEnemy = GetComponent<RobotEnemy>();
        if (robotEnemy == null)
        {
            Debug.LogWarning("机器人缺少RobotEnemy组件，自动添加...");
            robotEnemy = gameObject.AddComponent<RobotEnemy>();
            
            // 查找玩家作为目标
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                robotEnemy.target = player.transform;
            }
            else
            {
                Debug.LogError("找不到Player标签的对象，请确保您的玩家设置了Player标签！");
            }
        }
        
        // 检查Camera是否具有MainCamera标签
        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.CompareTag("MainCamera") == false)
        {
            Debug.LogWarning("场景主相机未设置MainCamera标签，自动添加...");
            mainCamera.gameObject.tag = "MainCamera";
        }
        else if (mainCamera == null)
        {
            Debug.LogError("场景中没有主相机！确保场景中有一个具有MainCamera标签的相机");
        }
        
        Debug.Log("机器人设置完成，所有必需组件已确认");
    }
    
    // 在Inspector中添加自动设置按钮
    [ContextMenu("自动设置机器人")]
    public void AutoSetupRobot()
    {
        // 触发Awake函数中的设置逻辑
        Awake();
        
        // 根据需要进行其他手动设置
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            // 确保代理在NavMesh上
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                Debug.Log("已将机器人放置到NavMesh上");
            }
            else
            {
                Debug.LogError("无法找到附近的NavMesh！请烘焙NavMesh或移动机器人");
            }
        }
        
        Debug.Log("机器人手动设置完成");
    }
} 