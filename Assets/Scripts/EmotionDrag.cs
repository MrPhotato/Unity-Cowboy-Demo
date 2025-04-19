using UnityEngine;
using System.Collections;

public class EmotionDrag : MonoBehaviour
{
    private Vector3 screenPoint;
    private Vector3 offset;
    private Vector3 originalPosition;
    private bool isDragging = false;
    private float dragHeight = 0.2f; // 拖动时的高度
    private Camera mainCamera;
    private int emotionNumber;
    
    // 交换相关设置
    private float maxSwapDistance = 3.0f; // 允许交换的最大距离
    
    void Start()
    {
        // 记录原始位置
        originalPosition = transform.position;
        
        // 获取主摄像机 - 使用标签查找以确保在相机重建后仍能找到
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
        if (mainCamera == null)
        {
            Debug.LogError($"EmotionDrag: {gameObject.name} 找不到主摄像机！");
            // 尝试直接查找
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError($"EmotionDrag: {gameObject.name} Camera.main也返回null！");
                // 最后尝试找到任何相机
                mainCamera = FindObjectOfType<Camera>();
                if (mainCamera != null)
                {
                    Debug.Log($"EmotionDrag: {gameObject.name} 使用找到的第一个相机: {mainCamera.name}");
                }
            }
        }
        
        // 从名称中提取编号 (例如 Emotion2 -> 2)
        string name = gameObject.name;
        string numberPart = name.Replace("Emotion", "");
        if (int.TryParse(numberPart, out emotionNumber))
        {
            Debug.Log($"EmotionDrag: 情绪对象{name}初始化完成，编号为{emotionNumber}");
        }
        else
        {
            Debug.LogError($"EmotionDrag: 情绪对象{name}命名格式不正确。请使用EmotionX格式。");
        }
        
        // 检查是否有碰撞器
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            Debug.LogError($"EmotionDrag: {gameObject.name} 没有附加碰撞器组件，将无法响应鼠标事件！");
        }
        else if (!collider.enabled)
        {
            Debug.LogError($"EmotionDrag: {gameObject.name} 的碰撞器已禁用，将无法响应鼠标事件！");
        }
        
        // 禁用可能存在的旧版脚本
        Component oldScript = GetComponent("EmotionDragSimple");
        if (oldScript != null && ((MonoBehaviour)oldScript).enabled)
        {
            Debug.LogWarning($"EmotionDrag: {gameObject.name} 同时具有EmotionDragSimple组件，已自动禁用旧组件。");
            ((MonoBehaviour)oldScript).enabled = false;
        }
    }
    
    void Update()
    {
        // 额外的输入检测，以防OnMouse事件不工作
        if (Input.GetMouseButtonDown(0))
        {
            // 检查点击是否命中此对象
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit) && hit.transform == transform)
            {
                HandleMouseDown();
            }
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            HandleMouseDrag();
        }
        else if (Input.GetMouseButtonUp(0) && isDragging)
        {
            HandleMouseUp();
        }
    }
    
    // 原生Unity鼠标事件
    void OnMouseDown()
    {
        HandleMouseDown();
    }
    
    void OnMouseDrag()
    {
        if (isDragging)
        {
            HandleMouseDrag();
        }
    }
    
    void OnMouseUp()
    {
        if (isDragging)
        {
            HandleMouseUp();
        }
    }
    
    // 拖拽功能实现
    private void HandleMouseDown()
    {
        isDragging = true;
        
        // 计算鼠标位置和物体位置的偏移
        screenPoint = mainCamera.WorldToScreenPoint(transform.position);
        offset = transform.position - mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
        
        // 升高物体以便在拖动时更容易看到
        Vector3 newPosition = transform.position;
        newPosition.y += dragHeight;
        transform.position = newPosition;
    }
    
    private void HandleMouseDrag()
    {
        // 计算拖动位置
        Vector3 cursorPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
        Vector3 cursorPosition = mainCamera.ScreenToWorldPoint(cursorPoint) + offset;
        
        // 保持物体在拖动高度
        cursorPosition.y = originalPosition.y + dragHeight;
        
        // 设置新位置
        transform.position = cursorPosition;
    }
    
    private void HandleMouseUp()
    {
        isDragging = false;
        
        // 检查是否有可交换的拼图块在下方
        bool swapped = CheckForSwap();
        
        // 如果没有交换，返回原位
        if (!swapped)
        {
            ReturnToOriginalPosition();
        }
    }
    
    // 检查是否有可交换的拼图块
    private bool CheckForSwap()
    {
        // 发射射线向下检测目标拼图块
        Ray ray = new Ray(transform.position, Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(ray, 5f);
        
        Debug.Log($"EmotionDrag: 射线检测到 {hits.Length} 个物体");
        
        // 遍历所有碰撞体
        foreach (RaycastHit hit in hits)
        {
            // 跳过自身
            if (hit.transform == transform) 
                continue;
            
            string targetName = hit.transform.name;
            Debug.Log($"EmotionDrag: 检测到物体 {targetName}");
            
            // 检查是否是目标拼图块格式 (PuzzlePieceX(1))
            if (targetName.StartsWith("PuzzlePiece") && targetName.Contains("("))
            {
                // 提取拼图块编号
                string puzzleNumberPart = targetName.Replace("PuzzlePiece", "").Split('(')[0];
                int puzzleNumber;
                
                if (int.TryParse(puzzleNumberPart, out puzzleNumber))
                {
                    // 检查编号是否匹配
                    if (puzzleNumber == emotionNumber)
                    {
                        // 找到对应的原始拼图块 (PuzzlePieceX)
                        string originalPieceName = "PuzzlePiece" + puzzleNumber;
                        GameObject originalPiece = GameObject.Find(originalPieceName);
                        
                        if (originalPiece != null)
                        {
                            // 交换拼图块位置
                            SwapPuzzlePieces(hit.transform.gameObject, originalPiece);
                            return true;
                        }
                        else
                        {
                            Debug.LogError($"EmotionDrag: 找不到原始拼图块 {originalPieceName}");
                        }
                    }
                    else
                    {
                        Debug.Log($"EmotionDrag: 拼图块编号不匹配，情绪编号={emotionNumber}，拼图编号={puzzleNumber}");
                    }
                }
            }
        }
        
        return false;
    }
    
    // 交换两个拼图块的位置
    private void SwapPuzzlePieces(GameObject piece1, GameObject piece2)
    {
        Debug.Log($"EmotionDrag: 交换 {piece1.name} 和 {piece2.name} 的位置");
        
        // 保存位置
        Vector3 piece1Pos = piece1.transform.position;
        Vector3 piece2Pos = piece2.transform.position;
        
        // 交换位置
        piece1.transform.position = piece2Pos;
        piece2.transform.position = piece1Pos;
        
        // 情绪对象消失 - 可以选择隐藏或销毁
        HideEmotionObject();
    }
    
    // 隐藏情绪对象
    private void HideEmotionObject()
    {
        Debug.Log($"EmotionDrag: 情绪对象 {gameObject.name} 成功匹配，正在隐藏");
        
        // 方法1：禁用游戏对象 (如果以后需要再次显示)
        gameObject.SetActive(false);
        
        // 方法2：销毁游戏对象 (如果不再需要)
        // Destroy(gameObject);
    }
    
    // 返回到原始位置
    private void ReturnToOriginalPosition()
    {
        transform.position = originalPosition;
    }
}
