using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 持久化音频管理器 - 负责在场景切换中维持背景音乐的连续播放与淡入淡出
/// </summary>
public class PersistentAudioManager : MonoBehaviour
{
    public static PersistentAudioManager Instance { get; private set; }

    [Header("音轨设置")]
    [Tooltip("所有音轨使用的主音量乘数")]
    public float masterVolume = 1.0f;
    
    [Tooltip("五条音轨的音频剪辑")]
    public AudioClip[] musicTracks = new AudioClip[5];
    
    [Tooltip("初始音轨音量")]
    [Range(0f, 1f)]
    public float[] initialTrackVolumes = new float[5] { 1f, 0f, 0f, 0f, 0f };
    
    [Header("淡变设置")]
    [Tooltip("默认淡入淡出时间（秒）")]
    public float defaultFadeTime = 2.0f;
    [Tooltip("状态变化的默认淡入淡出时间（秒）")]
    public float defaultStateFadeTime = 1.0f;
    
    [Header("场景配置")]
    [Tooltip("场景切换时自动应用场景配置")]
    public bool autoApplySceneSettings = true;
    
    [Tooltip("各场景的音频配置")]
    public SceneAudioConfig[] sceneConfigs;
    
    [Header("状态配置")]
    [Tooltip("各游戏状态的音频配置")]
    public StateAudioConfig[] stateConfigs;

    // 五条音轨的AudioSource组件
    private AudioSource[] audioSources = new AudioSource[5];
    
    // 当前活跃的淡变协程
    private Dictionary<int, Coroutine> activeCoroutines = new Dictionary<int, Coroutine>();
    
    // 当前场景名称
    private string currentSceneName;
    
    // 当前基础音轨音量（场景配置决定的）
    private float[] baseTrackVolumes = new float[5];
    
    // 当前活跃的状态列表（按优先级排序）
    private List<ActiveState> activeStates = new List<ActiveState>();
    
    // 状态类定义
    private class ActiveState
    {
        public string stateId;
        public float startTime;
        public StateAudioConfig config;
    }

    // 初始化
    private void Awake()
    {
        // 单例模式实现
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 初始化音频源
        InitializeAudioSources();
        
        // 初始化基础音量
        for (int i = 0; i < baseTrackVolumes.Length; i++)
        {
            baseTrackVolumes[i] = initialTrackVolumes[i];
        }
        
        // 注册场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // 获取当前场景名称
        currentSceneName = SceneManager.GetActiveScene().name;
    }
    
    private void OnDestroy()
    {
        // 取消事件订阅
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    // 初始化音频源
    private void InitializeAudioSources()
    {
        // 创建空的子对象来容纳各音轨
        GameObject audioContainer = new GameObject("Audio Tracks");
        audioContainer.transform.SetParent(transform);
        
        // 为每条音轨创建AudioSource
        for (int i = 0; i < 5; i++)
        {
            GameObject trackObj = new GameObject($"Track {i+1}");
            trackObj.transform.SetParent(audioContainer.transform);
            
            AudioSource source = trackObj.AddComponent<AudioSource>();
            audioSources[i] = source;
            
            // 基本设置
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f; // 2D音效
            
            // 如果有预配置的音频剪辑
            if (i < musicTracks.Length && musicTracks[i] != null)
            {
                source.clip = musicTracks[i];
            }
            
            // 设置初始音量
            source.volume = initialTrackVolumes[i] * masterVolume;
        }
    }
    
    // 开始播放所有音轨
    public void StartAllTracks()
    {
        for (int i = 0; i < audioSources.Length; i++)
        {
            if (audioSources[i].clip != null && !audioSources[i].isPlaying)
            {
                audioSources[i].Play();
            }
        }
        
        Debug.Log("所有音轨已开始播放");
    }
    
    // 场景加载回调
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!autoApplySceneSettings) return;
        
