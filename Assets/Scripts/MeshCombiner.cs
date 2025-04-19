using UnityEngine;
using UnityEditor; // Editor功能需要

public class MeshCombiner : MonoBehaviour
{
    public string chairTag = "Chair"; // 椅子的标签
    public Material sharedMaterial; // 合并后使用的材质

    [ContextMenu("合并所有椅子")]
    public void CombineChairs()
    {
        // 查找所有标记为Chair的游戏对象
        GameObject[] chairs = GameObject.FindGameObjectsWithTag(chairTag);
        
        if (chairs.Length == 0)
        {
            Debug.LogError("未找到标记为'" + chairTag + "'的椅子!");
            return;
        }
        
        Debug.Log("找到" + chairs.Length + "把椅子，开始合并...");
        
        // 准备合并信息
        CombineInstance[] combine = new CombineInstance[chairs.Length];
        
        // 确保有材质可用
        if (sharedMaterial == null && chairs[0].GetComponent<MeshRenderer>())
            sharedMaterial = chairs[0].GetComponent<MeshRenderer>().sharedMaterial;
        
        // 收集所有椅子的网格
        for (int i = 0; i < chairs.Length; i++)
        {
            MeshFilter meshFilter = chairs[i].GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                Debug.LogWarning("椅子 " + chairs[i].name + " 没有MeshFilter组件，跳过");
                continue;
            }
            
            combine[i].mesh = meshFilter.sharedMesh;
            combine[i].transform = meshFilter.transform.localToWorldMatrix;
        }
        
        // 创建合并后的游戏对象
        GameObject combined = new GameObject("CombinedChairs");
        MeshFilter filter = combined.AddComponent<MeshFilter>();
        MeshRenderer renderer = combined.AddComponent<MeshRenderer>();
        
        // 合并网格
        Mesh finalMesh = new Mesh();
        finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // 支持大型网格
        finalMesh.CombineMeshes(combine);
        filter.mesh = finalMesh;
        
        // 设置材质
        renderer.material = sharedMaterial;
        
        // 添加MeshCollider（如果需要碰撞）
        MeshCollider collider = combined.AddComponent<MeshCollider>();
        collider.sharedMesh = finalMesh;
        
        // 标记为静态对象
        combined.isStatic = true;
        
        // 隐藏原始椅子
        foreach (GameObject chair in chairs)
        {
            chair.SetActive(false);
        }
        
        Debug.Log("合并完成! 创建了CombinedChairs对象，包含" + chairs.Length + "把椅子的网格");
    }
    
    // 编辑器中的按钮
#if UNITY_EDITOR
    [CustomEditor(typeof(MeshCombiner))]
    public class MeshCombinerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            MeshCombiner combiner = (MeshCombiner)target;
            if (GUILayout.Button("合并所有椅子"))
            {
                combiner.CombineChairs();
            }
        }
    }
#endif
}