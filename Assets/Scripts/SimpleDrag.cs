using UnityEngine;

public class SimpleDrag : MonoBehaviour
{
    private Vector3 screenPoint;
    private Vector3 offset;
    private bool isDragging = false;
    private float dragHeight = 0.2f;
    
    void OnMouseDown()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = GameObject.Find("MainCamera");
            if (camObj != null)
            {
                cam = camObj.GetComponent<Camera>();
            }
        }
        
        if (cam == null) return;
        
        screenPoint = cam.WorldToScreenPoint(transform.position);
        offset = transform.position - cam.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
        
        isDragging = true;
        transform.position = new Vector3(transform.position.x, dragHeight, transform.position.z);
    }
    
    void OnMouseDrag()
    {
        if (!isDragging) return;
        
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = GameObject.Find("MainCamera");
            if (camObj != null)
            {
                cam = camObj.GetComponent<Camera>();
            }
        }
        
        if (cam == null) return;
        
        Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
        Vector3 curPosition = cam.ScreenToWorldPoint(curScreenPoint) + offset;
        
        transform.position = new Vector3(curPosition.x, dragHeight, curPosition.z);
    }
    
    void OnMouseUp()
    {
        isDragging = false;
    }
}