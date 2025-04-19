using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DraggablePuzzlePiece : MonoBehaviour
{
    private Vector3 screenPoint;
    private Vector3 offset;
    private Vector3 originalPosition;
    private bool isDragging = false;
    private float dragHeight = 0.5f; // 拖动时的高度
    private Camera mainCamera; 
    
    // 空位管理
    private Transform[] slots;
    private Transform[] homeSlots; // 新增：原始位置的插槽
    private static Dictionary<Transform, GameObject> occupiedSlots = new Dictionary<Transform, GameObject>();

    private void Start()
    {
        // 记录初始位置
        originalPosition = transform.position;
        
        // 缓存主相机引用
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("找不到主相机！请确保场景中有一个标记为MainCamera的相机。");
        }
        
        // 查找所有游戏插槽
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
        
        // 查找所有Home插槽
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

    private void OnMouseDown()
    {
        // 防止空引用异常
        if (mainCamera == null) return;
        
        // 计算屏幕点和偏移量
        screenPoint = mainCamera.WorldToScreenPoint(transform.position);
        offset = transform.position - mainCamera.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
        
        // 如果当前在某个插槽中，释放该插槽
        foreach (KeyValuePair<Transform, GameObject> pair in new Dictionary<Transform, GameObject>(occupiedSlots))
        {
            if (pair.Value == gameObject)
            {
                occupiedSlots.Remove(pair.Key);
                break;
            }
        }
        
        // 拖动时将块稍微抬高一点，以便更好地看到
        isDragging = true;
        transform.position = new Vector3(transform.position.x, dragHeight, transform.position.z);
    }

    private void OnMouseDrag()
    {
        // 防止空引用异常
        if (mainCamera == null || !isDragging) return;
        
        // 将鼠标位置转换为世界坐标并移动块
        Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
        Vector3 curPosition = mainCamera.ScreenToWorldPoint(curScreenPoint) + offset;
        
        // 保持Y轴高度不变，只在XZ平面移动
        transform.position = new Vector3(curPosition.x, dragHeight, curPosition.z);
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;
        
        isDragging = false;
        
        // 首先检查最近的游戏插槽
        Transform closestSlot = FindClosestAvailableSlot(slots);
        Transform closestHomeSlot = FindClosestAvailableSlot(homeSlots);
        
        // 判断哪个插槽更近
        float distanceToGameSlot = float.MaxValue;
        float distanceToHomeSlot = float.MaxValue;
        
        if (closestSlot != null)
        {
            distanceToGameSlot = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(closestSlot.position.x, closestSlot.position.z)
            );
        }
        
        if (closestHomeSlot != null)
        {
            distanceToHomeSlot = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(closestHomeSlot.position.x, closestHomeSlot.position.z)
            );
        }
        
        // 选择最近的可用插槽
        Transform targetSlot = null;
        
        if (distanceToGameSlot <= distanceToHomeSlot && closestSlot != null)
        {
            targetSlot = closestSlot;
        }
        else if (closestHomeSlot != null)
        {
            targetSlot = closestHomeSlot;
        }
        
        if (targetSlot != null)
        {
            // 移动到目标插槽位置
            transform.position = new Vector3(targetSlot.position.x, 0, targetSlot.position.z);
            
            // 标记该插槽为已占用
            occupiedSlots[targetSlot] = gameObject;
        }
        else
        {
            // 如果没有可用插槽，返回到初始位置
            transform.position = originalPosition;
        }
    }
    
    // 查找最近的可用插槽
    private Transform FindClosestAvailableSlot(Transform[] slotArray)
    {
        Transform closestSlot = null;
        float minDistance = float.MaxValue;
        
        foreach (Transform slot in slotArray)
        {
            if (slot == null) continue;
            
            // 计算水平距离（只考虑X和Z轴）
            float distance = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(slot.position.x, slot.position.z)
            );
            
            // 如果该插槽未被占用且比当前找到的最近插槽更近
            if (distance < minDistance && (!occupiedSlots.ContainsKey(slot) || occupiedSlots[slot] == gameObject))
            {
                minDistance = distance;
                closestSlot = slot;
            }
        }
        
        return closestSlot;
    }
}
