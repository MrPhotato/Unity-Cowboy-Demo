using UnityEngine;

/// <summary>
/// 音频状态管理器 - 提供静态方法让游戏中各系统便捷地触发音频状态
/// </summary>
public static class AudioStateManager
{
    // 预定义的状态ID常量
    public static class States
    {
        // 战斗状态
        public const string Combat = "combat";
        public const string CombatIntense = "combat_intense";
        
        // 探索状态
        public const string Stealth = "stealth";
        public const string Suspense = "suspense";
        
        // 成就与进度状态
        public const string Achievement = "achievement";
        public const string LevelUp = "level_up";
        
        // 区域状态
        public const string Danger = "danger_area";
        public const string Safe = "safe_area";
        
        // 玩家状态
        public const string Discovered = "discovered"; // 被发现
        public const string Excited = "excited";      // 情绪高涨
        public const string Highlight = "highlight";  // 高光时刻
    }
    
    /// <summary>
    /// 激活指定状态
    /// </summary>
    /// <param name="stateId">状态ID</param>
    /// <param name="fadeTime">淡变时间</param>
    public static void ActivateState(string stateId, float fadeTime = -1)
    {
        Debug.Log($"[AudioStateManager] 正在尝试激活状态: '{stateId}', fadeTime={fadeTime}");
        
        if (PersistentAudioManager.Instance != null)
        {
            PersistentAudioManager.Instance.ActivateState(stateId, fadeTime);
        }
        else
        {
            Debug.LogWarning("[AudioStateManager] 尝试激活音频状态，但PersistentAudioManager不存在");
        }
    }
    
    /// <summary>
    /// 停用指定状态
    /// </summary>
    /// <param name="stateId">状态ID</param>
    /// <param name="fadeTime">淡变时间</param>
    public static void DeactivateState(string stateId, float fadeTime = -1)
    {
        if (PersistentAudioManager.Instance != null)
        {
            PersistentAudioManager.Instance.DeactivateState(stateId, fadeTime);
        }
    }
    
    /// <summary>
    /// 停用所有状态
    /// </summary>
    /// <param name="fadeTime">淡变时间</param>
    public static void DeactivateAllStates(float fadeTime = -1)
    {
        if (PersistentAudioManager.Instance != null)
        {
            PersistentAudioManager.Instance.DeactivateAllStates(fadeTime);
        }
    }
    
    /// <summary>
    /// 检查状态是否活跃
    /// </summary>
    /// <param name="stateId">状态ID</param>
    /// <returns>状态是否活跃</returns>
    public static bool IsStateActive(string stateId)
    {
        Debug.Log($"[AudioStateManager] 检查状态 '{stateId}' 是否活跃");
        
        if (PersistentAudioManager.Instance != null)
        {
            bool isActive = PersistentAudioManager.Instance.IsStateActive(stateId);
            Debug.Log($"[AudioStateManager] 状态 '{stateId}' 当前状态: {(isActive ? "活跃" : "未活跃")}");
            return isActive;
        }
        
        Debug.LogWarning($"[AudioStateManager] PersistentAudioManager实例不存在，无法检查状态");
        return false;
    }
    
    /// <summary>
    /// 激活战斗状态
    /// </summary>
    /// <param name="intense">是否是激烈战斗</param>
    public static void ActivateCombat(bool intense = false)
    {
        ActivateState(intense ? States.CombatIntense : States.Combat);
    }
    
    /// <summary>
    /// 停用战斗状态
    /// </summary>
    public static void DeactivateCombat()
    {
        DeactivateState(States.Combat);
        DeactivateState(States.CombatIntense);
    }
    
    /// <summary>
    /// 播放等级提升音频效果
    /// </summary>
    /// <param name="duration">持续时间</param>
    public static void PlayLevelUp(float duration = 3.0f)
    {
        ActivateState(States.LevelUp, 0.3f);
        
        // 在duration时间后自动停用状态
        if (PersistentAudioManager.Instance != null)
        {
            PersistentAudioManager.Instance.StartCoroutine(
                DeactivateAfterDelay(States.LevelUp, duration, 1.0f));
        }
    }
    
    /// <summary>
    /// 延迟停用状态的协程
    /// </summary>
    private static System.Collections.IEnumerator DeactivateAfterDelay(string stateId, float delay, float fadeTime)
    {
        yield return new WaitForSeconds(delay);
        DeactivateState(stateId, fadeTime);
    }
    
    /// <summary>
    /// 延迟从一个状态切换到另一个状态的协程
    /// </summary>
    private static System.Collections.IEnumerator SwitchStateAfterDelay(string fromStateId, string toStateId, float delay, float fadeTime)
    {
        yield return new WaitForSeconds(delay);
        DeactivateState(fromStateId, fadeTime);
        ActivateState(toStateId, fadeTime);
        Debug.Log($"[AudioStateManager] 状态切换：从'{fromStateId}'切换到'{toStateId}'");
    }
    
    /// <summary>
    /// 清除所有状态并设置单一状态
    /// </summary>
    /// <param name="stateId">要设置的状态ID</param>
    /// <param name="fadeTime">淡变时间</param>
    public static void SetSingleState(string stateId, float fadeTime = -1)
    {
        Debug.Log($"[AudioStateManager] 清除所有状态并设置单一状态: '{stateId}'");
        
        if (PersistentAudioManager.Instance != null)
        {
            // 先停用所有状态
            PersistentAudioManager.Instance.DeactivateAllStates(fadeTime);
            
            // 然后激活指定状态
            PersistentAudioManager.Instance.ActivateState(stateId, fadeTime);
        }
        else
        {
            Debug.LogWarning("[AudioStateManager] 尝试设置单一音频状态，但PersistentAudioManager不存在");
        }
    }
    
    /// <summary>
    /// 激活高光时刻状态
    /// </summary>
    /// <param name="fadeTime">淡变时间</param>
    public static void ActivateHighlight(float fadeTime = 0.5f)
    {
        Debug.Log("[AudioStateManager] 激活高光时刻音频状态");
        ActivateState(States.Highlight, fadeTime);
    }
    
    /// <summary>
    /// 激活高光时刻状态，并在指定时间后自动停用
    /// </summary>
    /// <param name="duration">持续时间</param>
    /// <param name="fadeTime">淡变时间</param>
    /// <param name="returnToState">结束后返回的状态，默认为null（不自动切换）</param>
    public static void PlayHighlight(float duration = 5.0f, float fadeTime = 0.5f, string returnToState = null)
    {
        SetSingleState(States.Highlight, fadeTime);
        
        // 在duration时间后自动停用状态
        if (PersistentAudioManager.Instance != null)
        {
            if (returnToState != null)
            {
                // 如果指定了返回状态，使用returnToState
                PersistentAudioManager.Instance.StartCoroutine(
                    SwitchStateAfterDelay(States.Highlight, returnToState, duration, fadeTime));
            }
            else
            {
                // 否则只停用高光状态
                PersistentAudioManager.Instance.StartCoroutine(
                    DeactivateAfterDelay(States.Highlight, duration, fadeTime));
            }
        }
    }
    
    /// <summary>
    /// 停用高光时刻状态
    /// </summary>
    /// <param name="fadeTime">淡变时间</param>
    public static void DeactivateHighlight(float fadeTime = 0.5f)
    {
        Debug.Log("[AudioStateManager] 停用高光时刻音频状态");
        DeactivateState(States.Highlight, fadeTime);
    }
} 