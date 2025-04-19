using UnityEngine;

/// <summary>
/// 场景音频配置 - 定义场景中各音轨的音量和淡变设置
/// </summary>
[CreateAssetMenu(fileName = "NewSceneAudioConfig", menuName = "Audio/Scene Audio Config", order = 1)]
public class SceneAudioConfig : ScriptableObject
{
    [Tooltip("场景名称 - 必须与场景文件名完全匹配")]
    public string sceneName;
    
    [Tooltip("场景描述（可选）")]
    [TextArea(2, 4)]
    public string description;
    
    [Header("音轨设置")]
    [Tooltip("各音轨的音量和淡变设置")]
    public TrackSettings[] trackSettings = new TrackSettings[5];
    
    // 在编辑器中初始化默认值
    private void OnValidate()
    {
        // 确保始终有5个音轨设置
        if (trackSettings == null || trackSettings.Length != 5)
        {
            trackSettings = new TrackSettings[5];
        }
        
        // 确保音量值在有效范围内
        for (int i = 0; i < trackSettings.Length; i++)
        {
            trackSettings[i].volume = Mathf.Clamp01(trackSettings[i].volume);
        }
    }
}

/// <summary>
/// 单个音轨的设置
/// </summary>
[System.Serializable]
public class TrackSettings
{
    [Tooltip("音轨在场景中的目标音量")]
    [Range(0f, 1f)]
    public float volume = 0f;
    
    [Tooltip("淡入淡出时间（秒），0表示使用默认值")]
    public float fadeTime = 0f;
} 