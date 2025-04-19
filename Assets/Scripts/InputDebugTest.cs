using UnityEngine;

// 这个脚本不会受角色控制器影响，是独立的输入测试脚本
public class InputDebugTest : MonoBehaviour
{
    private float lastLogTime = 0f;
    public bool logAllKeys = true; // 是否记录所有按键
    public KeyCode[] keysToTest = new KeyCode[] { 
        KeyCode.F, KeyCode.E, KeyCode.Space, KeyCode.Return 
    };

    private void Start()
    {
        // 确认脚本已经启动
        Debug.Log("[InputTest] 输入测试脚本已启动! 请按F、E、空格或回车键测试输入");
    }

    private void Update()
    {
        // 每3秒提醒用户测试存在
        if (Time.time > lastLogTime + 3f)
        {
            lastLogTime = Time.time;
            Debug.Log("[InputTest] 等待输入测试...");
        }

        // 检测指定按键
        foreach (KeyCode key in keysToTest)
        {
            if (Input.GetKeyDown(key))
            {
                Debug.Log("[InputTest] 检测到按键: " + key);
            }
        }

        // 检测任何按键
        if (logAllKeys && Input.anyKeyDown)
        {
            // 尝试记录用户按下的任何键
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    Debug.Log("[InputTest] 按下了: " + key);
                }
            }
        }
    }

    // 确保此脚本优先于其他脚本执行
    private void Awake()
    {
        // 将脚本优先级设为非常高
        Debug.Log("[InputTest] 输入测试脚本已唤醒，优先级设定为高");
    }
} 