using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PuzzleManager : MonoBehaviour
{
    public GameObject[] puzzlePieces; // 拼图块数组
    public Transform[] slots; // 插槽数组
    public Transform[] homeSlots; // 原始位置插槽数组
    private bool isGameCompleted = false;

    // 视频播放器引用 (在Inspector中分配)
    public UnityEngine.Video.VideoPlayer completionVideo;
    
    // 视频Canvas引用
    public Canvas videoCanvas;
    
    // 视频播放后场景切换
    public string nextSceneName = "Demo"; // 视频播放后要切换的场景名称
    public float videoPlayTime = 4f; // 视频播放时间，4秒后切换场景
    private bool isSceneTransitioning = false;

    // 初始化
    void Start()
    {
        // 如果拼图块未分配，则自动查找
        if (puzzlePieces == null || puzzlePieces.Length == 0)
        {
            puzzlePieces = new GameObject[8];
            for (int i = 1; i <= 8; i++)
            {
                puzzlePieces[i-1] = GameObject.Find("PuzzlePiece" + i);
                if (puzzlePieces[i-1] == null)
                {
                    Debug.LogError("找不到拼图块: PuzzlePiece" + i);
                }
            }
        }

        // 如果插槽未分配，则自动查找
        if (slots == null || slots.Length == 0)
        {
            slots = new Transform[8];
            for (int i = 1; i <= 8; i++)
            {
                GameObject slotObj = GameObject.Find("Slot" + i);
                if (slotObj != null)
                {
                    slots[i-1] = slotObj.transform;
                }
                else
                {
                    Debug.LogError("找不到插槽: Slot" + i);
                }
            }
        }
        
        // 如果原始位置插槽未分配，则自动查找
        if (homeSlots == null || homeSlots.Length == 0)
        {
            homeSlots = new Transform[8];
            for (int i = 1; i <= 8; i++)
            {
                GameObject homeSlotObj = GameObject.Find("HomeSlot" + i);
                if (homeSlotObj != null)
                {
                    homeSlots[i-1] = homeSlotObj.transform;
                }
                else
                {
                    Debug.LogError("找不到Home插槽: HomeSlot" + i);
                }
            }
        }
        
        // 初始隐藏视频Canvas
        HideVideoCanvas();
    }
    
    // 隐藏视频Canvas
    void HideVideoCanvas()
    {
        // 如果已经指定了videoCanvas
        if (videoCanvas != null)
        {
            videoCanvas.enabled = false;
            Debug.Log("已隐藏视频Canvas");
        }
        else
        {
            // 尝试查找视频Canvas
            if (completionVideo != null)
            {
                // 尝试在VideoPlayer对象上查找Canvas
                Canvas canvas = completionVideo.GetComponentInChildren<Canvas>();
                if (canvas != null)
                {
                    videoCanvas = canvas;
                    videoCanvas.enabled = false;
                    Debug.Log("已查找并隐藏视频Canvas");
                }
            }
            else
            {
                Debug.LogWarning("无法隐藏视频Canvas：未设置videoCanvas且未找到completionVideo");
            }
        }
    }
    
    // 显示视频Canvas
    void ShowVideoCanvas()
    {
        // 如果已经指定了videoCanvas
        if (videoCanvas != null)
        {
            videoCanvas.enabled = true;
            Debug.Log("已显示视频Canvas");
        }
        else
        {
            // 尝试查找视频Canvas
            if (completionVideo != null)
            {
                // 尝试在VideoPlayer对象上查找Canvas
                Canvas canvas = completionVideo.GetComponentInChildren<Canvas>();
                if (canvas != null)
                {
                    videoCanvas = canvas;
                    videoCanvas.enabled = true;
                    Debug.Log("已查找并显示视频Canvas");
                }
                else
                {
                    Debug.LogWarning("未找到视频Canvas，请确保VideoPlayer对象有Canvas子对象");
                }
            }
            else
            {
                Debug.LogWarning("无法显示视频Canvas：未设置videoCanvas且未找到completionVideo");
            }
        }
    }
    
    // 检查拼图是否已完成
    void CheckCompletion()
    {
        int correctPlacements = 0;
        
        // 检查每个拼图块是否在正确的插槽中
        for (int i = 0; i < puzzlePieces.Length; i++)
        {
            if (i >= slots.Length) continue;
            
            GameObject piece = puzzlePieces[i];
            Transform slot = slots[i];
            
            if (piece == null || slot == null) continue;
            
            // 获取拼图块的编号
            int pieceNumber = GetNumberFromName(piece.name, "PuzzlePiece");
            
            // 获取插槽的编号
            int slotNumber = GetNumberFromName(slot.name, "Slot");
            
            // 计算拼图块和对应插槽之间的距离
            float xDiff = Mathf.Abs(piece.transform.position.x - slot.position.x);
            float zDiff = Mathf.Abs(piece.transform.position.z - slot.position.z);
            
            // 拼图块位于对应插槽附近，且编号匹配
            if (xDiff < 0.1f && zDiff < 0.1f && pieceNumber == slotNumber)
            {
                correctPlacements++;
                Debug.Log($"拼图块{pieceNumber}放置在正确位置");
            }
        }
        
        // 所有拼图块都放置在正确位置
        if (correctPlacements == slots.Length && !isGameCompleted)
        {
            isGameCompleted = true;
            Debug.Log("拼图完成！祝贺你！所有拼图块都放置在正确对应的插槽上。");
            
            // 拼图完成后播放视频
            PlayCompletionVideo();
        }
    }
    
    // 从名称中提取数字
    int GetNumberFromName(string name, string prefix)
    {
        // 移除前缀
        string numberPart = name.Replace(prefix, "");
        
        // 处理clone后缀或其他特殊情况
        if (numberPart.Contains("("))
        {
            numberPart = numberPart.Split('(')[0];
        }
        
        // 尝试解析数字
        int number;
        if (int.TryParse(numberPart, out number))
        {
            return number;
        }
        
        return -1; // 解析失败返回-1
    }
    
    // 重置拼图，将拼图块移回初始位置
    void ResetPuzzle()
    {
        // 将拼图块移到对应的HomeSlot位置
        for (int i = 0; i < puzzlePieces.Length; i++)
        {
            if (i < homeSlots.Length && homeSlots[i] != null)
            {
                puzzlePieces[i].transform.position = homeSlots[i].position;
            }
        }
        
        isGameCompleted = false;
    }

    // 更新
    void Update()
    {
        if (!isGameCompleted)
        {
            CheckCompletion();
        }

        // 按R键重置拼图
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetPuzzle();
        }
    }

    // 播放完成视频
    void PlayCompletionVideo()
    {
        Debug.Log("尝试播放完成视频");
        
        // 显示视频Canvas
        ShowVideoCanvas();
        
        // 检查视频播放器是否已分配
        if (completionVideo != null)
        {
            // 播放视频
            completionVideo.Play();
            Debug.Log("开始播放完成视频");
            
            // 启动场景切换倒计时
            if (!string.IsNullOrEmpty(nextSceneName) && !isSceneTransitioning)
            {
                StartCoroutine(LoadSceneAfterDelay());
            }
        }
        else
        {
            Debug.LogWarning("未分配视频播放器，请在Inspector中设置completionVideo引用");
            
            // 尝试查找场景中的VideoPlayer
            UnityEngine.Video.VideoPlayer foundPlayer = FindObjectOfType<UnityEngine.Video.VideoPlayer>();
            if (foundPlayer != null)
            {
                completionVideo = foundPlayer;
                completionVideo.Play();
                Debug.Log("已找到并使用场景中的VideoPlayer");
                
                // 启动场景切换倒计时
                if (!string.IsNullOrEmpty(nextSceneName) && !isSceneTransitioning)
                {
                    StartCoroutine(LoadSceneAfterDelay());
                }
            }
            else
            {
                // 如果找不到视频播放器，也切换场景
                if (!string.IsNullOrEmpty(nextSceneName))
                {
                    Debug.Log("未找到视频播放器，4秒后直接切换场景");
                    StartCoroutine(LoadSceneAfterDelay());
                }
            }
        }
    }
    
    // 等待指定时间后加载下一个场景
    private IEnumerator LoadSceneAfterDelay()
    {
        isSceneTransitioning = true;
        
        Debug.Log($"视频播放中... {videoPlayTime}秒后将切换到场景: {nextSceneName}");
        
        // 等待指定时间
        yield return new WaitForSeconds(videoPlayTime);
        
        Debug.Log($"准备切换到场景: {nextSceneName}");
        
        try
        {
            SceneManager.LoadScene(nextSceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"场景加载失败: {e.Message}");
            isSceneTransitioning = false; // 重置状态，允许再次尝试
        }
    }

    private void OnEnable()
    {
        // 显示鼠标并解锁
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 启用输入法
        Input.imeCompositionMode = IMECompositionMode.On;
    }

    private void OnDisable()
    {
        // 重新隐藏鼠标并锁定
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // 禁用输入法
        Input.imeCompositionMode = IMECompositionMode.Off;
    }
}