        currentSceneName = scene.name;
        ApplySceneAudioSettings(currentSceneName);
    }
    
    // 根据场景名称应用音频设置
    public void ApplySceneAudioSettings(string sceneName)
    {
        if (sceneConfigs == null) return;
        
        // 查找匹配的场景配置
        SceneAudioConfig config = null;
        foreach (var conf in sceneConfigs)
        {
            if (conf != null && conf.sceneName == sceneName)
            {
                config = conf;
                break;
            }
        }
        
        if (config == null)
        {
            Debug.LogWarning($"没有找到场景 '{sceneName}' 的音频配置");
            return;
        }
        
        // 应用配置
        for (int i = 0; i < 5; i++)
        {
            if (i < config.trackSettings.Length)
            {
                // 保存基础音量
                baseTrackVolumes[i] = config.trackSettings[i].volume;
                
                // 计算最终音量（考虑状态调整）
                float finalVolume = CalculateFinalVolume(i, baseTrackVolumes[i]);
                float fadeTime = config.trackSettings[i].fadeTime;
                
                // 如果淡变时间为0，使用默认值
                if (fadeTime <= 0)
                {
                    fadeTime = defaultFadeTime;
                }
                
                // 应用音量变化
                FadeTrackVolume(i, finalVolume, fadeTime);
            }
        }
        
        Debug.Log($"已应用场景 '{sceneName}' 的音频配置");
    }
    
    // 激活状态
    public void ActivateState(string stateId, float fadeTime = -1)
    {
        Debug.Log($"[PersistentAudioManager] 准备激活状态: '{stateId}', fadeTime={fadeTime}");
        
        if (stateConfigs == null) 
        {
            Debug.LogError($"[PersistentAudioManager] stateConfigs为空，无法激活状态");
            return;
        }
        
        // 查找状态配置
        StateAudioConfig config = FindStateConfig(stateId);
        if (config == null)
        {
            Debug.LogWarning($"[PersistentAudioManager] 未找到状态 '{stateId}' 的音频配置");
            return;
        }
        
        // 检查是否已激活该状态
        foreach (var state in activeStates)
        {
            if (state.stateId == stateId)
            {
                Debug.Log($"[PersistentAudioManager] 状态 '{stateId}' 已激活，忽略");
                return;
            }
        }
        
        // 创建新状态
        ActiveState newState = new ActiveState
        {
            stateId = stateId,
            startTime = Time.time,
            config = config
        };
        
        // 添加状态
        activeStates.Add(newState);
        
        // 输出当前激活的所有状态
        string activeStatesList = string.Join(", ", GetActiveStates());
        Debug.Log($"[PersistentAudioManager] 当前激活的所有状态: {activeStatesList}");
        
        // 重新计算并应用所有音轨的音量
        RecalculateAndApplyAllVolumes(fadeTime < 0 ? defaultStateFadeTime : fadeTime);
        
        Debug.Log($"[PersistentAudioManager] 已激活状态 '{stateId}'");
    }
    
    // 停用状态
    public void DeactivateState(string stateId, float fadeTime = -1)
    {
        Debug.Log($"[PersistentAudioManager] 准备停用状态: '{stateId}', fadeTime={fadeTime}");
        
        bool removed = false;
        
        // 移除指定状态
        for (int i = activeStates.Count - 1; i >= 0; i--)
        {
            if (activeStates[i].stateId == stateId)
            {
                Debug.Log($"[PersistentAudioManager] 找到并移除状态: '{stateId}', 位置索引={i}");
                activeStates.RemoveAt(i);
                removed = true;
                break;
            }
        }
        
        if (removed)
        {
            // 输出当前激活的所有状态
            string activeStatesList = string.Join(", ", GetActiveStates());
            Debug.Log($"[PersistentAudioManager] 当前剩余激活的状态: {activeStatesList}");
            
            // 重新计算并应用所有音轨的音量
            RecalculateAndApplyAllVolumes(fadeTime < 0 ? defaultStateFadeTime : fadeTime);
            Debug.Log($"[PersistentAudioManager] 已停用状态 '{stateId}'");
        }
        else
        {
            Debug.LogWarning($"[PersistentAudioManager] 未找到要停用的状态 '{stateId}'");
        }
    }
    
    // 停用所有状态
    public void DeactivateAllStates(float fadeTime = -1)
    {
        if (activeStates.Count > 0)
        {
            activeStates.Clear();
            RecalculateAndApplyAllVolumes(fadeTime < 0 ? defaultStateFadeTime : fadeTime);
            Debug.Log("已停用所有状态");
        }
    }
    
    // 重新计算并应用所有音轨的音量
    private void RecalculateAndApplyAllVolumes(float fadeTime)
    {
        Debug.Log($"[PersistentAudioManager] 开始重新计算所有音轨音量，淡变时间={fadeTime}秒");
        
        for (int i = 0; i < baseTrackVolumes.Length; i++)
        {
            float finalVolume = CalculateFinalVolume(i, baseTrackVolumes[i]);
            Debug.Log($"[PersistentAudioManager] 音轨{i}：基础音量={baseTrackVolumes[i]}，计算后音量={finalVolume}");
            FadeTrackVolume(i, finalVolume, fadeTime);
        }
        
        Debug.Log("[PersistentAudioManager] 所有音轨音量已重新计算并开始淡变");
    }
    
    // 计算最终音量（使用最后激活的状态的音量设置）
    private float CalculateFinalVolume(int trackIndex, float baseVolume)
    {
        if (activeStates.Count == 0)
        {
            Debug.Log($"[PersistentAudioManager] 音轨{trackIndex}：无活跃状态，使用基础音量={baseVolume}");
            return baseVolume;
        }
            
        // 使用最后激活的状态
        ActiveState state = activeStates[activeStates.Count - 1];
        
        if (trackIndex < state.config.trackSettings.Length)
        {
            StateTrackSettings settings = state.config.trackSettings[trackIndex];
            Debug.Log($"[PersistentAudioManager] 音轨{trackIndex}：应用状态'{state.stateId}'的音量={settings.volume}");
            return settings.volume; // 直接使用设置的绝对音量
        }
        
        Debug.Log($"[PersistentAudioManager] 音轨{trackIndex}：状态'{state.stateId}'没有该音轨设置，使用基础音量={baseVolume}");
        return baseVolume;
    }
    
    // 淡变单个音轨的音量
    public void FadeTrackVolume(int trackIndex, float targetVolume, float fadeTime)
    {
        if (trackIndex < 0 || trackIndex >= audioSources.Length)
        {
            Debug.LogError($"[PersistentAudioManager] 音轨索引 {trackIndex} 超出范围");
            return;
        }
        
        // 如果还未播放，先播放
        AudioSource source = audioSources[trackIndex];
        if (source.clip != null && !source.isPlaying)
        {
            source.volume = 0f;
            source.Play();
            Debug.Log($"[PersistentAudioManager] 音轨{trackIndex}开始播放，初始音量=0");
        }
        
        // 停止该音轨上正在进行的淡变
        if (activeCoroutines.ContainsKey(trackIndex))
        {
            StopCoroutine(activeCoroutines[trackIndex]);
            activeCoroutines.Remove(trackIndex);
            Debug.Log($"[PersistentAudioManager] 停止音轨{trackIndex}上正在进行的淡变");
        }
        
        Debug.Log($"[PersistentAudioManager] 开始音轨{trackIndex}的淡变：当前音量={source.volume}，目标音量={targetVolume}，时间={fadeTime}秒");
        
        // 开始新的淡变
        Coroutine fadeCoroutine = StartCoroutine(FadeVolumeCoroutine(source, targetVolume, fadeTime));
        activeCoroutines[trackIndex] = fadeCoroutine;
    }
    
    // 淡变协程
    private IEnumerator FadeVolumeCoroutine(AudioSource source, float targetVolume, float duration)
    {
        float startVolume = source.volume;
        float startTime = Time.time;
        float endTime = startTime + duration;
        
        Debug.Log($"[PersistentAudioManager] 淡变协程开始：从{startVolume}到{targetVolume * masterVolume}，持续{duration}秒");
        
        while (Time.time < endTime)
        {
            float t = (Time.time - startTime) / duration;
            source.volume = Mathf.Lerp(startVolume, targetVolume * masterVolume, t);
            yield return null;
        }
        
        // 确保最终音量精确
        source.volume = targetVolume * masterVolume;
        Debug.Log($"[PersistentAudioManager] 淡变完成：最终音量={source.volume}");
    }
    
    // 设置主音量
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        
        // 应用到所有音轨
        for (int i = 0; i < audioSources.Length; i++)
        {
            if (audioSources[i] != null)
            {
                float normalizedVolume = audioSources[i].volume / (masterVolume > 0 ? masterVolume : 1);
                audioSources[i].volume = normalizedVolume * masterVolume;
            }
        }
    }
    
    // 立即设置单个音轨的音量（无淡变）
    public void SetTrackVolume(int trackIndex, float volume)
    {
        if (trackIndex < 0 || trackIndex >= audioSources.Length) return;
        
        volume = Mathf.Clamp01(volume);
        audioSources[trackIndex].volume = volume * masterVolume;
    }
    
    // 设置单个音轨的音频
    public void SetTrackClip(int trackIndex, AudioClip clip, bool playImmediately = true)
    {
        if (trackIndex < 0 || trackIndex >= audioSources.Length) return;
        
        AudioSource source = audioSources[trackIndex];
        bool wasPlaying = source.isPlaying;
        
        source.Stop();
        source.clip = clip;
        
        if (clip != null && (wasPlaying || playImmediately))
        {
            source.Play();
        }
    }
    
    // 根据ID查找状态配置
    private StateAudioConfig FindStateConfig(string stateId)
    {
        if (stateConfigs == null) return null;
        
        foreach (var conf in stateConfigs)
        {
            if (conf != null && conf.stateId == stateId)
            {
                return conf;
            }
        }
        
        return null;
    }
    
    // 获取当前活跃的状态
    public string[] GetActiveStates()
    {
        string[] states = new string[activeStates.Count];
        for (int i = 0; i < activeStates.Count; i++)
        {
            states[i] = activeStates[i].stateId;
        }
        return states;
    }
    
    // 检查状态是否活跃
    public bool IsStateActive(string stateId)
    {
        Debug.Log($"[PersistentAudioManager] 检查状态 '{stateId}' 是否活跃，当前活跃状态数量={activeStates.Count}");
        
        foreach (var state in activeStates)
        {
            if (state.stateId == stateId)
            {
                Debug.Log($"[PersistentAudioManager] 状态 '{stateId}' 已激活");
                return true;
            }
        }
        
        Debug.Log($"[PersistentAudioManager] 状态 '{stateId}' 未激活");
        return false;
    }
    
    // 临时调整音量（如特效、UI音效等）
    public void AdjustVolumeTemporarily(int trackIndex, float targetVolume, float duration, float returnDuration)
    {
        StartCoroutine(TemporaryVolumeAdjustment(trackIndex, targetVolume, duration, returnDuration));
    }
    
    // 临时音量调整协程
    private IEnumerator TemporaryVolumeAdjustment(int trackIndex, float targetVolume, float duration, float returnDuration)
    {
        // 保存当前音量
        float originalVolume = audioSources[trackIndex].volume / masterVolume;
        
        // 淡变到目标音量
        FadeTrackVolume(trackIndex, targetVolume, duration);
        
        // 等待持续时间
        yield return new WaitForSeconds(duration);
        
        // 返回到原始音量
        float finalVolume = CalculateFinalVolume(trackIndex, baseTrackVolumes[trackIndex]);
        FadeTrackVolume(trackIndex, finalVolume, returnDuration);
    }
    
    // 获取当前音频源数组
    public AudioSource[] GetAudioSources()
    {
        return audioSources;
    }
} 