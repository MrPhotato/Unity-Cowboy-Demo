using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyCowboy;

namespace MyCowboy.Demo
{
    public class BasicMotionsCamera : MonoBehaviour
    {
        [Header("[TARGET]")]
        public Transform player; // 角色的 Transform（角色根节点）
        private BasicMotionsCharacterController bMCC; // 参考角色控制器
        private BoxCollider playerBoxCollider; // 参考角色的 BoxCollider

        [Header("[CAMERA PIVOT]")]
        public Transform cameraPositionsRoot; // 摄像机根节点（旋转中心）

        public float pivotHeightOffset;
        public float pivotHeightOffsetCloser;

        [Header("[CAMERA VERTICAL TILT]")]
        public float rotationSpeed = 100f; // 相机倾斜的速度（垂直方向）
        public float maxAngle = 50f; // 最大垂直倾斜角度
        public float minAngle = -50f; // 最小垂直倾斜角度

        [Header("[ZOOM]")]
        public float zoomSpeed = 0.2f; // 缩放速度（滚轮控制）
        public float minDistance = 1f; // 最小缩放距离
        public float maxDistance = 5f; // 最大缩放距离
        private float targetDistance = 5.0f; // 目标缩放距离（用于平滑过渡）

        private float currentDistance; // 当前实际的相机距离
        private float horizontalAngle = 0.0f; // 水平旋转角度
        private float verticalAngle = 0.0f; // 垂直旋转角度

        private float zoomCooldown = 0.1f; // 缩放冷却间隔
        private float zoomTimer = 0f; // 缩放冷却时间

        private float invisibleSurfaceMarginFix = 0.05f; // 障碍检测的边距

        void Awake()
        {
            currentDistance = targetDistance; // 初始化当前距离
            bMCC = player.GetComponent<BasicMotionsCharacterController>(); // 获取角色控制器
            playerBoxCollider = bMCC.collisionBox; // 获取角色的碰撞体
        }

        void Update()
        {
            // 获取输入
            GetInputs();

            // 更新相机控制
            ControlCamera();
        }

        private void GetInputs()
        {
            // 滚轮缩放
            float scrollInput = Input.mouseScrollDelta.y;
            if (zoomTimer >= zoomCooldown)
            {
                if (scrollInput != 0)
                {
                    targetDistance += scrollInput * zoomSpeed; // 根据滚轮输入调整目标距离
                    targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance); // 限制缩放范围
                    zoomTimer = 0f;
                }
            }
            else
            {
                zoomTimer += Time.deltaTime;
            }

            // 鼠标控制水平和垂直角度
            horizontalAngle += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            verticalAngle -= Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;
            verticalAngle = Mathf.Clamp(verticalAngle, minAngle, maxAngle); // 限制垂直角度
        }

        private void ControlCamera()
        {
            // 平滑调整当前距离
            currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * 5f);

            // 根据角度计算相机位置
            float x = Mathf.Cos(verticalAngle * Mathf.Deg2Rad) * Mathf.Sin(horizontalAngle * Mathf.Deg2Rad);
            float z = Mathf.Cos(verticalAngle * Mathf.Deg2Rad) * Mathf.Cos(horizontalAngle * Mathf.Deg2Rad);
            float y = Mathf.Sin(verticalAngle * Mathf.Deg2Rad);
            Vector3 offset = new Vector3(x, y, z) * currentDistance;

            // 设置相机位置和方向
            transform.position = player.position + offset;
            //相机看向比player高一点的地方
            transform.position += Vector3.up * pivotHeightOffset;
            // 创建一个目标点，让相机看向角色而不是自己
            Vector3 targetPoint = player.position + Vector3.up * pivotHeightOffset;
            transform.LookAt(targetPoint);

            // 障碍检测
            Vector3 boxColliderCenter = player.position + playerBoxCollider.center;
            Vector3 cameraRayDirection = (transform.position - boxColliderCenter).normalized;
            float actualDistance = Vector3.Distance(boxColliderCenter, transform.position);
            RaycastHit[] raycastHits = Physics.RaycastAll(boxColliderCenter, cameraRayDirection, actualDistance);

            RaycastHit? closestHit = null;
            foreach (RaycastHit hit in raycastHits)
            {
                if (hit.transform.parent == bMCC.collisionsRoot)
                {
                    if (closestHit == null || hit.distance < closestHit.Value.distance)
                    {
                        closestHit = hit;
                    }
                }
            }

            if (closestHit != null)
            {
                // 将相机位置调整到障碍物前
                transform.position = closestHit.Value.point + (cameraRayDirection * -invisibleSurfaceMarginFix);
            }
        }
    }
}
