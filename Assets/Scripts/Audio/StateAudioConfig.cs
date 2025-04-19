using UnityEngine;

/// <summary>
/// 状态音频配置 - 定义游戏状态的各音轨调整
/// </summary>
[CreateAssetMenu(fileName = "NewStateAudioConfig", menuName = "Audio/State Audio Config", order = 2)]
public class StateAudioConfig : ScriptableObject
{
    [Tooltip("状态ID - 唯一标识符")]
    public string stateId;
    
    [Tooltip("状态名称 - 便于编辑器中识别")]
    public string stateName;
    
    [Tooltip("状态描述（可选）")]
    [TextArea(2, 4)]
    public string description;
    
    [Header("音轨设置")]
    [Tooltip("状态激活时各音轨的音量设置")]
    public StateTrackSettings[] trackSettings = new StateTrackSettings[5];
    
    // 在编辑器中初始化默认值
    private void OnValidate()
    {
        // 确保始终有5个音轨设置
        if (trackSettings == null || trackSettings.Length != 5)
        {
            trackSettings = new StateTrackSettings[5];
        }
        
        // 确保值在有效范围内
        for (int i = 0; i < trackSettings.Length; i++)
        {
            trackSettings[i].volume = Mathf.Clamp(trackSettings[i].volume, 0f, 1f);
        }
    }
}

/// <summary>
/// 状态下单个音轨的设置
/// </summary>
[System.Serializable]
public class StateTrackSettings
{
    [Tooltip("音量值 (0-1)")]
    [Range(0f, 1f)]
    public float volume = 0f;
    
    [Tooltip("淡入淡出时间（秒），0表示使用默认值")]
    public float fadeTime = 0f;
} 