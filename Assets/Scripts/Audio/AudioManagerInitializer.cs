using UnityEngine;

/// <summary>
/// 音频管理器初始化器 - 确保在游戏开始时PersistentAudioManager正确设置
/// 将此脚本放在第一个场景的任何对象上
/// </summary>
public class AudioManagerInitializer : MonoBehaviour
{
    [Tooltip("如果为true，将自动创建一个PersistentAudioManager实例")]
    public bool createManagerIfNotExists = true;
    
    [Tooltip("PersistentAudioManager预制体，如果需要创建")]
    public GameObject audioManagerPrefab;
    
    [Tooltip("如果为true，将在初始化后自动开始播放音轨")]
    public bool autoPlayOnStart = true;
    
    private void Awake()
    {
        // 检查是否已存在PersistentAudioManager
        if (PersistentAudioManager.Instance == null && createManagerIfNotExists)
        {
            GameObject managerObj;
            
            // 如果提供了预制体，实例化预制体
            if (audioManagerPrefab != null)
            {
                managerObj = Instantiate(audioManagerPrefab);
                managerObj.name = "PersistentAudioManager";
            }
            else
            {
                // 创建新对象
                managerObj = new GameObject("PersistentAudioManager");
                managerObj.AddComponent<PersistentAudioManager>();
                Debug.Log("已创建新的PersistentAudioManager实例");
            }
        }
    }
    
    private void Start()
    {
        // 确保音频管理器已经初始化
        if (PersistentAudioManager.Instance != null && autoPlayOnStart)
        {
            PersistentAudioManager.Instance.StartAllTracks();
        }
    }
} 